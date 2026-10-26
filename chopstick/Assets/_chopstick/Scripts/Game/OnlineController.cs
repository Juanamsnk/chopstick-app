using System;
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
        public async void CreateRoom(string playerName, bool splitAllowed, Action<string> onCreated, Action<string> onError)
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
                match = new OnlineMatch(new NetMessenger(), true, playerName, splitAllowed, false, shop.EquippedId, shop.Tone);
                Hook(match);
                lobby.SetCode(code);
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
        public async void JoinRoom(string playerName, string code, Action<string> onError)
        {
            if (busy)
            {
                if (onError != null) onError("Espera un momento...");
                return;
            }
            busy = true;
            menu.ShowMessage("Uniéndose a la partida...");
            try
            {
                await service.JoinAsync(code);
                match = new OnlineMatch(new NetMessenger(), false, playerName, true, false, shop.EquippedId, shop.Tone);
                Hook(match);
                lobby.SetCode(code);
                RefreshLobby();
                menu.ShowMessage("");
                navigator.Show(ScreenId.Lobby);
            }
            catch (Exception e)
            {
                Debug.LogWarning("No se pudo unir a la partida: " + e);
                string message = "No se encontró esa partida o no se pudo unir.";
                if (onError != null) onError(message); else menu.ShowMessage(message);
                CleanUp();
            }
            busy = false;
        }

        // ------------------------------------------------------------
        // Busqueda rapida
        // ------------------------------------------------------------
        private async void OnQuickMatch(string playerName)
        {
            if (busy)
            {
                menu.ShowMessage("Espera un momento...");
                return;
            }
            busy = true;
            int id = ++searchId;
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
                match = new OnlineMatch(new NetMessenger(), isHost, playerName, true, true, shop.EquippedId, shop.Tone);
                Hook(match);

                if (isHost)
                {
                    // Se creo una sala: se espera a que llegue alguien
                    search.SetStatus("Esperando a un rival...");
                    search.Schedule(ShowMachineAfterMs, () => OfferMachine(id));
                }
                else
                {
                    search.SetStatus("Rival encontrado. Conectando...");
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
            lobby.SetPlayers(match.Names, match.LocalSeat);
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
