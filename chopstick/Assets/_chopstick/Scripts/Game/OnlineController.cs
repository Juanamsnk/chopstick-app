using System;
using Chopsticks.Online;
using Chopsticks.UI;
using UnityEngine;

namespace Chopsticks.Game
{
    /// <summary>
    /// Coordina el modo online: crear o unirse a una sala, mostrar la sala de espera,
    /// pasar a la partida y salir limpiamente. La partida en si la dibuja GameController.
    /// </summary>
    public class OnlineController
    {
        private readonly ScreenNavigator navigator;
        private readonly OnlineMenuView menu;
        private readonly LobbyView lobby;
        private readonly GameController game;
        private readonly OnlineSessionService service = new OnlineSessionService();

        private OnlineMatch match;
        private bool busy;

        public OnlineController(ScreenNavigator navigator, OnlineMenuView menu, LobbyView lobby, GameController game)
        {
            this.navigator = navigator;
            this.menu = menu;
            this.lobby = lobby;
            this.game = game;

            menu.CreateClicked += OnCreate;
            menu.JoinClicked += OnJoin;
            lobby.StartClicked += OnStartClicked;
        }

        // ------------------------------------------------------------
        // Crear y unirse
        // ------------------------------------------------------------
        private async void OnCreate(string playerName, bool splitAllowed)
        {
            if (busy) return;
            busy = true;
            menu.ShowMessage("Creando la partida...");
            try
            {
                string code = await service.CreateAsync(2);
                match = new OnlineMatch(new NetMessenger(), true, playerName, splitAllowed);
                Hook(match);
                lobby.SetCode(code);
                RefreshLobby();
                menu.ShowMessage("");
                navigator.Show(ScreenId.Lobby);
            }
            catch (Exception e)
            {
                Debug.LogWarning("No se pudo crear la partida: " + e);
                menu.ShowMessage("No se pudo crear la partida. Revisa tu conexión.");
                CleanUp();
            }
            busy = false;
        }

        private async void OnJoin(string playerName, string code)
        {
            if (busy) return;
            busy = true;
            menu.ShowMessage("Uniéndose a la partida...");
            try
            {
                await service.JoinAsync(code);
                match = new OnlineMatch(new NetMessenger(), false, playerName, true);
                Hook(match);
                lobby.SetCode(code);
                RefreshLobby();
                menu.ShowMessage("");
                navigator.Show(ScreenId.Lobby);
            }
            catch (Exception e)
            {
                Debug.LogWarning("No se pudo unir a la partida: " + e);
                menu.ShowMessage("No se encontró esa partida o no se pudo unir.");
                CleanUp();
            }
            busy = false;
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
            // Desde aqui la partida la dibuja GameController
            game.StartMatch(match);
        }

        private void OnDisconnected(string message)
        {
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
