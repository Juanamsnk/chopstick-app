using System;
using UnityEngine.UIElements;

namespace Chopsticks.UI
{
    /// <summary>Ventana que sale encima de cualquier pantalla cuando un amigo te reta.</summary>
    public class ChallengePopupView
    {
        private readonly VisualElement root;
        private readonly Label text;
        private readonly Button acceptButton;
        private readonly Button declineButton;

        public event Action AcceptClicked;
        public event Action DeclineClicked;

        public ChallengePopupView(VisualElement popup)
        {
            root = popup;
            text = root.Q<Label>("challenge-text");
            acceptButton = root.Q<Button>("btn-challenge-accept");
            declineButton = root.Q<Button>("btn-challenge-decline");
            acceptButton.clicked += () => { if (AcceptClicked != null) AcceptClicked(); };
            declineButton.clicked += () => { if (DeclineClicked != null) DeclineClicked(); };
        }

        public bool IsVisible { get { return root.style.display.value != DisplayStyle.None; } }

        public void Show(string fromName)
        {
            text.text = fromName + " te reta a una partida";
            acceptButton.style.display = DisplayStyle.Flex;
            declineButton.text = "Rechazar";
            root.style.display = DisplayStyle.Flex;
        }

        /// <summary>Tras aceptar: avisa de que se esta entrando en la partida. El boton pasa a "Cancelar".</summary>
        public void ShowJoining(string fromName)
        {
            text.text = "Uniéndote a la partida de " + fromName + "...";
            acceptButton.style.display = DisplayStyle.None;
            declineButton.text = "Cancelar";
            root.style.display = DisplayStyle.Flex;
        }

        public void Hide()
        {
            root.style.display = DisplayStyle.None;
        }
    }
}
