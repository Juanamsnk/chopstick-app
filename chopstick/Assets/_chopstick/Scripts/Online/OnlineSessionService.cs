using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Netcode;
using Unity.Services.Multiplayer;

namespace Chopsticks.Online
{
    /// <summary>
    /// Todo lo de Unity Gaming Services en un solo sitio: iniciar sesion (anonima), crear una sala
    /// con codigo, unirse con un codigo, buscar partida rapida y salir.
    /// No sabe nada del juego ni de los mensajes.
    /// </summary>
    /// <summary>La busqueda no encontro ninguna sala abierta y este dispositivo no puede crear una (version web).</summary>
    public class NoOpenGameException : Exception
    {
        public NoOpenGameException() : base("No hay partidas abiertas") { }
    }

    /// <summary>Resultado de mirar si hay otra sala mas antigua mientras esperabas como anfitrion.</summary>
    public enum RescanResult { None, Joined, Recreated }

    public class OnlineSessionService
    {
        private const int QuickJoinTimeoutSeconds = 4;

        private ISession session;

        /// <summary>Codigo para unirse (solo existe tras crear o unirse).</summary>
        public string Code { get { return session != null ? session.Code : null; } }

        /// <summary>true si este dispositivo es el anfitrion de la sala actual.</summary>
        public bool IsHost { get { return session != null && session.IsHost; } }

        // ------------------------------------------------------------
        // Sala con codigo (privada: nadie la encuentra con la busqueda rapida)
        // ------------------------------------------------------------
        public async Task<string> CreateAsync(int maxPlayers)
        {
            await SignInAsync();
            await EnsureNetworkStoppedAsync();
            var options = new SessionOptions { MaxPlayers = maxPlayers, IsPrivate = true }.WithRelayNetwork();
            session = await MultiplayerService.Instance.CreateSessionAsync(options);
            return session.Code;
        }

        public async Task JoinAsync(string code)
        {
            await SignInAsync();
            await EnsureNetworkStoppedAsync();
            session = await MultiplayerService.Instance.JoinSessionByCodeAsync(code);
        }

        // ------------------------------------------------------------
        // Busqueda rapida
        // ------------------------------------------------------------
        /// <summary>
        /// Busca un rival. Primero mira salas abiertas de tu misma etiqueta (pais y version).
        /// Si no hay, mira salas abiertas de cualquier pais (misma version no se comprueba aqui).
        /// Si tampoco hay, crea una sala publica y espera. Despues de esto, IsHost dice que ha pasado.
        /// </summary>
        public async Task QuickMatchAsync(string regionTag)
        {
            await SignInAsync();
            await EnsureNetworkStoppedAsync();

            // 1) Misma region: si no hay ninguna sala, la busqueda lanza una excepcion y se sigue
            try
            {
                session = await MultiplayerService.Instance.MatchmakeSessionAsync(
                    BuildQuickJoin(regionTag, false), BuildSessionOptions(regionTag));
                Debug.Log("[QuickMatch] Paso 1: unido a una sala de tu region. Eres anfitrion: " + session.IsHost);
                return;
            }
            catch (SessionException e)
            {
                // Nadie en tu region ahora mismo
                Debug.Log("[QuickMatch] Paso 1 (tu region): no hay salas. " + e.Message);
            }

#if UNITY_WEBGL && !UNITY_EDITOR
            // 2) La version web no puede crear salas para la busqueda (los demas no podrian entrar):
            //    solo se une a una sala abierta de cualquier region.
            try
            {
                session = await MultiplayerService.Instance.MatchmakeSessionAsync(
                    BuildQuickJoin(null, false), BuildSessionOptions(regionTag));
                Debug.Log("[QuickMatch] Paso 2: unido a una sala abierta. Eres anfitrion: " + session.IsHost);
            }
            catch (SessionException e)
            {
                Debug.Log("[QuickMatch] Paso 2 (cualquier region): no hay salas. " + e.Message);
                throw new NoOpenGameException();
            }
#else
            // 2) Cualquier region; si no hay nadie, crea la sala
            session = await MultiplayerService.Instance.MatchmakeSessionAsync(
                BuildQuickJoin(null, true), BuildSessionOptions(regionTag));
            Debug.Log("[QuickMatch] Paso 2: sala encontrada o creada. Eres anfitrion: " + session.IsHost + ". Codigo: " + session.Code);
#endif
        }

        /// <summary>
        /// Para quien espera como anfitrion en la busqueda rapida: mira si hay otra sala abierta MAS ANTIGUA
        /// (misma version de la app). Si la hay, cierra la tuya y entra en esa.
        /// Sirve para cuando dos moviles buscan a la vez: cada uno no encuentra nada y crea su propia sala, y
        /// sin esto se quedarian los dos esperando para siempre. Siempre se entra en la mas antigua, asi
        /// nunca se hace un circulo (la mas antigua de todas no se mueve y las demas van a ella).
        /// </summary>
        public async Task<RescanResult> TryJoinOlderSessionAsync(string regionTag)
        {
            ISession mine = session;
            if (mine == null || !mine.IsHost || mine.PlayerCount > 1) return RescanResult.None;

            QuerySessionsResults results = await MultiplayerService.Instance.QuerySessionsAsync(new QuerySessionsOptions
            {
                Count = 25,
                FilterOptions = new List<FilterOption>
                {
                    new FilterOption(FilterField.AvailableSlots, "1", FilterOperation.GreaterOrEqual)
                }
            });

            // Hace falta ver tu propia sala en la lista para comparar fechas con la misma vara de medir
            ISessionInfo own = null;
            foreach (ISessionInfo info in results.Sessions)
            {
                if (info.Id == mine.Id) { own = info; break; }
            }
            if (own == null) return RescanResult.None;

            ISessionInfo best = null;
            foreach (ISessionInfo info in results.Sessions)
            {
                if (info.Id == mine.Id || info.IsLocked || info.AvailableSlots < 1) continue;
                if (!SameVersion(info, regionTag)) continue;
                if (!IsOlder(info, own)) continue;
                if (best == null || IsOlder(info, best)) best = info;
            }
            if (best == null) return RescanResult.None;

            // Si mientras tanto alguien entro en tu sala (o la cancelaste), no se mueve nada
            if (session != mine || mine.PlayerCount > 1) return RescanResult.None;

            Debug.Log("[QuickMatch] Hay otra sala mas antigua (" + best.Id + "). Dejo la mia y entro en esa.");
            session = null;
            try { await mine.LeaveAsync(); }
            catch { /* si ya estaba cerrada no pasa nada */ }
            await EnsureNetworkStoppedAsync();

            try
            {
                session = await MultiplayerService.Instance.JoinSessionByIdAsync(best.Id);
                Debug.Log("[QuickMatch] Unido a la sala mas antigua. Eres anfitrion: " + session.IsHost);
                return RescanResult.Joined;
            }
            catch (Exception e)
            {
                // Se llenó o se cerró justo antes de entrar: se vuelve a crear una sala propia y se sigue esperando
                Debug.LogWarning("[QuickMatch] No se pudo entrar en la sala mas antigua: " + e.Message);
                session = await MultiplayerService.Instance.CreateSessionAsync(BuildSessionOptions(regionTag));
                return RescanResult.Recreated;
            }
        }

        private static bool IsOlder(ISessionInfo a, ISessionInfo b)
        {
            if (a.Created != b.Created) return a.Created < b.Created;
            return string.CompareOrdinal(a.Id, b.Id) < 0;
        }

        // Solo salas de busqueda rapida de la misma version de la app (la etiqueta es "PAIS-version")
        private static bool SameVersion(ISessionInfo info, string regionTag)
        {
            int dash = regionTag.IndexOf('-');
            if (dash < 0) return true;
            string version = regionTag.Substring(dash);

            SessionProperty property;
            if (info.Properties != null && info.Properties.TryGetValue("region", out property) && property != null && property.Value != null)
            {
                return property.Value.EndsWith(version);
            }
            return false;
        }

        private static QuickJoinOptions BuildQuickJoin(string regionTag, bool createIfNone)
        {
            var filters = new List<FilterOption>
            {
                new FilterOption(FilterField.AvailableSlots, "1", FilterOperation.GreaterOrEqual)
            };
            if (regionTag != null)
            {
                filters.Add(new FilterOption(FilterField.StringIndex1, regionTag, FilterOperation.Equal));
            }

            return new QuickJoinOptions
            {
                Filters = filters,
                Timeout = TimeSpan.FromSeconds(QuickJoinTimeoutSeconds),
                CreateSession = createIfNone
            };
        }

        // Opciones de la sala que se crea si no hay ninguna: publica, para 2 jugadores, con la etiqueta de region.
        private static SessionOptions BuildSessionOptions(string regionTag)
        {
            var options = new SessionOptions { MaxPlayers = 2, IsPrivate = false, Type = "Session" }.WithRelayNetwork();
            options.SessionProperties = new Dictionary<string, SessionProperty>
            {
                // En algunas versiones del SDK esta enumeracion se llama VisibilityOptions
                { "region", new SessionProperty(regionTag, VisibilityPropertyOptions.Public, PropertyIndex.String1) }
            };
            return options;
        }

        // ------------------------------------------------------------
        // Salir
        // ------------------------------------------------------------
        public async Task LeaveAsync()
        {
            if (session == null) return;
            ISession old = session;
            session = null;
            try { await old.LeaveAsync(); }
            catch { /* si ya estaba cerrada no pasa nada */ }
            await EnsureNetworkStoppedAsync();
        }

        /// <summary>
        /// Si la red de una partida anterior sigue abierta, la nueva sala no llega a usar su propia conexion
        /// (el SDK avisa con "NetworkManager is already connected") y nadie puede entrar. Se cierra antes de empezar.
        /// </summary>
        private static async Task EnsureNetworkStoppedAsync()
        {
            NetworkManager nm = NetworkManager.Singleton;
            if (nm == null) return;

            if (nm.IsListening || nm.IsConnectedClient)
            {
                Debug.LogWarning("[Online] Habia una red de una partida anterior abierta. Se cierra antes de seguir.");
                nm.Shutdown();
            }

            // Se espera a que termine de cerrarse (3 segundos como maximo)
            float until = Time.realtimeSinceStartup + 3f;
            while ((nm.ShutdownInProgress || nm.IsListening) && Time.realtimeSinceStartup < until)
            {
                await Task.Delay(100);
            }
        }

        // Sesion anonima: no hay cuentas ni registro.
        private static async Task SignInAsync()
        {
            if (UnityServices.State == ServicesInitializationState.Uninitialized)
            {
                await UnityServices.InitializeAsync();
            }
            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }
        }
    }
}
