using System;
using UnityEngine.UIElements;

namespace Chopsticks.UI
{
    /// <summary>
    /// Pantalla del menu online: nombre, crear partida o unirse con un codigo.
    /// Solo lee los campos y avisa con eventos; no se conecta a nada.
    /// </summary>
    public class OnlineMenuView
    {
        private readonly TextField nameField;
        private readonly TextField codeField;
        private readonly Toggle splitToggle;
        private readonly Label message;

        public event Action<string, bool> CreateClicked;   // (nombre, permitir repartir)
        public event Action<string, string> JoinClicked;   // (nombre, codigo)
        public event Action<string> QuickClicked;          // (nombre) buscar partida rapida

        public OnlineMenuView(VisualElement screen)
        {
            nameField = screen.Q("my-name").Q<TextField>("input");
            codeField = screen.Q("join-code").Q<TextField>("input");
            splitToggle = screen.Q("opt-split").Q<Toggle>("toggle");
            message = screen.Q<Label>("error");

            screen.Q<Button>("btn-create").clicked += OnCreate;

            Button quick = screen.Q<Button>("btn-quick");
            if (quick != null) quick.clicked += () => { if (QuickClicked != null) QuickClicked(PlayerName()); };
            screen.Q("action-row").Q<Button>("btn-primary").clicked += OnJoin;
        }

        /// <summary>Muestra un aviso o un error debajo del boton (texto vacio = nada).</summary>
        public void ShowMessage(string text)
        {
            message.text = text;
        }

        private string PlayerName()
        {
            string value = nameField.value.Trim();
            return value.Length > 0 ? value : "Jugador";
        }

        private void OnCreate()
        {
            if (CreateClicked != null) CreateClicked(PlayerName(), splitToggle.value);
        }

        private void OnJoin()
        {
            string code = codeField.value.Trim().ToUpperInvariant();
            if (code.Length == 0)
            {
                ShowMessage("Escribe el código de la partida.");
                return;
            }
            if (JoinClicked != null) JoinClicked(PlayerName(), code);
        }
    }
}
