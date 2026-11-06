using System;
using System.Collections.Generic;
using Chopsticks.Social;
using UnityEngine;
using UnityEngine.UIElements;

namespace Chopsticks.UI
{
    /// <summary>
    /// Pantalla de amigos. Sin sesion: boton de Google. Con sesion: perfil con tu codigo, acceso a la Clasificacion,
    /// anadir amigo por codigo, peticiones recibidas y lista de amigos con boton Retar.
    /// La estructura esta en Friends.uxml y el aspecto en Social.uss; aqui solo se rellenan datos y se avisa con eventos.
    /// No se conecta a Firebase.
    /// </summary>
    public class FriendsView
    {
        private readonly VisualElement signedOutPanel;
        private readonly VisualElement signedInPanel;
        private readonly Button googleButton;
        private readonly Button copyButton;
        private readonly Button addButton;
        private readonly Button leaderboardButton;
        private readonly Label hintLabel;
        private readonly Label messageLabel;
        private readonly Label nameLabel;
        private readonly Label initialLabel;
        private readonly Label codeLabel;
        private readonly VisualElement avatar;
        private readonly VisualElement chipsBox;
        private readonly TextField friendCodeField;
        private readonly VisualElement requestsBox;
        private readonly VisualElement friendsBox;

        private List<FriendInfo> friends = new List<FriendInfo>();
        private Dictionary<string, RecordInfo> records = new Dictionary<string, RecordInfo>();   // tu marcador contra cada rival (clave = uid)
        private string myName = "";
        private string myCode = "";

        public event Action GoogleClicked;
        public event Action<string> AddClicked;               // (codigo)
        public event Action<FriendRequestInfo> AcceptClicked;
        public event Action<FriendRequestInfo> DeclineClicked;
        public event Action<FriendInfo> ChallengeClicked;
        public event Action<FriendInfo> RemoveClicked;
        public event Action LeaderboardClicked;

        public FriendsView(VisualElement screen)
        {
            signedOutPanel = screen.Q("signed-out");
            signedInPanel = screen.Q("signed-in");
            googleButton = screen.Q<Button>("btn-google");
            copyButton = screen.Q<Button>("btn-copy");
            addButton = screen.Q<Button>("btn-add");
            leaderboardButton = screen.Q<Button>("btn-leaderboard");
            hintLabel = screen.Q<Label>("friends-hint");
            messageLabel = screen.Q<Label>("friends-msg");
            nameLabel = screen.Q<Label>("me-name");
            initialLabel = screen.Q<Label>("me-initial");
            codeLabel = screen.Q<Label>("my-code");
            avatar = screen.Q("me-avatar");
            chipsBox = screen.Q("me-chips");
            friendCodeField = screen.Q<TextField>("friend-code");
            UppercaseField.Apply(friendCodeField);   // los codigos siempre son en mayusculas

            ScrollView list = screen.Q<ScrollView>("friends-list");
            requestsBox = new VisualElement();
            friendsBox = new VisualElement();
            list.Add(requestsBox);
            list.Add(friendsBox);

            googleButton.clicked += () => { if (GoogleClicked != null) GoogleClicked(); };
            addButton.clicked += OnAdd;
            copyButton.clicked += OnCopy;
            leaderboardButton.clicked += () => { if (LeaderboardClicked != null) LeaderboardClicked(); };

            SetMyRecord(new RecordInfo());
            SetRequests(new List<FriendRequestInfo>());
            SetFriends(new List<FriendInfo>());
        }

        // ------------------------------------------------------------
        // Estado de la sesion
        // ------------------------------------------------------------
        /// <summary>Sin sesion. "ready" = Firebase listo; si no, el boton espera o se explica por que.</summary>
        public void ShowSignedOut(bool ready, string hint)
        {
            signedOutPanel.style.display = DisplayStyle.Flex;
            signedInPanel.style.display = DisplayStyle.None;
            googleButton.SetEnabled(ready);
            hintLabel.text = hint;
        }

        public void ShowSignedIn(string name, string code)
        {
            signedOutPanel.style.display = DisplayStyle.None;
            signedInPanel.style.display = DisplayStyle.Flex;
            myName = string.IsNullOrEmpty(name) ? "Jugador" : name;
            myCode = code ?? "";
            nameLabel.text = myName;
            SocialStyle.SetAvatar(avatar, initialLabel, myName);
            codeLabel.text = myCode.Length > 0 ? myCode : "------";
        }

        /// <summary>Aviso debajo del titulo (vacio = nada). Los errores salen en rojo.</summary>
        public void ShowMessage(string text, bool isError)
        {
            messageLabel.text = text;
            messageLabel.EnableInClassList("friends__msg--error", isError);
        }

        public void SetBusy(bool busy)
        {
            googleButton.SetEnabled(!busy);
            addButton.SetEnabled(!busy);
        }

        // ------------------------------------------------------------
        // Datos
        // ------------------------------------------------------------
        /// <summary>Tus victorias y derrotas online en total.</summary>
        public void SetMyRecord(RecordInfo record)
        {
            chipsBox.Clear();
            if (record == null || record.Played == 0)
            {
                chipsBox.Add(SocialStyle.Chip("Sin partidas online"));
                return;
            }
            chipsBox.Add(SocialStyle.Chip(SocialStyle.Plural(record.Wins, "ganada", "ganadas"), "chip--win"));
            chipsBox.Add(SocialStyle.Chip(SocialStyle.Plural(record.Losses, "perdida", "perdidas"), "chip--lose"));
        }

        public void SetRequests(List<FriendRequestInfo> requests)
        {
            requestsBox.Clear();
            requestsBox.style.display = requests.Count > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            if (requests.Count == 0) return;

            requestsBox.Add(SectionTitle("Peticiones", requests.Count, true));
            foreach (FriendRequestInfo request in requests)
            {
                FriendRequestInfo r = request;
                VisualElement actions;
                VisualElement row = Row(r.Name, "quiere ser tu amigo", null, true, out actions);

                Button accept = new Button();
                accept.text = "Aceptar";
                accept.AddToClassList("btn");
                accept.AddToClassList("btn--green");
                accept.AddToClassList("btn--xs");
                accept.clicked += () => { if (AcceptClicked != null) AcceptClicked(r); };
                actions.Add(accept);

                Button decline = LinkButton("Rechazar");
                decline.clicked += () => { if (DeclineClicked != null) DeclineClicked(r); };
                actions.Add(decline);

                requestsBox.Add(row);
            }
        }

        /// <summary>Tu marcador contra cada rival (clave = uid). Se ensena en la fila de cada amigo.</summary>
        public void SetRecords(Dictionary<string, RecordInfo> map)
        {
            records = map ?? new Dictionary<string, RecordInfo>();
            SetFriends(friends);
        }

        public void SetFriends(List<FriendInfo> list)
        {
            friends = list ?? new List<FriendInfo>();
            friendsBox.Clear();
            friendsBox.Add(SectionTitle("Amigos", friends.Count, false));

            if (friends.Count == 0)
            {
                var empty = new Label("Todavía no tienes amigos. Comparte tu código o escribe el de un amigo para retarle.");
                empty.AddToClassList("friends__hint");
                friendsBox.Add(empty);
                return;
            }

            foreach (FriendInfo friend in friends)
            {
                FriendInfo f = friend;
                string recordText;
                string recordClass;
                DescribeRecord(f.Uid, out recordText, out recordClass);

                VisualElement actions;
                VisualElement row = Row(f.Name, recordText, recordClass, false, out actions);

                Button challenge = new Button();
                challenge.text = "Retar";
                challenge.AddToClassList("btn");
                challenge.AddToClassList("btn--xs");
                challenge.clicked += () => { if (ChallengeClicked != null) ChallengeClicked(f); };
                actions.Add(challenge);
                actions.Add(RemoveButton(f));

                friendsBox.Add(row);
            }
        }

        /// <summary>Vacia el campo del codigo (tras enviar una peticion).</summary>
        public void ClearCodeField()
        {
            friendCodeField.value = "";
        }

        // ------------------------------------------------------------
        // Piezas
        // ------------------------------------------------------------
        private static VisualElement SectionTitle(string text, int count, bool highlight)
        {
            var row = new VisualElement();
            row.AddToClassList("section-row");

            var title = new Label(text.ToUpperInvariant());
            title.AddToClassList("section-title");
            row.Add(title);

            var badge = new Label(count.ToString());
            badge.AddToClassList("section-row__badge");
            if (!highlight) badge.AddToClassList("section-row__badge--plain");
            row.Add(badge);
            return row;
        }

        /// <summary>Fila: avatar, nombre y una linea debajo; "actions" es la columna de botones de la derecha.</summary>
        private static VisualElement Row(string name, string subtitle, string subtitleClass, bool accent, out VisualElement actions)
        {
            var row = new VisualElement();
            row.AddToClassList("row-card");
            if (accent) row.AddToClassList("row-card--accent");

            row.Add(SocialStyle.Avatar(name));

            var info = new VisualElement();
            info.AddToClassList("row-card__info");
            var nameLabel = new Label(name);
            nameLabel.AddToClassList("row-card__name");
            info.Add(nameLabel);
            var sub = new Label(subtitle);
            sub.AddToClassList("row-card__sub");
            if (subtitleClass != null) sub.AddToClassList(subtitleClass);
            info.Add(sub);
            row.Add(info);

            actions = new VisualElement();
            actions.AddToClassList("row-card__actions");
            row.Add(actions);
            return row;
        }

        private static Button LinkButton(string text)
        {
            var button = new Button();
            button.text = text;
            button.AddToClassList("btn-link");
            return button;
        }

        /// <summary>"Quitar" pide confirmacion: el primer toque cambia a "¿Seguro?" y el segundo lo borra.</summary>
        private Button RemoveButton(FriendInfo friend)
        {
            Button button = LinkButton("Quitar");
            bool armed = false;
            button.clicked += () =>
            {
                if (!armed)
                {
                    armed = true;
                    button.text = "¿Seguro?";
                    button.AddToClassList("btn-link--armed");
                    button.schedule.Execute(() =>
                    {
                        armed = false;
                        button.text = "Quitar";
                        button.RemoveFromClassList("btn-link--armed");
                    }).StartingIn(3000);
                    return;
                }
                if (RemoveClicked != null) RemoveClicked(friend);
            };
            return button;
        }

        /// <summary>Texto "Tú: 3 ganadas · 2 perdidas" contra ese amigo, y un color si vas ganando o perdiendo.</summary>
        private void DescribeRecord(string uid, out string text, out string cssClass)
        {
            RecordInfo record;
            cssClass = null;
            if (!records.TryGetValue(uid, out record) || record.Played == 0)
            {
                text = "Aún no habéis jugado online";
                return;
            }
            text = "Tú: " + record.Wins + " ganadas · " + record.Losses + " perdidas";
            if (record.Wins > record.Losses) cssClass = "row-card__sub--ahead";
            else if (record.Wins < record.Losses) cssClass = "row-card__sub--behind";
        }

        // ------------------------------------------------------------
        // Acciones
        // ------------------------------------------------------------
        private void OnAdd()
        {
            string code = friendCodeField.value.Trim();
            if (code.Length == 0)
            {
                ShowMessage("Escribe el código de tu amigo.", true);
                return;
            }
            if (AddClicked != null) AddClicked(code);
        }

        private void OnCopy()
        {
            if (myCode.Length == 0) return;
            GUIUtility.systemCopyBuffer = myCode;
            ShowMessage("Código copiado.", false);
        }
    }
}
