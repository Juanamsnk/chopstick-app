using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;

namespace Chopsticks.Online
{
    /// <summary>
    /// Todo lo de Unity Gaming Services en un solo sitio: iniciar sesion (anonima), crear una sala
    /// con codigo, unirse con un codigo, buscar partida rapida y salir.
    /// No sabe nada del juego ni de los mensajes.
    /// </summary>
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
            var options = new SessionOptions { MaxPlayers = maxPlayers, IsPrivate = true }.WithRelayNetwork();
            session = await MultiplayerService.Instance.CreateSessionAsync(options);
            return session.Code;
        }

        public async Task JoinAsync(string code)
        {
            await SignInAsync();
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

            // 1) Misma region: si no hay ninguna sala, la busqueda lanza una excepcion y se sigue
            try
            {
                session = await MultiplayerService.Instance.MatchmakeSessionAsync(
                    BuildQuickJoin(regionTag, false), BuildSessionOptions(regionTag));
                return;
            }
            catch (SessionException)
            {
                // Nadie en tu region ahora mismo
            }

            // 2) Cualquier region; si no hay nadie, crea la sala
            session = await MultiplayerService.Instance.MatchmakeSessionAsync(
                BuildQuickJoin(null, true), BuildSessionOptions(regionTag));
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
                // Si el compilador dice que VisibilityOptions no existe, en tu version se llama VisibilityPropertyOptions
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
