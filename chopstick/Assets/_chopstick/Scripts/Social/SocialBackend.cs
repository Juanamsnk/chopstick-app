using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
#if !UNITY_WEBGL
using Firebase;
using Firebase.Auth;
using Firebase.Database;
#endif

namespace Chopsticks.Social
{
#if !UNITY_WEBGL
    /// <summary>
    /// Todo lo de Firebase en un solo sitio: iniciar sesion con Google (Authentication) y
    /// guardar amigos, peticiones y retos en Realtime Database.
    ///
    /// Estructura de la base de datos:
    ///   users/{uid}                      { name, code }       perfil publico (nombre y codigo de amigo)
    ///   codes/{codigo}                   uid                  para buscar a alguien por su codigo
    ///   friends/{uid}/{amigo}            true                 lista de amigos (en las dos direcciones)
    ///   friendRequests/{para}/{de}       { name }             peticiones pendientes
    ///   challenges/{para}/{de}           { name, code }       retos pendientes (code = codigo de la sala online)
    ///   saves/{uid}                      { completed, coins, owned, equipped, tone, updated }   progreso de la cuenta
    ///   leaderboard/{uid}                { name, wins, losses }   victorias y derrotas ONLINE (clasificacion general; lectura publica)
    ///   vs/{uid}/{rival}                 { name, wins, losses }   tu marcador contra cada rival (solo lo lees tu)
    /// Las reglas de seguridad estan en Firebase/database.rules.json.
    /// </summary>
    public partial class SocialBackend
    {
        public const bool IsSupported = true;

        private const int TimeoutMs = 12000;
        private const string CodeChars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";   // sin 0, O, 1, I
        private const int CodeLength = 6;

        private readonly System.Random random = new System.Random();
        private FirebaseAuth auth;
        private DatabaseReference root;
        private DatabaseReference friendsRef;
        private DatabaseReference requestsRef;
        private DatabaseReference challengesRef;
        private DatabaseReference saveRef;
        private DatabaseReference statsRef;
        private DatabaseReference versusRef;
        private int friendsVersion;

        /// <summary>Firebase esta listo (dependencias comprobadas).</summary>
        public bool Ready { get; private set; }
        public bool IsSignedIn
        {
            get
            {
#if UNITY_EDITOR
                if (mockSignedIn) return true;   // datos de prueba del editor
#endif
                return auth != null && auth.CurrentUser != null;
            }
        }

        public string Uid
        {
            get
            {
#if UNITY_EDITOR
                if (mockSignedIn) return "mock-me";
#endif
                return IsSignedIn ? auth.CurrentUser.UserId : null;
            }
        }
        public string MyName { get; private set; }
        public string MyCode { get; private set; }

        /// <summary>Tus victorias y derrotas online (se actualiza sola mientras hay sesion).</summary>
        public RecordInfo MyRecord { get; private set; } = new RecordInfo();

        public event Action SignInChanged;
        public event Action<List<FriendInfo>> FriendsChanged;
        public event Action<List<FriendRequestInfo>> RequestsChanged;
        public event Action<ChallengeInfo> ChallengeReceived;
        public event Action<SaveData> SaveChanged;
        public event Action<RecordInfo> RecordChanged;                         // tus victorias y derrotas online
        public event Action<Dictionary<string, RecordInfo>> VersusChanged;     // tu marcador contra cada rival (clave = uid)

        // ------------------------------------------------------------
        // Inicio y sesion
        // ------------------------------------------------------------
        /// <summary>Arranca Firebase. Si ya habia una sesion guardada en el dispositivo, la recupera.</summary>
        public async Task InitAsync()
        {
#if UNITY_EDITOR
            // En el editor, por defecto: amigos de prueba, sin Firebase (menu Chopsticks > Amigos de prueba en el editor)
            mockActive = MockEnabled;
            if (mockActive)
            {
                Ready = true;
                MockSignIn();
                return;
            }
#endif
            DependencyStatus status = await FirebaseApp.CheckAndFixDependenciesAsync();
            if (status != DependencyStatus.Available)
                throw new SocialException("Firebase no está disponible en este dispositivo (" + status + ").");

            auth = FirebaseAuth.DefaultInstance;
            root = FirebaseDatabase.DefaultInstance.RootReference;
            Ready = true;

            if (auth.CurrentUser != null) await OnSignedInAsync();
        }

        /// <summary>Inicia sesion con Google. En el editor entra como anonimo para poder probar.</summary>
        public async Task SignInWithGoogleAsync()
        {
            if (!Ready) throw new SocialException("Todavía se está conectando. Espera un momento.");
#if UNITY_EDITOR
            if (mockActive)
            {
                MockSignIn();
                return;
            }
#endif
            try
            {
#if UNITY_EDITOR
                await auth.SignInAnonymouslyAsync();
#else
                var data = new FederatedOAuthProviderData
                {
                    ProviderId = "google.com",
                    Scopes = new List<string> { "email", "profile" }
                };
                var provider = new FederatedOAuthProvider();
                provider.SetProviderData(data);
                await auth.SignInWithProviderAsync(provider);
#endif
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Social] Inicio de sesion fallido: " + e);
                throw new SocialException("No se pudo iniciar sesión con Google.");
            }
            await OnSignedInAsync();
        }

        public void SignOut()
        {
#if UNITY_EDITOR
            mockSignedIn = false;
#endif
            StopListening();
            if (auth != null) auth.SignOut();
            MyName = null;
            MyCode = null;
            MyRecord = new RecordInfo();
            if (RecordChanged != null) RecordChanged(MyRecord);
            if (VersusChanged != null) VersusChanged(new Dictionary<string, RecordInfo>());
            if (SignInChanged != null) SignInChanged();
        }

        private async Task OnSignedInAsync()
        {
            string name = auth.CurrentUser.DisplayName;
            if (string.IsNullOrWhiteSpace(name)) name = "Jugador";
            name = name.Trim();
            MyName = name.Length > 20 ? name.Substring(0, 20) : name;

            await EnsureProfileAsync();
            StartListening();
            if (SignInChanged != null) SignInChanged();
        }

        /// <summary>Crea el perfil con su codigo de amigo la primera vez; las siguientes solo actualiza el nombre.</summary>
        private async Task EnsureProfileAsync()
        {
            string uid = Uid;
            DatabaseReference userRef = root.Child("users").Child(uid);

            DataSnapshot snap = await WithTimeout(userRef.GetValueAsync());
            if (snap.Exists && snap.Child("code").Exists)
            {
                MyCode = snap.Child("code").Value.ToString();

                // El nombre guardado en la cuenta manda (puede ser el que elegiste en Ajustes).
                // El de Google solo se usa la primera vez o si la cuenta no tiene ninguno.
                object saved = snap.Child("name").Value;
                string savedName = saved != null ? saved.ToString().Trim() : "";
                if (savedName.Length > 0) MyName = savedName;
                else await WithTimeout(userRef.Child("name").SetValueAsync(MyName));
                return;
            }

            for (int attempt = 0; attempt < 8; attempt++)
            {
                string code = RandomCode();
                try
                {
                    // Las reglas solo dejan reservar un codigo que todavia no existe
                    await WithTimeout(root.Child("codes").Child(code).SetValueAsync(uid));
                }
                catch (SocialException) { throw; }
                catch (Exception) { continue; }   // ya estaba cogido: se prueba otro

                var profile = new Dictionary<string, object> { { "name", MyName }, { "code", code } };
                await WithTimeout(userRef.SetValueAsync(profile));
                MyCode = code;
                return;
            }
            throw new SocialException("No se pudo crear tu código de amigo. Inténtalo de nuevo.");
        }

        /// <summary>Cambia tu nombre en la cuenta: lo ven tus amigos y sale en tus partidas.</summary>
        public async Task UpdateNameAsync(string name)
        {
            name = (name ?? "").Trim();
            if (name.Length == 0) throw new SocialException("Escribe un nombre.");
            if (name.Length > PlayerProfile.MaxNameLength) name = name.Substring(0, PlayerProfile.MaxNameLength);
            if (!IsSignedIn) throw new SocialException("Inicia sesión para cambiar tu nombre.");
#if UNITY_EDITOR
            if (mockActive)
            {
                await Task.Delay(MockDelayMs);
                MyName = name;
                return;
            }
#endif
            await WithTimeout(root.Child("users").Child(Uid).Child("name").SetValueAsync(name));
            MyName = name;

            // Si ya tienes marcador en la clasificacion, se cambia el nombre ahi tambien (si no se puede, no pasa nada)
            try
            {
                DatabaseReference board = root.Child("leaderboard").Child(Uid);
                DataSnapshot entry = await WithTimeout(board.GetValueAsync());
                if (entry.Exists) await WithTimeout(board.Child("name").SetValueAsync(name));
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Social] No se pudo actualizar el nombre en la clasificacion: " + e.Message);
            }
        }

        private string RandomCode()
        {
            var chars = new char[CodeLength];
            for (int i = 0; i < CodeLength; i++) chars[i] = CodeChars[random.Next(CodeChars.Length)];
            return new string(chars);
        }

        // ------------------------------------------------------------
        // Amigos
        // ------------------------------------------------------------
        /// <summary>Envia una peticion de amistad al dueno de ese codigo. Devuelve su nombre.</summary>
        public async Task<string> SendFriendRequestAsync(string code)
        {
#if UNITY_EDITOR
            if (mockActive) return await MockSendFriendRequestAsync(code);
#endif
            code = (code ?? "").Trim().ToUpperInvariant();
            if (code.Length != CodeLength) throw new SocialException("El código tiene " + CodeLength + " caracteres.");
            if (code == MyCode) throw new SocialException("Ese es tu propio código.");

            DataSnapshot found = await WithTimeout(root.Child("codes").Child(code).GetValueAsync());
            if (!found.Exists) throw new SocialException("No existe ningún jugador con ese código.");
            string other = found.Value.ToString();
            if (other == Uid) throw new SocialException("Ese es tu propio código.");

            DataSnapshot already = await WithTimeout(root.Child("friends").Child(Uid).Child(other).GetValueAsync());
            if (already.Exists) throw new SocialException("Ya sois amigos.");

            string name = "Jugador";
            DataSnapshot nameSnap = await WithTimeout(root.Child("users").Child(other).Child("name").GetValueAsync());
            if (nameSnap.Exists && nameSnap.Value != null) name = nameSnap.Value.ToString();

            // Si el otro ya me habia enviado una peticion, se aceptan directamente
            DataSnapshot theirs = await WithTimeout(root.Child("friendRequests").Child(Uid).Child(other).GetValueAsync());
            if (theirs.Exists)
            {
                await AcceptRequestAsync(other);
                return name;
            }

            var request = new Dictionary<string, object> { { "name", MyName } };
            await WithTimeout(root.Child("friendRequests").Child(other).Child(Uid).SetValueAsync(request));
            return name;
        }

        /// <summary>Acepta una peticion: los dos quedan como amigos y la peticion se borra (todo a la vez).</summary>
        public Task AcceptRequestAsync(string fromUid)
        {
#if UNITY_EDITOR
            if (mockActive) return MockAcceptRequestAsync(fromUid);
#endif
            var updates = new Dictionary<string, object>
            {
                { "friends/" + Uid + "/" + fromUid, true },
                { "friends/" + fromUid + "/" + Uid, true },
                { "friendRequests/" + Uid + "/" + fromUid, null }
            };
            return WithTimeout(root.UpdateChildrenAsync(updates));
        }

        public Task DeclineRequestAsync(string fromUid)
        {
#if UNITY_EDITOR
            if (mockActive) return MockDeclineRequestAsync(fromUid);
#endif
            return WithTimeout(root.Child("friendRequests").Child(Uid).Child(fromUid).RemoveValueAsync());
        }

        public Task RemoveFriendAsync(string friendUid)
        {
#if UNITY_EDITOR
            if (mockActive) return MockRemoveFriendAsync(friendUid);
#endif
            var updates = new Dictionary<string, object>
            {
                { "friends/" + Uid + "/" + friendUid, null },
                { "friends/" + friendUid + "/" + Uid, null }
            };
            return WithTimeout(root.UpdateChildrenAsync(updates));
        }

        // ------------------------------------------------------------
        // Retos
        // ------------------------------------------------------------
        /// <summary>Envia un reto a un amigo con el codigo de la sala. Si el emisor se desconecta, el reto desaparece solo.</summary>
        public async Task SendChallengeAsync(string toUid, string joinCode)
        {
#if UNITY_EDITOR
            if (mockActive) { await MockSendChallengeAsync(toUid, joinCode); return; }
#endif
            DatabaseReference challenge = root.Child("challenges").Child(toUid).Child(Uid);
            var data = new Dictionary<string, object> { { "name", MyName }, { "code", joinCode } };
            await WithTimeout(challenge.SetValueAsync(data));
            await challenge.OnDisconnect().RemoveValue();
        }

        /// <summary>Retira un reto que yo envie a ese amigo.</summary>
        public async Task CancelChallengeAsync(string toUid)
        {
#if UNITY_EDITOR
            if (mockActive) return;
#endif
            DatabaseReference challenge = root.Child("challenges").Child(toUid).Child(Uid);
            await WithTimeout(challenge.RemoveValueAsync());
            await challenge.OnDisconnect().Cancel();
        }

        /// <summary>Borra un reto que yo recibi (al aceptarlo o rechazarlo).</summary>
        public Task DismissChallengeAsync(string fromUid)
        {
#if UNITY_EDITOR
            if (mockActive) return Task.CompletedTask;
#endif
            return WithTimeout(root.Child("challenges").Child(Uid).Child(fromUid).RemoveValueAsync());
        }

        // ------------------------------------------------------------
        // Victorias y clasificacion (solo partidas online)
        // ------------------------------------------------------------
        /// <summary>
        /// Suma una victoria o derrota a tu marcador general (el de la clasificacion).
        /// Cada jugador apunta SOLO lo suyo en su propia cuenta.
        /// </summary>
        public Task RecordGeneralAsync(bool won)
        {
#if UNITY_EDITOR
            if (mockActive) return MockRecordGeneralAsync(won);
#endif
            if (Uid == null) return Task.CompletedTask;
            return WithTimeout(Increment(root.Child("leaderboard").Child(Uid), won ? "wins" : "losses", MyName));
        }

        /// <summary>Suma una victoria o derrota a tu marcador contra ese rival (si tenia cuenta).</summary>
        public Task RecordVersusAsync(bool won, string opponentUid, string opponentName)
        {
#if UNITY_EDITOR
            if (mockActive) return MockRecordVersusAsync(won, opponentUid);
#endif
            if (Uid == null || string.IsNullOrEmpty(opponentUid) || opponentUid == Uid) return Task.CompletedTask;
            string name = string.IsNullOrWhiteSpace(opponentName) ? "Jugador" : opponentName.Trim();
            if (name.Length > 20) name = name.Substring(0, 20);
            return WithTimeout(Increment(root.Child("vs").Child(Uid).Child(opponentUid), won ? "wins" : "losses", name));
        }

        /// <summary>Suma 1 a "wins" o "losses" con una transaccion (nunca se pisan dos partidas seguidas).</summary>
        private static Task Increment(DatabaseReference target, string field, string name)
        {
            string other = field == "wins" ? "losses" : "wins";
            return target.RunTransaction(data =>
            {
                var map = data.Value as Dictionary<string, object> ?? new Dictionary<string, object>();
                object current;
                map.TryGetValue(field, out current);
                map[field] = ToLong(current) + 1;
                if (!map.ContainsKey(other)) map[other] = 0L;
                map["name"] = name ?? "Jugador";
                data.Value = map;
                return TransactionResult.Success(data);
            });
        }

        /// <summary>Los jugadores con mas victorias online (de mas a menos). Los empates se deciden por menos derrotas.</summary>
        public async Task<List<LeaderboardEntry>> LoadLeaderboardAsync(int limit)
        {
#if UNITY_EDITOR
            if (mockActive) return await MockLoadLeaderboardAsync(limit);
#endif
            DataSnapshot snap = await WithTimeout(root.Child("leaderboard").OrderByChild("wins").LimitToLast(limit).GetValueAsync());
            var list = new List<LeaderboardEntry>();
            foreach (DataSnapshot child in snap.Children)
            {
                var entry = ParseEntry(child.Key, child);
                if (entry.Wins > 0) list.Add(entry);
            }
            list.Sort((a, b) =>
            {
                int result = b.Wins.CompareTo(a.Wins);
                if (result != 0) return result;
                result = a.Losses.CompareTo(b.Losses);
                if (result != 0) return result;
                return string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
            });
            return list;
        }

        private static LeaderboardEntry ParseEntry(string uid, DataSnapshot s)
        {
            return new LeaderboardEntry
            {
                Uid = uid,
                Name = s.Child("name").Value != null ? s.Child("name").Value.ToString() : "Jugador",
                Wins = (int)ToLong(s.Child("wins").Value),
                Losses = (int)ToLong(s.Child("losses").Value)
            };
        }

        private void OnStatsValue(object sender, ValueChangedEventArgs args)
        {
            if (args.DatabaseError != null)
            {
                Debug.LogWarning("[Social] Marcador: " + args.DatabaseError.Message);
                return;
            }
            MyRecord = args.Snapshot.Exists
                ? new RecordInfo { Wins = (int)ToLong(args.Snapshot.Child("wins").Value), Losses = (int)ToLong(args.Snapshot.Child("losses").Value) }
                : new RecordInfo();
            if (RecordChanged != null) RecordChanged(MyRecord);
        }

        private void OnVersusValue(object sender, ValueChangedEventArgs args)
        {
            if (args.DatabaseError != null)
            {
                Debug.LogWarning("[Social] Marcador contra amigos: " + args.DatabaseError.Message);
                return;
            }
            var map = new Dictionary<string, RecordInfo>();
            foreach (DataSnapshot child in args.Snapshot.Children)
            {
                map[child.Key] = new RecordInfo
                {
                    Wins = (int)ToLong(child.Child("wins").Value),
                    Losses = (int)ToLong(child.Child("losses").Value)
                };
            }
            if (VersusChanged != null) VersusChanged(map);
        }

        // ------------------------------------------------------------
        // Progreso de la cuenta
        // ------------------------------------------------------------
        /// <summary>Lee el progreso guardado en la cuenta. Devuelve null si todavia no hay nada.</summary>
        public async Task<SaveData> LoadSaveAsync()
        {
#if UNITY_EDITOR
            if (mockActive) return null;   // el progreso de prueba no se guarda en ningun sitio
#endif
            DataSnapshot snap = await WithTimeout(root.Child("saves").Child(Uid).GetValueAsync());
            return snap.Exists ? ParseSave(snap) : null;
        }

        /// <summary>Guarda el progreso completo en la cuenta.</summary>
        public Task WriteSaveAsync(SaveData data)
        {
#if UNITY_EDITOR
            if (mockActive) return Task.CompletedTask;
#endif
            var map = new Dictionary<string, object>
            {
                { "completed", (long)data.Completed },
                { "coins", (long)data.Coins },
                { "owned", data.Owned ?? "" },
                { "equipped", data.Equipped ?? "" },
                { "tone", (long)data.Tone },
                { "updated", data.Updated }
            };
            return WithTimeout(root.Child("saves").Child(Uid).SetValueAsync(map));
        }

        /// <summary>Avisa (SaveChanged) cada vez que el progreso de la cuenta cambia, tambien desde otro movil.</summary>
        public void ListenSave()
        {
#if UNITY_EDITOR
            if (mockActive) return;
#endif
            StopListenSave();
            saveRef = root.Child("saves").Child(Uid);
            saveRef.ValueChanged += OnSaveValue;
        }

        private void StopListenSave()
        {
            if (saveRef != null) saveRef.ValueChanged -= OnSaveValue;
            saveRef = null;
        }

        private void OnSaveValue(object sender, ValueChangedEventArgs args)
        {
            if (args.DatabaseError != null)
            {
                Debug.LogWarning("[Social] Progreso: " + args.DatabaseError.Message);
                return;
            }
            if (!args.Snapshot.Exists) return;
            if (SaveChanged != null) SaveChanged(ParseSave(args.Snapshot));
        }

        private static SaveData ParseSave(DataSnapshot s)
        {
            return new SaveData
            {
                Completed = (int)ToLong(s.Child("completed").Value),
                Coins = (int)ToLong(s.Child("coins").Value),
                Owned = s.Child("owned").Value != null ? s.Child("owned").Value.ToString() : "",
                Equipped = s.Child("equipped").Value != null ? s.Child("equipped").Value.ToString() : "",
                Tone = (int)ToLong(s.Child("tone").Value),
                Updated = ToLong(s.Child("updated").Value)
            };
        }

        private static long ToLong(object value)
        {
            if (value == null) return 0;
            try { return Convert.ToInt64(value); }
            catch (Exception) { return 0; }
        }

        // ------------------------------------------------------------
        // Escuchar cambios en tiempo real
        // ------------------------------------------------------------
        private void StartListening()
        {
            StopListening();
            string uid = Uid;
            friendsRef = root.Child("friends").Child(uid);
            requestsRef = root.Child("friendRequests").Child(uid);
            challengesRef = root.Child("challenges").Child(uid);
            friendsRef.ValueChanged += OnFriendsValue;
            requestsRef.ValueChanged += OnRequestsValue;
            challengesRef.ChildAdded += OnChallengeAdded;
            statsRef = root.Child("leaderboard").Child(uid);
            versusRef = root.Child("vs").Child(uid);
            statsRef.ValueChanged += OnStatsValue;
            versusRef.ValueChanged += OnVersusValue;
        }

        public void StopListening()
        {
            StopListenSave();
            if (statsRef != null) statsRef.ValueChanged -= OnStatsValue;
            if (versusRef != null) versusRef.ValueChanged -= OnVersusValue;
            statsRef = null;
            versusRef = null;
            if (friendsRef != null) friendsRef.ValueChanged -= OnFriendsValue;
            if (requestsRef != null) requestsRef.ValueChanged -= OnRequestsValue;
            if (challengesRef != null) challengesRef.ChildAdded -= OnChallengeAdded;
            friendsRef = null;
            requestsRef = null;
            challengesRef = null;
        }

        private async void OnFriendsValue(object sender, ValueChangedEventArgs args)
        {
            if (args.DatabaseError != null)
            {
                Debug.LogWarning("[Social] Amigos: " + args.DatabaseError.Message);
                return;
            }

            int version = ++friendsVersion;
            var children = new List<DataSnapshot>(args.Snapshot.Children);
            var list = new List<FriendInfo>();
            foreach (DataSnapshot child in children)
            {
                string name = "Jugador";
                try
                {
                    DataSnapshot n = await WithTimeout(root.Child("users").Child(child.Key).Child("name").GetValueAsync());
                    if (n.Exists && n.Value != null) name = n.Value.ToString();
                }
                catch (Exception e)
                {
                    Debug.LogWarning("[Social] Nombre de amigo: " + e.Message);
                }
                list.Add(new FriendInfo { Uid = child.Key, Name = name });
            }

            if (version != friendsVersion) return;   // llego una actualizacion mas nueva mientras se leian los nombres
            list.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
            if (FriendsChanged != null) FriendsChanged(list);
        }

        private void OnRequestsValue(object sender, ValueChangedEventArgs args)
        {
            if (args.DatabaseError != null)
            {
                Debug.LogWarning("[Social] Peticiones: " + args.DatabaseError.Message);
                return;
            }

            var list = new List<FriendRequestInfo>();
            foreach (DataSnapshot child in args.Snapshot.Children)
            {
                string name = child.Child("name").Value != null ? child.Child("name").Value.ToString() : "Jugador";
                list.Add(new FriendRequestInfo { Uid = child.Key, Name = name });
            }
            if (RequestsChanged != null) RequestsChanged(list);
        }

        private void OnChallengeAdded(object sender, ChildChangedEventArgs args)
        {
            if (args.DatabaseError != null)
            {
                Debug.LogWarning("[Social] Retos: " + args.DatabaseError.Message);
                return;
            }

            DataSnapshot s = args.Snapshot;
            if (s.Child("code").Value == null) return;
            var challenge = new ChallengeInfo
            {
                FromUid = s.Key,
                FromName = s.Child("name").Value != null ? s.Child("name").Value.ToString() : "Un amigo",
                JoinCode = s.Child("code").Value.ToString()
            };
            if (ChallengeReceived != null) ChallengeReceived(challenge);
        }

        // ------------------------------------------------------------
        // Ayudas
        // ------------------------------------------------------------
        private static async Task<T> WithTimeout<T>(Task<T> task)
        {
            Task done = await Task.WhenAny(task, Task.Delay(TimeoutMs));
            if (done != task) throw new SocialException("El servidor no responde. Revisa tu conexión.");
            return await task;
        }

        private static async Task WithTimeout(Task task)
        {
            Task done = await Task.WhenAny(task, Task.Delay(TimeoutMs));
            if (done != task) throw new SocialException("El servidor no responde. Revisa tu conexión.");
            await task;
        }
    }
#else
    /// <summary>La version web no usa Firebase (su SDK no funciona en WebGL): los amigos no estan disponibles.</summary>
    public class SocialBackend
    {
        public const bool IsSupported = false;
        private const string Unsupported = "Los amigos no están disponibles en la versión web.";

#pragma warning disable 0067
        public event Action SignInChanged;
        public event Action<List<FriendInfo>> FriendsChanged;
        public event Action<List<FriendRequestInfo>> RequestsChanged;
        public event Action<ChallengeInfo> ChallengeReceived;
        public event Action<SaveData> SaveChanged;
        public event Action<RecordInfo> RecordChanged;
        public event Action<Dictionary<string, RecordInfo>> VersusChanged;
#pragma warning restore 0067

        public bool Ready { get { return false; } }
        public bool IsSignedIn { get { return false; } }
        public string Uid { get { return null; } }
        public string MyName { get { return null; } }
        public string MyCode { get { return null; } }
        public RecordInfo MyRecord { get { return new RecordInfo(); } }

        public Task InitAsync() { return Task.FromException(new SocialException(Unsupported)); }
        public Task SignInWithGoogleAsync() { return Task.FromException(new SocialException(Unsupported)); }
        public Task UpdateNameAsync(string name) { return Task.FromException(new SocialException(Unsupported)); }
        public void SignOut() { }
        public void StopListening() { }
        public Task<string> SendFriendRequestAsync(string code) { return Task.FromException<string>(new SocialException(Unsupported)); }
        public Task AcceptRequestAsync(string fromUid) { return Task.FromException(new SocialException(Unsupported)); }
        public Task DeclineRequestAsync(string fromUid) { return Task.FromException(new SocialException(Unsupported)); }
        public Task RemoveFriendAsync(string friendUid) { return Task.FromException(new SocialException(Unsupported)); }
        public Task SendChallengeAsync(string toUid, string joinCode) { return Task.FromException(new SocialException(Unsupported)); }
        public Task CancelChallengeAsync(string toUid) { return Task.FromException(new SocialException(Unsupported)); }
        public Task DismissChallengeAsync(string fromUid) { return Task.FromException(new SocialException(Unsupported)); }
        public Task<SaveData> LoadSaveAsync() { return Task.FromException<SaveData>(new SocialException(Unsupported)); }
        public Task WriteSaveAsync(SaveData data) { return Task.FromException(new SocialException(Unsupported)); }
        public void ListenSave() { }
        public Task RecordGeneralAsync(bool won) { return Task.FromException(new SocialException(Unsupported)); }
        public Task RecordVersusAsync(bool won, string opponentUid, string opponentName) { return Task.FromException(new SocialException(Unsupported)); }
        public Task<List<LeaderboardEntry>> LoadLeaderboardAsync(int limit) { return Task.FromException<List<LeaderboardEntry>>(new SocialException(Unsupported)); }
    }
#endif
}
