using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Chopsticks.Career;
using Chopsticks.Shop;
using Chopsticks.Social;
using Chopsticks.UI;
using UnityEngine;

namespace Chopsticks.Game
{
    /// <summary>
    /// Une la pantalla de amigos con Firebase (SocialBackend) y con el modo online.
    ///  - Retar a un amigo: crea una sala online normal y le envia el codigo de la sala.
    ///  - Recibir un reto: sale una ventana; al aceptar, entras en esa sala con el codigo.
    /// </summary>
    public class SocialController
    {
        private const int ChallengeLifetimeMs = 90000;

        private readonly ScreenNavigator navigator;
        private readonly FriendsView view;
        private readonly ChallengePopupView popup;
        private readonly OnlineController online;
        private readonly PlayerProfile profile;
        private readonly SettingsView settings;
        private readonly SocialBackend backend = new SocialBackend();
        private readonly ProgressSync progressSync;   // guarda el progreso en la cuenta (o en el dispositivo si eres invitado)

        private ChallengeInfo pending;   // reto recibido que se esta mostrando
        private string initError;
        private bool busy;

        public SocialController(ScreenNavigator navigator, FriendsView view, ChallengePopupView popup, OnlineController online,
            PlayerProfile profile, SettingsView settings, CareerProgress career, ShopInventory shop)
        {
            this.profile = profile;
            this.settings = settings;
            this.navigator = navigator;
            this.view = view;
            this.popup = popup;
            this.online = online;
            progressSync = new ProgressSync(backend, career, shop);

            navigator.Shown += id => { if (id == ScreenId.Friends) Refresh(); };

            view.GoogleClicked += OnGoogle;
            view.SignOutClicked += OnSignOut;
            settings.GoogleClicked += OnGoogle;      // los mismos botones, desde Ajustes
            settings.SignOutClicked += OnSignOut;
            view.AddClicked += OnAdd;
            view.AcceptClicked += r => Run(() => backend.AcceptRequestAsync(r.Uid), r.Name + " ahora es tu amigo.");
            view.DeclineClicked += r => Run(() => backend.DeclineRequestAsync(r.Uid), "");
            view.RemoveClicked += f => Run(() => backend.RemoveFriendAsync(f.Uid), "");
            view.ChallengeClicked += OnChallenge;

            popup.AcceptClicked += OnAcceptChallenge;
            popup.DeclineClicked += OnDeclineChallenge;

            backend.SignInChanged += Refresh;
            backend.FriendsChanged += list => view.SetFriends(list);
            backend.RequestsChanged += list => view.SetRequests(list);
            backend.ChallengeReceived += OnChallengeReceived;
        }

        /// <summary>Arranca Firebase. Si habia una sesion guardada, se recupera sola.</summary>
        public async void Start()
        {
            if (!SocialBackend.IsSupported)
            {
                initError = "Los amigos no están disponibles en la versión web.";
                Refresh();
                return;
            }
            try
            {
                await backend.InitAsync();
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Social] No se pudo iniciar Firebase: " + e);
                initError = e is SocialException ? e.Message : "No se pudo conectar con Firebase.";
            }
            Refresh();
        }

        /// <summary>Deja de escuchar a Firebase (al cerrar la interfaz).</summary>
        public void Stop()
        {
            backend.StopListening();
        }

        // ------------------------------------------------------------
        // Sesion
        // ------------------------------------------------------------
        private void Refresh()
        {
            if (backend.IsSignedIn)
            {
                view.ShowSignedIn(backend.MyName, backend.MyCode);
                settings.ShowSignedIn();
                profile.Set(backend.MyName);   // el nombre se rellena solo en las partidas
                return;
            }

            profile.Set("");
            string hint;
            bool ready = false;
            if (initError != null) hint = initError;
            else if (!backend.Ready) hint = "Conectando...";
            else { hint = "Inicia sesión para añadir amigos y retarlos a una partida online."; ready = true; }

            view.ShowSignedOut(ready, hint);
            settings.ShowSignedOut(ready, ready ? "Inicia sesión para guardar tu progreso y tus compras en tu cuenta, y jugar con amigos." : hint);
        }

        /// <summary>Muestra un mensaje en Amigos y en Ajustes.</summary>
        private void Say(string text, bool isError)
        {
            view.ShowMessage(text, isError);
            settings.ShowMessage(text, isError);
        }

        private void SetBusyAll(bool value)
        {
            view.SetBusy(value);
            settings.SetBusy(value);
        }

        private async void OnGoogle()
        {
            if (busy) return;
            busy = true;
            SetBusyAll(true);
            Say("Iniciando sesión...", false);
            try
            {
                await backend.SignInWithGoogleAsync();
                Say("", false);
            }
            catch (Exception e)
            {
                Say(MessageOf(e), true);
            }
            SetBusyAll(false);
            busy = false;
            Refresh();
        }

        private void OnSignOut()
        {
            backend.SignOut();
            view.SetFriends(new List<FriendInfo>());
            view.SetRequests(new List<FriendRequestInfo>());
            Say("", false);
            Refresh();
        }

        // ------------------------------------------------------------
        // Amigos
        // ------------------------------------------------------------
        private async void OnAdd(string code)
        {
            if (busy) return;
            busy = true;
            view.SetBusy(true);
            try
            {
                string name = await backend.SendFriendRequestAsync(code);
                view.ClearCodeField();
                view.ShowMessage("Petición enviada a " + name + ".", false);
            }
            catch (Exception e)
            {
                view.ShowMessage(MessageOf(e), true);
            }
            view.SetBusy(false);
            busy = false;
        }

        /// <summary>Ejecuta una accion de Firebase y ensena el resultado en la pantalla de amigos.</summary>
        private async void Run(Func<Task> action, string successMessage)
        {
            try
            {
                await action();
                view.ShowMessage(successMessage, false);
            }
            catch (Exception e)
            {
                view.ShowMessage(MessageOf(e), true);
            }
        }

        // ------------------------------------------------------------
        // Retos
        // ------------------------------------------------------------
        /// <summary>Retar: se crea una sala online (como "Crear partida") y se le manda el codigo al amigo.</summary>
        private void OnChallenge(FriendInfo friend)
        {
            view.ShowMessage("", false);
            online.CreateRoom(backend.MyName, true,
                code => SendChallenge(friend, code),
                message => view.ShowMessage(message, true));
        }

        private async void SendChallenge(FriendInfo friend, string joinCode)
        {
            try
            {
                await backend.SendChallengeAsync(friend.Uid, joinCode);
                await Task.Delay(ChallengeLifetimeMs);   // si no lo acepta en un rato, se retira
                await backend.CancelChallengeAsync(friend.Uid);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Social] Reto: " + e.Message);
            }
        }

        private void OnChallengeReceived(ChallengeInfo challenge)
        {
            // En plena partida, sala o busqueda no se molesta: el reto se descarta
            ScreenId now = navigator.Current;
            if (now == ScreenId.Game || now == ScreenId.Lobby || now == ScreenId.Search)
            {
                Run(() => backend.DismissChallengeAsync(challenge.FromUid), "");
                return;
            }

            pending = challenge;
            popup.Show(challenge.FromName);
        }

        private void OnAcceptChallenge()
        {
            popup.Hide();
            ChallengeInfo challenge = pending;
            pending = null;
            if (challenge == null) return;

            Run(() => backend.DismissChallengeAsync(challenge.FromUid), "");
            online.JoinRoom(backend.MyName, challenge.JoinCode, message =>
            {
                navigator.Show(ScreenId.Friends);
                view.ShowMessage(message, true);
            });
        }

        private void OnDeclineChallenge()
        {
            popup.Hide();
            ChallengeInfo challenge = pending;
            pending = null;
            if (challenge != null) Run(() => backend.DismissChallengeAsync(challenge.FromUid), "");
        }

        private static string MessageOf(Exception e)
        {
            if (e is SocialException) return e.Message;
            Debug.LogWarning("[Social] " + e);
            return "No se pudo completar la acción. Revisa tu conexión.";
        }
    }
}
