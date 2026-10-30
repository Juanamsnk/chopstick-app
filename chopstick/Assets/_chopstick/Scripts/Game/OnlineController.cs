using System;
using System.Threading.Tasks;
using Chopsticks.Online;
using Chopsticks.Shop;
using Chopsticks.UI;
using UnityEngine;

namespace Chopsticks.Game
{
    /// <summary>
    /// Coordina el modo online: crear sala con codigo, unirse con codigo, buscar partida rapida,
    /// la sala de espera, pasar a la partida y salir limpiamente.
    /// La partida en si la dibuja GameController.
    /// </summary>
    public class OnlineController
    {
        private const long ShowMachineAfterMs = 20000;
        private const int RescanMinMs = 4000;    // cada cuanto se mira si hay otra sala mas antigua
        private const int RescanMaxMs = 7000;    // (con un poco de azar para que dos moviles no vayan al compas)
        private const int ConnectTimeoutMs = 15000;   // si entras en una sala y la partida no arranca en este tiempo, se busca otra
        private const int MaxQuickAttempts = 3;

        private readonly ShopInventory shop;
        private readonly ScreenNavigator navigator;
        private readonly OnlineMenuView menu;
        private readonly LobbyView lobby;
        private readonly SearchView search;
        private readonly GameController game;
        private readonly OnlineSessionService service = new OnlineSessionService();

        private OnlineMatch match;
        private bool busy;
        private bool searching;
        private int searchId;       // cada busqueda tiene su numero; si cambia, la busqueda se cancelo
        private int joinId;         // cada intento de unirse tiene el suyo; si cambia, se cancelo
        private string quickName;   // tu nombre en la busqueda rapida en curso
        private int quickAttempts;  // intentos seguidos que se quedaron sin conectar

        /// <summary>Numero de la sala de espera actual. Cambia con cada sala nueva, para no mezclar avisos de salas viejas.</summary>
        public int LobbyId { get; private set; }

        public OnlineController(ScreenNavigator navigator, OnlineMenuView menu, LobbyView lobby,
                                SearchView search, GameController game, ShopInventory shop)
        {
            this.shop = shop;
            this.navigator = navigator;
            this.menu = menu;
            this.lobby = lobby;
            this.search = search;
            this.game = game;

            menu.CreateClicked += OnCreate;
            menu.JoinClicked += OnJoin;
            menu.QuickClicked += OnQuickMatch;
            lobby.StartClicked += OnStartClicked;
            search.CancelClicked += CancelSearch;
            search.MachineClicked += OnPlayMachine;
        }

        // ------------------------------------------------------------
        // Crear sala con codigo
        // ------------------------------------------------------------
        private void OnCreate(string playerName, bool splitAllowed)
        {
            CreateRoom(playerName, splitAllowed, null, null);
        }

        /// <summary>
        /// Crea una sala con codigo y va a la sala de espera. Tambien la usa la pantalla de amigos para retar:
        /// onCreated recibe el codigo y onError un mensaje si falla (si es null, el error sale en el menu online).
        /// </summary>
        public async void CreateRoom(string playerName, bool splitAllowed, Action<string> onCreated, Action<string> onError,
                                     string challengedName = null)
        {
            if (busy)
            {
                if (onError != null) onError("Espera un momento...");
                return;
            }
            busy = true;
            menu.ShowMessage("Creando la partida...");
            try
            {
                string code = await service.CreateAsync(2);
                match = new OnlineMatch(new NetMessenger(), true, playerName, splitAllowed, false, shop.EquippedId, shop.Tone, shop.OnlineRank);
                Hook(match);
                LobbyId++;
                lobby.SetCode(code);
                // Si es un reto, la sala de espera explica a quien se esta esperando
                lobby.SetChallengeStatus(challengedName != null ? "Enviando reto a " + challengedName + "..." : null);
                RefreshLobby();
                menu.ShowMessage("");
                navigator.Show(ScreenId.Lobby);
                if (onCreated != null) onCreated(code);
            }
            catch (Exception e)
            {
                Debug.LogWarning("No se pudo crear la partida: " + e);
                string message = "No se pudo crear la partida. Revisa tu conexión.";
                if (onError != null) onError(message); else menu.ShowMessage(message);
                CleanUp();
            }
            busy = false;
        }

        // ------------------------------------------------------------
        // Unirse con codigo
        // ------------------------------------------------------------
        private void OnJoin(string playerName, string code)
        {
            JoinRoom(playerName, code, null);
        }

        /// <summary>
        /// Se une a una sala con su codigo y va a la sala de espera. Tambien la usa la pantalla de amigos
        /// al aceptar un reto: onError recibe un mensaje si falla (si es null, el error sale en el menu online).
        /// </summary>
        public async void JoinRoom(string playerName, string code, Action<string> onError, Action onJoined = null)
        {
            if (busy)
            {
                if (onError != null) onError("Espera un momento...");
                return;
            }
            busy = true;
            int id = ++joinId;
            menu.ShowMessage("Uniéndose a la partida...");
            try
            {
                await service.JoinAsync(code);

                // Si se cancelo mientras se esperaba, se sale de la sala sin ensenar nada
                if (id != joinId)
                {
                    CleanUp();
                    busy = false;
                    return;
                }

                match = new OnlineMatch(new NetMessenger(), false, playerName, true, false, shop.EquippedId, shop.Tone, shop.OnlineRank);
                Hook(match);
                LobbyId++;
                lobby.SetCode(code);
                lobby.SetChallengeStatus(null);
                RefreshLobby();
                menu.ShowMessage("");
                navigator.Show(ScreenId.Lobby);
                if (onJoined != null) onJoined();
            }
            catch (Exception e)
            {
                Debug.LogWarning("No se pudo unir a la partida: " + e);
                CleanUp();
                if (id == joinId)
                {
                    string message = "No se encontró esa partida o no se pudo unir.";
                    if (onError != null) onError(message); else menu.ShowMessage(message);
                }
            }
            busy = false;
        }

        /// <summary>Cancela el intento de unirse que esta en marcha (si ya entro en la sala, no hace nada).</summary>
        public void CancelJoin()
        {
            joinId++;
        }

        /// <summary>Cambia el aviso de la sala de espera del anfitrion (por ejemplo, el estado de un reto). Se ignora si la sala ya no es esa.</summary>
        public void SetLobbyStatus(int lobbyId, string text)
        {
            if (lobbyId != LobbyId || match == null) return;
            lobby.SetChallengeStatus(text);
            RefreshLobby();
        }

        // ------------------------------------------------------------
        // Busqueda rapida
        // ------------------------------------------------------------
        private void OnQuickMatch(string playerName)
        {
            quickAttempts = 0;
            StartQuickMatch(playerName);
        }

        private async void StartQuickMatch(string playerName)
        {
            if (busy)
            {
                menu.ShowMessage("Espera un momento...");
                return;
            }
            busy = true;
            int id = ++searchId;
            quickName = playerName;
            searching = true;
            menu.ShowMessage("");
            search.Begin();
            navigator.Show(ScreenId.Search);

            try
            {
                await service.QuickMatchAsync(RegionTag.Get());

                // Si cancelaste mientras se buscaba, se sale de la sala que se haya encontrado
                if (id != searchId)
                {
                    await service.LeaveAsync();
                    return;
                }

                bool isHost = service.IsHost;
                Debug.Log("[Online] Busqueda lista. Anfitrion: " + isHost + ". Codigo: " + service.Code);
                match = new OnlineMatch(new NetMessenger(), isHost, playerName, true, true, shop.EquippedId, shop.Tone, shop.OnlineRank);
                Hook(match);

                if (isHost)
                {
                    // Se creo una sala: se espera a que llegue alguien
                    search.SetStatus("Esperando a un rival...");
                    search.Schedule(ShowMachineAfterMs, () => OfferMachine(id));
                    RescanLoop(id);   // por si otro movil buscaba a la vez y creo su propia sala
                }
                else
                {
                    search.SetStatus("Rival encontrado. Conectando...");
                    ConnectWatchdog(id);   // si la partida no arranca, no te quedas esperando para siempre
                }
            }
            catch (NoOpenGameException)
            {
                // Solo pasa en la version web: no hay ninguna sala abierta a la que unirse
                if (id == searchId)
                {
                    searching = false;
                    search.End();
                    CleanUp();
                    menu.ShowMessage("No hay partidas abiertas ahora mismo. Prueba otra vez en unos segundos o juega contra la máquina.");
                    navigator.Show(ScreenId.OnlineMenu);
                }
            }
            catch (Exception e)
            {
                if (id == searchId)
                {
                    Debug.LogWarning("No se pudo buscar partida: " + e);
                    searching = false;
                    search.End();
                    CleanUp();
                    menu.ShowMessage("No se pudo buscar partida. Revisa tu conexión.");
                    navigator.Show(ScreenId.OnlineMenu);
                }
            }
            finally
            {
                busy = false;
            }
        }

        /// <summary>
        /// Entraste en una sala como cliente pero la partida no arranca (anfitrion que ya no esta, red que no conecta...).
        /// Pasado un tiempo se abandona esa sala y se busca otra, hasta MaxQuickAttempts veces.
        /// </summary>
        private async void ConnectWatchdog(int id)
        {
            await Task.Delay(ConnectTimeoutMs);
            if (id != searchId || !searching || match == null || match.IsHost) return;

            quickAttempts++;
            Debug.LogWarning("[Online] La partida no arranco en " + (ConnectTimeoutMs / 1000) + " s. Intento " + quickAttempts + " de " + MaxQuickAttempts + ".");
            searchId++;          // esta busqueda ya no vale
            CleanUp();

            if (quickAttempts >= MaxQuickAttempts)
            {
                searching = false;
                search.End();
                menu.ShowMessage("No se pudo conectar con ningún rival. Prueba otra vez en un momento.");
                navigator.Show(ScreenId.OnlineMenu);
                return;
            }

            search.SetStatus("No se pudo conectar con ese rival. Buscando otro...");
            await Task.Delay(2000);
            if (!searching) return;   // cancelaste mientras tanto
            StartQuickMatch(quickName);
        }

        private bool StillWaitingAsHost(int id)
        {
            return id == searchId && searching && match != null && match.IsHost && match.Names.Count < 2;
        }

        /// <summary>
        /// Mientras esperas rival como anfitrion, cada pocos segundos mira si hay otra sala mas antigua y, si la hay,
        /// te cambias a ella. Asi dos moviles que buscan a la vez acaban en la misma partida.
        /// </summary>
        private async void RescanLoop(int id)
        {
            while (true)
            {
                await Task.Delay(UnityEngine.Random.Range(RescanMinMs, RescanMaxMs));
                if (!StillWaitingAsHost(id)) return;

                RescanResult result;
                try
                {
                    result = await service.TryJoinOlderSessionAsync(RegionTag.Get());
                }
                catch (Exception e)
                {
                    Debug.LogWarning("[QuickMatch] No se pudo mirar si hay otras salas: " + e.Message);
                    continue;
                }

                // Cancelaste (o empezo la partida) mientras se cambiaba de sala
                if (id != searchId || !searching)
                {
                    if (result != RescanResult.None) await service.LeaveAsync();
                    return;
                }
                if (result == RescanResult.None) continue;

                // Cambio de sala: el anfitrion de antes se sustituye por el nuevo (cliente, o anfitrion de la sala recreada)
                Unhook(match);
                match.Dispose();
                bool isHost = service.IsHost;
                match = new OnlineMatch(new NetMessenger(), isHost, quickName, true, true, shop.EquippedId, shop.Tone, shop.OnlineRank);
                Hook(match);
                search.SetStatus(isHost ? "Esperando a un rival..." : "Rival encontrado. Conectando...");
                if (!isHost) return;
            }
        }

        private void Unhook(OnlineMatch m)
        {
            m.LobbyChanged -= RefreshLobby;
            m.Started -= OnStarted;
            m.Disconnected -= OnDisconnected;
            m.Closed -= CleanUp;
        }

        /// <summary>Pasado un rato sin rival, se ofrece jugar contra la maquina (la sala sigue abierta).</summary>
        private void OfferMachine(int id)
        {
            if (id != searchId || !searching) return;
            search.SetStatus("Todavía no hay rivales. Puedes seguir esperando o jugar contra la máquina.");
            search.ShowMachineOption(true);
        }

        private void CancelSearch()
        {
            searchId++;          // invalida la busqueda en curso
            searching = false;
            search.End();
            CleanUp();
            navigator.Show(ScreenId.OnlineMenu);
        }

        private void OnPlayMachine()
        {
            CancelSearch();
            navigator.Show(ScreenId.AiSetup);
        }

        // ------------------------------------------------------------
        // Sala y partida
        // ------------------------------------------------------------
        private void Hook(OnlineMatch m)
        {
            m.LobbyChanged += RefreshLobby;
            m.Started += OnStarted;
            m.Disconnected += OnDisconnected;
            m.Closed += CleanUp;
        }

        private void RefreshLobby()
        {
            if (match == null) return;
            lobby.SetPlayers(match.Names, match.LocalSeat, match.RankNames);
            lobby.SetHostControls(match.IsHost, match.Names.Count);
        }

        private void OnStartClicked()
        {
            if (match != null) match.StartGame();
        }

        private void OnStarted()
        {
            searching = false;
            search.End();
            // Desde aqui la partida la dibuja GameController
            game.StartMatch(match);
        }

        private void OnDisconnected(string message)
        {
            searching = false;
            search.End();
            CleanUp();
            menu.ShowMessage(message);
            navigator.Show(ScreenId.OnlineMenu);
        }

        /// <summary>Cierra la red y la sala. Es seguro llamarla varias veces.</summary>
        public void CleanUp()
        {
            if (match != null)
            {
                match.LobbyChanged -= RefreshLobby;
                match.Started -= OnStarted;
                match.Disconnected -= OnDisconnected;
                match.Closed -= CleanUp;
                match.Dispose();
                match = null;
            }
            LeaveService();
        }

        private async void LeaveService()
        {
            await service.LeaveAsync();
        }
    }
}
