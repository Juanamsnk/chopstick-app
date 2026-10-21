using Chopsticks.Audio;
using UnityEngine.UIElements;

namespace Chopsticks.UI
{
    /// <summary>
    /// Hace sonar el sonido de boton cada vez que se pulsa un boton de la interfaz.
    /// No suena con las manos del juego: esas tienen sus propios sonidos (ataque, golpe).
    /// </summary>
    public class ButtonSounds
    {
        private readonly SoundManager sounds;

        public ButtonSounds(VisualElement root, SoundManager sounds)
        {
            this.sounds = sounds;
            // Un solo registro en la raiz sirve para todos los botones, tambien los creados por codigo.
            root.RegisterCallback<ClickEvent>(OnClick, TrickleDown.TrickleDown);
        }

        private void OnClick(ClickEvent evt)
        {
            if (sounds == null) return;

            var element = evt.target as VisualElement;
            if (element == null) return;

            Button button = element as Button ?? element.GetFirstAncestorOfType<Button>();
            if (button == null) return;
            if (button.ClassListContains("hand")) return;   // las manos no hacen sonido de boton

            sounds.Play(SoundId.Button);
        }
    }
}
