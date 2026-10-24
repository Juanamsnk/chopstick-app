using System;
using UnityEngine.UIElements;

namespace Chopsticks.UI
{
    /// <summary>Ventana que sale encima de cualquier pantalla cuando un amigo te reta.</summary>
    public class ChallengePopupView
    {
        private readonly VisualElement root;
        private readonly Label text;

        public event Action AcceptClicked;
        public event Action DeclineClicked;

        public ChallengePopupView(VisualElement popup)
        {
            root = popup;
            text = root.Q<Label>("challenge-text");
            root.Q<Button>("btn-challenge-accept").clicked += () => { if (AcceptClicked != null) AcceptClicked(); };
            root.Q<Button>("btn-challenge-decline").clicked += () => { if (DeclineClicked != null) DeclineClicked(); };
        }

        public bool IsVisible { get { return root.style.display.value != DisplayStyle.None; } }

        public void Show(string fromName)
        {
            text.text = fromName + " te reta a una partida";
            root.style.display = DisplayStyle.Flex;
        }

        public void Hide()
        {
            root.style.display = DisplayStyle.None;
        }
    }
}
