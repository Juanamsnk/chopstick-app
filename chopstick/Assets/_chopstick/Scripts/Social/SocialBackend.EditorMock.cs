#if UNITY_EDITOR && !UNITY_WEBGL
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Chopsticks.Social
{
    /// <summary>
    /// SOLO EN EL EDITOR: amigos, peticiones y clasificacion de mentira, en memoria, sin Firebase ni inicio de sesion.
    /// Sirve para ver y probar las pantallas sin tocar la base de datos real.
    /// Se activa y desactiva en el menu  Chopsticks > Amigos de prueba en el editor  (por defecto, activado).
    /// No existe en las builds del movil.
    ///
    /// Para probar "Añadir amigo" con el codigo de otra persona, escribe  VALER2.
    /// </summary>
    public partial class SocialBackend
    {
        internal const string MockPrefKey = "Chopsticks.SocialMock";
        private const int MockDelayMs = 250;     // pequena espera para ver los estados de "cargando"

        private bool mockActive;                 // esta sesion del editor usa los datos de prueba
        private bool mockSignedIn;

        private readonly Dictionary<string, string> mockNames = new Dictionary<string, string>();
        private readonly Dictionary<string, string> mockCodes = new Dictionary<string, string>();       // codigo -> uid
        private readonly HashSet<string> mockFriendUids = new HashSet<string>();
        private readonly List<FriendRequestInfo> mockRequests = new List<FriendRequestInfo>();
        private readonly Dictionary<string, RecordInfo> mockVersus = new Dictionary<string, RecordInfo>();
        private readonly Dictionary<string, RecordInfo> mockBoard = new Dictionary<string, RecordInfo>();   // victorias y derrotas de los demas

        private static bool MockEnabled
        {
            get { return EditorPrefs.GetBool(MockPrefKey, true); }
        }

        // ------------------------------------------------------------
        // Sesion
        // ------------------------------------------------------------
        private void MockSignIn()
        {
            MockBuildData();
            MyName = "Juan Manuel Rivas";
            MyCode = "ZAPSW7";
            MyRecord = new RecordInfo { Wins = 7, Losses = 3 };
            mockSignedIn = true;

            if (SignInChanged != null) SignInChanged();
            MockRaiseAll();
        }

        private void MockAddPerson(string uid, string name, int wins, int losses, string code = null)
        {
            mockNames[uid] = name;
            mockBoard[uid] = new RecordInfo { Wins = wins, Losses = losses };
            if (code != null) mockCodes[code] = uid;
        }

        private void MockBuildData()
        {
            mockNames.Clear();
            mockCodes.Clear();
            mockFriendUids.Clear();
            mockRequests.Clear();
            mockVersus.Clear();
            mockBoard.Clear();

            MockAddPerson("mock-1", "Lucía", 14, 5);
            MockAddPerson("mock-2", "Marcos", 11, 9);
            MockAddPerson("mock-3", "Álvaro Fernández-Quintana", 9, 3);   // nombre largo, para ver como se recorta
            MockAddPerson("mock-4", "Sofía", 7, 7);
            MockAddPerson("mock-5", "Pablo", 6, 2);
            MockAddPerson("mock-6", "Carmen", 3, 4);
            MockAddPerson("mock-7", "Diego", 2, 6);
            MockAddPerson("mock-8", "Elena", 5, 5);
            MockAddPerson("mock-9", "Nacho", 1, 0);
            MockAddPerson("mock-10", "Valeria", 8, 2, "VALER2");
            MockAddPerson("mock-11", "Isabel", 12, 1);

            for (int i = 1; i <= 7; i++) mockFriendUids.Add("mock-" + i);
            mockRequests.Add(new FriendRequestInfo { Uid = "mock-8", Name = "Elena" });
            mockRequests.Add(new FriendRequestInfo { Uid = "mock-9", Name = "Nacho" });

            mockVersus["mock-1"] = new RecordInfo { Wins = 3, Losses = 1 };   // vas ganando
            mockVersus["mock-2"] = new RecordInfo { Wins = 1, Losses = 2 };   // vas perdiendo
            mockVersus["mock-3"] = new RecordInfo { Wins = 2, Losses = 2 };   // empate
        }

        private List<FriendInfo> MockFriendList()
        {
            var list = new List<FriendInfo>();
            foreach (string uid in mockFriendUids) list.Add(new FriendInfo { Uid = uid, Name = mockNames[uid] });
            list.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
            return list;
        }

        private void MockRaiseFriends()
        {
            if (FriendsChanged != null) FriendsChanged(MockFriendList());
        }

        private void MockRaiseRequests()
        {
            if (RequestsChanged != null) RequestsChanged(new List<FriendRequestInfo>(mockRequests));
        }

        private void MockRaiseAll()
        {
            if (RecordChanged != null) RecordChanged(MyRecord);
            if (VersusChanged != null) VersusChanged(new Dictionary<string, RecordInfo>(mockVersus));
            MockRaiseFriends();
            MockRaiseRequests();
        }

        // ------------------------------------------------------------
        // Amigos
        // ------------------------------------------------------------
        private async Task<string> MockSendFriendRequestAsync(string code)
        {
            await Task.Delay(MockDelayMs);
            code = (code ?? "").Trim().ToUpperInvariant();
            if (code.Length != CodeLength) throw new SocialException("El código tiene " + CodeLength + " caracteres.");
            if (code == MyCode) throw new SocialException("Ese es tu propio código.");

            string uid;
            if (!mockCodes.TryGetValue(code, out uid))
                throw new SocialException("No existe ningún jugador con ese código. (Modo de prueba: usa VALER2)");
            if (mockFriendUids.Contains(uid)) throw new SocialException("Ya sois amigos.");
            return mockNames[uid];
        }

        private async Task MockAcceptRequestAsync(string fromUid)
        {
            await Task.Delay(MockDelayMs);
            mockRequests.RemoveAll(r => r.Uid == fromUid);
            mockFriendUids.Add(fromUid);
            MockRaiseRequests();
            MockRaiseFriends();
        }

        private async Task MockDeclineRequestAsync(string fromUid)
        {
            await Task.Delay(MockDelayMs);
            mockRequests.RemoveAll(r => r.Uid == fromUid);
            MockRaiseRequests();
        }

        private async Task MockRemoveFriendAsync(string friendUid)
        {
            await Task.Delay(MockDelayMs);
            mockFriendUids.Remove(friendUid);
            MockRaiseFriends();
        }

        private async Task MockSendChallengeAsync(string toUid, string joinCode)
        {
            await Task.Delay(MockDelayMs);
            string name;
            mockNames.TryGetValue(toUid, out name);
            Debug.Log("[Social/Prueba] Reto enviado a " + name + " (sala " + joinCode + ").");
        }

        // ------------------------------------------------------------
        // Victorias y clasificacion
        // ------------------------------------------------------------
        private Task MockRecordGeneralAsync(bool won)
        {
            MyRecord = new RecordInfo { Wins = MyRecord.Wins + (won ? 1 : 0), Losses = MyRecord.Losses + (won ? 0 : 1) };
            if (RecordChanged != null) RecordChanged(MyRecord);
            return Task.CompletedTask;
        }

        private Task MockRecordVersusAsync(bool won, string opponentUid)
        {
            RecordInfo current;
            if (!mockVersus.TryGetValue(opponentUid, out current)) current = new RecordInfo();
            mockVersus[opponentUid] = new RecordInfo { Wins = current.Wins + (won ? 1 : 0), Losses = current.Losses + (won ? 0 : 1) };
            if (VersusChanged != null) VersusChanged(new Dictionary<string, RecordInfo>(mockVersus));
            return Task.CompletedTask;
        }

        private async Task<List<LeaderboardEntry>> MockLoadLeaderboardAsync(int limit)
        {
            await Task.Delay(MockDelayMs);

            var list = new List<LeaderboardEntry>();
            foreach (KeyValuePair<string, RecordInfo> pair in mockBoard)
                list.Add(new LeaderboardEntry { Uid = pair.Key, Name = mockNames[pair.Key], Wins = pair.Value.Wins, Losses = pair.Value.Losses });
            list.Add(new LeaderboardEntry { Uid = Uid, Name = MyName, Wins = MyRecord.Wins, Losses = MyRecord.Losses });
            list.RemoveAll(e => e.Wins <= 0);

            list.Sort((a, b) =>
            {
                int result = b.Wins.CompareTo(a.Wins);
                if (result != 0) return result;
                result = a.Losses.CompareTo(b.Losses);
                if (result != 0) return result;
                return string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
            });
            if (list.Count > limit) list.RemoveRange(limit, list.Count - limit);
            return list;
        }
    }

    /// <summary>Menu del editor para activar / desactivar los datos de prueba (necesita volver a dar Play).</summary>
    public static class SocialMockMenu
    {
        private const string MenuPath = "Chopsticks/Amigos de prueba en el editor";

        [MenuItem(MenuPath)]
        private static void Toggle()
        {
            bool on = !EditorPrefs.GetBool(SocialBackend.MockPrefKey, true);
            EditorPrefs.SetBool(SocialBackend.MockPrefKey, on);
            Debug.Log(on
                ? "[Social] Amigos de prueba ACTIVADOS (sin Firebase). Vuelve a dar Play."
                : "[Social] Amigos de prueba DESACTIVADOS: el editor usará Firebase de verdad. Vuelve a dar Play.");
        }

        [MenuItem(MenuPath, true)]
        private static bool ToggleValidate()
        {
            Menu.SetChecked(MenuPath, EditorPrefs.GetBool(SocialBackend.MockPrefKey, true));
            return true;
        }
    }
}
#endif
