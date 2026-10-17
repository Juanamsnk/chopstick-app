using UnityEngine;
using UnityEngine.UIElements;

namespace Chopsticks.UI
{
    /// <summary>El banner de arriba que dice de quien es el turno.</summary>
    public class TurnBannerView
    {
        private readonly VisualElement root;
        private readonly Label title;
        private readonly Label hint;

        public TurnBannerView(VisualElement container)
        {
            root = container.Q("turn-banner");
            title = container.Q<Label>("title");
            hint = container.Q<Label>("hint");
        }

        public void Set(string titleText, string hintText, Color color, bool waiting)
        {
            title.text = titleText;
            hint.text = hintText;
            root.style.backgroundColor = color;
            root.EnableInClassList("turn-banner--wait", waiting);
        }
    }
}
