using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Chopsticks.Career;
using Chopsticks.Online;
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
        private readonly LeaderboardView leaderboard;
        private readonly SocialBackend backend = new SocialBackend();
        private readonly ProgressSync progressSync;   // guarda el progreso en la cuenta (o en el dispositivo si eres invitado)

        private ChallengeInfo pending;   // reto recibido que se esta mostrando
        private bool joining;            // se esta entrando en la partida de un reto aceptado
        private bool sending;            // la ventana de "preparando tu reto" (o su error) esta abierta
        private string initError;
        private bool busy;
        private bool settled;

        /// <summary>Firebase ya arranco y, si habia sesion, ya se cargo el progreso de la cuenta (la pantalla de carga espera esto).</summary>
        public event Action Settled;

        public SocialController(ScreenNavigator navigator, FriendsView view, ChallengePopupView popup, OnlineController online,
            PlayerProfile profile, SettingsView settings, CareerProgress career, ShopInventory shop,
            LeaderboardView leaderboard)
        {
            this.leaderboard = leaderboard;
            this.profile = profile;
            this.settings = settings;
            this.navigator = navigator;
            this.view = view;
            this.popup = popup;
            this.online = online;
            progressSync = new ProgressSync(backend, career, shop);

            navigator.Shown += id =>
            {
                if (id == ScreenId.Friends) Refresh();
                if (id == ScreenId.Leaderboard) LoadLeaderboard();
            };

            // Victorias y derrotas ONLINE: se apuntan al terminar cada partida (el modo carrera no pasa por aqui)
            online.UidProvider = () => backend.Uid;
            online.ResultReady += OnOnlineResult;
            view.LeaderboardClicked += () => navigator.Show(ScreenId.Leaderboard);
            leaderboard.RefreshClicked += LoadLeaderboard;
            backend.RecordChanged += r => view.SetMyRecord(r);
            backend.VersusChanged += map => view.SetRecords(map);

            view.GoogleClicked += OnGoogle;
            settings.GoogleClicked += OnGoogle;      // los mismos botones, desde Ajustes
            settings.SignOutClicked += OnSignOut;
            settings.NameSaveClicked += OnRename;
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
                MarkSettled();
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

            // Con sesion iniciada, se espera a que lleguen tu progreso y tus compras antes de dar todo por listo
            if (progressSync.IsSigningIn) progressSync.SignInFinished += OnSignInFinished;
            else MarkSettled();
        }

        private void OnSignInFinished()
        {
            progressSync.SignInFinished -= OnSignInFinished;
            MarkSettled();
        }

        private void MarkSettled()
        {
            if (settled) return;
            settled = true;
            if (Settled != null) Settled();
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

        /// <summary>Guarda el nombre que escribiste en Ajustes. Al terminar, Refresh lo reparte por toda la app.</summary>
        private async void OnRename(string newName)
        {
            if (busy) return;
            busy = true;
            SetBusyAll(true);
            try
            {
                await backend.UpdateNameAsync(newName);
                settings.ShowNameMessage("Nombre guardado.", false);
            }
            catch (Exception e)
            {
                settings.ShowNameMessage(MessageOf(e), true);
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
        // Victorias, derrotas y clasificacion
        // ------------------------------------------------------------
        /// <summary>Termino una partida online: se apunta al marcador general y al marcador contra ese rival.</summary>
        private async void OnOnlineResult(OnlineResult result)
        {
            if (!backend.IsSignedIn) return;   // los invitados no puntuan
            await RecordWithRetry(() => backend.RecordGeneralAsync(result.Won));
            await RecordWithRetry(() => backend.RecordVersusAsync(result.Won, result.OpponentUid, result.OpponentName));
        }

        /// <summary>
        /// Reintenta una vez si falla por la red. Si fue por tiempo agotado NO se reintenta:
        /// la escritura podria haber llegado igualmente y la partida se contaria dos veces.
        /// </summary>
        private static async Task RecordWithRetry(Func<Task> action)
        {
            for (int attempt = 0; attempt < 2; attempt++)
            {
                try
                {
                    await action();
                    return;
                }
                catch (SocialException e)
                {
                    Debug.LogWarning("[Social] Resultado sin confirmar: " + e.Message);
                    return;
                }
                catch (Exception e)
                {
                    Debug.LogWarning("[Social] No se pudo apuntar el resultado (intento " + (attempt + 1) + "): " + e.Message);
                    if (attempt == 0) await Task.Delay(4000);
                }
            }
        }

        private async void LoadLeaderboard()
        {
            if (!backend.IsSignedIn)
            {
                leaderboard.ShowError("Inicia sesión para ver la clasificación.");
                return;
            }
            leaderboard.ShowLoading();
            try
            {
                List<LeaderboardEntry> entries = await backend.LoadLeaderboardAsync(50);
                leaderboard.ShowEntries(entries, backend.Uid, backend.MyRecord);
            }
            catch (Exception e)
            {
                leaderboard.ShowError(MessageOf(e));
            }
        }

        // ------------------------------------------------------------
        // Retos
        // ------------------------------------------------------------
        /// <summary>Retar: se crea una sala online (como "Crear partida") y se le manda el codigo al amigo.</summary>
        private void OnChallenge(FriendInfo friend)
        {
            sending = true;
            popup.ShowPreparing(friend.Name);
            online.CreateRoom(backend.MyName, true,
                code => SendChallenge(friend, code),
                message => popup.ShowError(message),   // la ventana se queda con el error hasta que la cierres
                friend.Name);
        }

        private async void SendChallenge(FriendInfo friend, string joinCode)
        {
            sending = false;
            popup.Hide();   // la sala ya esta lista: se acabo el "preparando"
            int lobbyId = online.LobbyId;   // los avisos solo valen para esta sala
            try
            {
                await backend.SendChallengeAsync(friend.Uid, joinCode);
                online.SetLobbyStatus(lobbyId, "Reto enviado a " + friend.Name + ". Esperando a que acepte...");
                await Task.Delay(ChallengeLifetimeMs);   // si no lo acepta en un rato, se retira
                await backend.CancelChallengeAsync(friend.Uid);
                online.SetLobbyStatus(lobbyId, friend.Name + " no ha respondido al reto. Puedes seguir esperando o pasarle el código.");
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Social] Reto: " + e.Message);
                online.SetLobbyStatus(lobbyId, "No se pudo enviar el reto a " + friend.Name + ". Puedes pasarle el código tú mismo.");
            }
        }

        private void OnChallengeReceived(ChallengeInfo challenge)
        {
            // En plena partida, sala o busqueda no se molesta: el reto se descarta
            ScreenId now = navigator.Current;
            if (joining || sending || now == ScreenId.Game || now == ScreenId.Lobby || now == ScreenId.Search)
            {
                Run(() => backend.DismissChallengeAsync(challenge.FromUid), "");
                return;
            }

            pending = challenge;
            popup.Show(challenge.FromName);
        }

        private void OnAcceptChallenge()
        {
            ChallengeInfo challenge = pending;
            pending = null;
            if (challenge == null)
            {
                popup.Hide();
                return;
            }

            // La ventana se queda abierta con "Uniéndote..." hasta que se entra en la sala o falla
            joining = true;
            popup.ShowJoining(challenge.FromName);
            Run(() => backend.DismissChallengeAsync(challenge.FromUid), "");
            online.JoinRoom(backend.MyName, challenge.JoinCode,
                message =>
                {
                    joining = false;
                    popup.Hide();
                    navigator.Show(ScreenId.Friends);
                    view.ShowMessage(message == "Espera un momento..."
                        ? message
                        : "No se pudo entrar en la partida de " + challenge.FromName + ". Puede que la haya cerrado.", true);
                },
                () =>
                {
                    joining = false;
                    popup.Hide();
                });
        }

        private void OnDeclineChallenge()
        {
            // Si la ventana muestra el error de tu reto, este boton es "Cerrar"
            if (sending)
            {
                sending = false;
                popup.Hide();
                return;
            }

            // Mientras se entra en la partida, este boton es "Cancelar"
            if (joining)
            {
                joining = false;
                online.CancelJoin();
                popup.Hide();
                return;
            }

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
