using System;
using System.Collections.Generic;
using Chopsticks.Social;
using UnityEngine;
using UnityEngine.UIElements;

namespace Chopsticks.UI
{
    /// <summary>
    /// Pantalla de amigos. Sin sesion: boton de Google. Con sesion: tu codigo, anadir amigo por codigo,
    /// peticiones recibidas y lista de amigos con boton Retar.
    /// Solo dibuja y avisa con eventos; no se conecta a Firebase.
    /// </summary>
    public class FriendsView
    {
        private readonly VisualElement signedOutPanel;
        private readonly VisualElement signedInPanel;
        private readonly Button googleButton;
        private readonly Button signOutButton;
        private readonly Button copyButton;
        private readonly Button addButton;
        private readonly Label hintLabel;
        private readonly Label messageLabel;
        private readonly Label meLabel;
        private readonly Label codeLabel;
        private readonly TextField friendCodeField;
        private readonly VisualElement requestsBox;
        private readonly VisualElement friendsBox;
        private string myCode = "";

        public event Action GoogleClicked;
        public event Action SignOutClicked;
        public event Action<string> AddClicked;               // (codigo)
        public event Action<FriendRequestInfo> AcceptClicked;
        public event Action<FriendRequestInfo> DeclineClicked;
        public event Action<FriendInfo> ChallengeClicked;
        public event Action<FriendInfo> RemoveClicked;

        public FriendsView(VisualElement screen)
        {
            signedOutPanel = screen.Q("signed-out");
            signedInPanel = screen.Q("signed-in");
            googleButton = screen.Q<Button>("btn-google");
            copyButton = screen.Q<Button>("btn-copy");
            addButton = screen.Q<Button>("btn-add");
            hintLabel = screen.Q<Label>("friends-hint");
            messageLabel = screen.Q<Label>("friends-msg");
            meLabel = screen.Q<Label>("me-name");
            codeLabel = screen.Q<Label>("my-code");
            friendCodeField = screen.Q("friend-code").Q<TextField>("input");
            signOutButton = screen.Q("action-row").Q<Button>("btn-primary");

            ScrollView list = screen.Q<ScrollView>("friends-list");
            requestsBox = new VisualElement();
            friendsBox = new VisualElement();
            list.Add(requestsBox);
            list.Add(friendsBox);

            googleButton.clicked += () => { if (GoogleClicked != null) GoogleClicked(); };
            signOutButton.clicked += () => { if (SignOutClicked != null) SignOutClicked(); };
            addButton.clicked += OnAdd;
            copyButton.clicked += OnCopy;

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
            signOutButton.style.display = DisplayStyle.None;
            googleButton.SetEnabled(ready);
            hintLabel.text = hint;
        }

        public void ShowSignedIn(string name, string code)
        {
            signedOutPanel.style.display = DisplayStyle.None;
            signedInPanel.style.display = DisplayStyle.Flex;
            signOutButton.style.display = DisplayStyle.Flex;
            signOutButton.text = "Cerrar sesión";
            meLabel.text = "Hola, " + name;
            myCode = code ?? "";
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
        // Listas
        // ------------------------------------------------------------
        public void SetRequests(List<FriendRequestInfo> requests)
        {
            requestsBox.Clear();
            requestsBox.style.display = requests.Count > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            if (requests.Count == 0) return;

            requestsBox.Add(Title("Peticiones (" + requests.Count + ")"));
            foreach (FriendRequestInfo request in requests)
            {
                FriendRequestInfo r = request;
                VisualElement row = Row(r.Name);
                AddMini(row, "Aceptar", "mini-btn--ok", () => { if (AcceptClicked != null) AcceptClicked(r); });
                AddMini(row, "Rechazar", "mini-btn--no", () => { if (DeclineClicked != null) DeclineClicked(r); });
                requestsBox.Add(row);
            }
        }

        public void SetFriends(List<FriendInfo> friends)
        {
            friendsBox.Clear();
            friendsBox.Add(Title("Amigos (" + friends.Count + ")"));

            if (friends.Count == 0)
            {
                var empty = new Label("Todavía no tienes amigos. Comparte tu código o escribe el de un amigo.");
                empty.AddToClassList("friends__hint");
                friendsBox.Add(empty);
                return;
            }

            foreach (FriendInfo friend in friends)
            {
                FriendInfo f = friend;
                VisualElement row = Row(f.Name);
                AddMini(row, "Retar", "mini-btn--fight", () => { if (ChallengeClicked != null) ChallengeClicked(f); });
                AddRemove(row, f);
                friendsBox.Add(row);
            }
        }

        // ------------------------------------------------------------
        // Piezas
        // ------------------------------------------------------------
        private static Label Title(string text)
        {
            var label = new Label(text);
            label.AddToClassList("career__tier");
            return label;
        }

        private static VisualElement Row(string name)
        {
            var row = new VisualElement();
            row.AddToClassList("friend-row");
            var label = new Label(name);
            label.AddToClassList("friend-row__name");
            row.Add(label);
            return row;
        }

        private static Button AddMini(VisualElement row, string text, string colorClass, Action click)
        {
            var button = new Button();
            button.text = text;
            button.AddToClassList("mini-btn");
            button.AddToClassList(colorClass);
            button.clicked += click;
            row.Add(button);
            return button;
        }

        /// <summary>"Quitar" pide confirmacion: el primer toque cambia a "¿Seguro?" y el segundo lo borra.</summary>
        private void AddRemove(VisualElement row, FriendInfo friend)
        {
            Button button = null;
            bool armed = false;
            button = AddMini(row, "Quitar", "mini-btn--ghost", () =>
            {
                if (!armed)
                {
                    armed = true;
                    button.text = "¿Seguro?";
                    button.schedule.Execute(() => { armed = false; button.text = "Quitar"; }).StartingIn(3000);
                    return;
                }
                if (RemoveClicked != null) RemoveClicked(friend);
            });
        }

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

        /// <summary>Vacia el campo del codigo (tras enviar una peticion).</summary>
        public void ClearCodeField()
        {
            friendCodeField.value = "";
        }

        private void OnCopy()
        {
            if (myCode.Length == 0) return;
            GUIUtility.systemCopyBuffer = myCode;
            ShowMessage("Código copiado.", false);
        }
    }
}
