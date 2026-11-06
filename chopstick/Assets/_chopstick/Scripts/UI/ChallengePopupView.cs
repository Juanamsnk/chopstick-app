using System;
using Chopsticks.Audio;
using UnityEngine.UIElements;

namespace Chopsticks.UI
{
    /// <summary>
    /// Ventana que sale encima de cualquier pantalla para los avisos online:
    /// cuando un amigo te reta, mientras se prepara tu reto a un amigo y si algo falla.
    /// </summary>
    public class ChallengePopupView
    {
        private readonly VisualElement root;
        private readonly Label title;
        private readonly Label text;
        private readonly Button acceptButton;
        private readonly Button declineButton;
        private readonly SoundManager sounds;

        public event Action AcceptClicked;
        public event Action DeclineClicked;

        public ChallengePopupView(VisualElement popup, SoundManager sounds)
        {
            this.sounds = sounds;
            root = popup;
            title = root.Q<Label>("challenge-title");
            text = root.Q<Label>("challenge-text");
            acceptButton = root.Q<Button>("btn-challenge-accept");
            declineButton = root.Q<Button>("btn-challenge-decline");
            acceptButton.clicked += () => { if (AcceptClicked != null) AcceptClicked(); };
            declineButton.clicked += () => { if (DeclineClicked != null) DeclineClicked(); };
        }

        public bool IsVisible { get { return root.style.display.value != DisplayStyle.None; } }

        /// <summary>Un amigo te reta. Suena el aviso.</summary>
        public void Show(string fromName)
        {
            Set("¡Reto!", fromName + " te reta a una partida", true, true, "Rechazar");
            if (sounds != null) sounds.Play(SoundId.Notification);
        }

        /// <summary>Tras aceptar: avisa de que se esta entrando en la partida. El boton pasa a "Cancelar".</summary>
        public void ShowJoining(string fromName)
        {
            Set("¡Reto!", "Uniéndote a la partida de " + fromName + "...", false, true, "Cancelar");
        }

        /// <summary>Tu reto a un amigo: se esta preparando la partida. Sin botones hasta que termine.</summary>
        public void ShowPreparing(string friendName)
        {
            Set("¡Reto!", "Preparando la partida para retar a " + friendName + "...", false, false, "");
        }

        /// <summary>Algo fallo al preparar o enviar el reto. El boton cierra la ventana.</summary>
        public void ShowError(string message)
        {
            Set("Ups", message, false, true, "Cerrar");
        }

        public void Hide()
        {
            root.style.display = DisplayStyle.None;
        }

        private void Set(string titleText, string message, bool showAccept, bool showDecline, string declineText)
        {
            title.text = titleText;
            text.text = message;
            acceptButton.style.display = showAccept ? DisplayStyle.Flex : DisplayStyle.None;
            declineButton.style.display = showDecline ? DisplayStyle.Flex : DisplayStyle.None;
            declineButton.text = declineText;
            root.style.display = DisplayStyle.Flex;
        }
    }
}
