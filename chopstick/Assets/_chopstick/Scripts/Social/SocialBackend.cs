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
    /// Las reglas de seguridad estan en Firebase/database.rules.json.
    /// </summary>
    public class SocialBackend
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
        private int friendsVersion;

        /// <summary>Firebase esta listo (dependencias comprobadas).</summary>
        public bool Ready { get; private set; }
        public bool IsSignedIn { get { return auth != null && auth.CurrentUser != null; } }
        public string Uid { get { return IsSignedIn ? auth.CurrentUser.UserId : null; } }
        public string MyName { get; private set; }
        public string MyCode { get; private set; }

        public event Action SignInChanged;
        public event Action<List<FriendInfo>> FriendsChanged;
        public event Action<List<FriendRequestInfo>> RequestsChanged;
        public event Action<ChallengeInfo> ChallengeReceived;

        // ------------------------------------------------------------
        // Inicio y sesion
        // ------------------------------------------------------------
        /// <summary>Arranca Firebase. Si ya habia una sesion guardada en el dispositivo, la recupera.</summary>
        public async Task InitAsync()
        {
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
            StopListening();
            if (auth != null) auth.SignOut();
            MyName = null;
            MyCode = null;
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
                await WithTimeout(userRef.Child("name").SetValueAsync(MyName));
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
            return WithTimeout(root.Child("friendRequests").Child(Uid).Child(fromUid).RemoveValueAsync());
        }

        public Task RemoveFriendAsync(string friendUid)
        {
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
            DatabaseReference challenge = root.Child("challenges").Child(toUid).Child(Uid);
            var data = new Dictionary<string, object> { { "name", MyName }, { "code", joinCode } };
            await WithTimeout(challenge.SetValueAsync(data));
            await challenge.OnDisconnect().RemoveValue();
        }

        /// <summary>Retira un reto que yo envie a ese amigo.</summary>
        public async Task CancelChallengeAsync(string toUid)
        {
            DatabaseReference challenge = root.Child("challenges").Child(toUid).Child(Uid);
            await WithTimeout(challenge.RemoveValueAsync());
            await challenge.OnDisconnect().Cancel();
        }

        /// <summary>Borra un reto que yo recibi (al aceptarlo o rechazarlo).</summary>
        public Task DismissChallengeAsync(string fromUid)
        {
            return WithTimeout(root.Child("challenges").Child(Uid).Child(fromUid).RemoveValueAsync());
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
        }

        public void StopListening()
        {
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
#pragma warning restore 0067

        public bool Ready { get { return false; } }
        public bool IsSignedIn { get { return false; } }
        public string Uid { get { return null; } }
        public string MyName { get { return null; } }
        public string MyCode { get { return null; } }

        public Task InitAsync() { return Task.FromException(new SocialException(Unsupported)); }
        public Task SignInWithGoogleAsync() { return Task.FromException(new SocialException(Unsupported)); }
        public void SignOut() { }
        public void StopListening() { }
        public Task<string> SendFriendRequestAsync(string code) { return Task.FromException<string>(new SocialException(Unsupported)); }
        public Task AcceptRequestAsync(string fromUid) { return Task.FromException(new SocialException(Unsupported)); }
        public Task DeclineRequestAsync(string fromUid) { return Task.FromException(new SocialException(Unsupported)); }
        public Task RemoveFriendAsync(string friendUid) { return Task.FromException(new SocialException(Unsupported)); }
        public Task SendChallengeAsync(string toUid, string joinCode) { return Task.FromException(new SocialException(Unsupported)); }
        public Task CancelChallengeAsync(string toUid) { return Task.FromException(new SocialException(Unsupported)); }
        public Task DismissChallengeAsync(string fromUid) { return Task.FromException(new SocialException(Unsupported)); }
    }
#endif
}
