using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Chopsticks.UI
{
    /// <summary>Una mano dibujada: dedos levantados, numero, estados y etiqueta.</summary>
    public class HandView
    {
        private readonly Button button;
        private readonly VisualElement[] fingers = new VisualElement[4];
        private readonly Label count;
        private readonly Label tag;

        public event Action Clicked;

        public HandView(VisualElement container)
        {
            button = container.Q<Button>("hand");
            for (int i = 0; i < fingers.Length; i++) fingers[i] = container.Q("finger-" + i);
            count = container.Q<Label>("count");
            tag = container.Q<Label>("tag");

            button.clicked += () => { if (Clicked != null) Clicked(); };
        }

        /// <summary>Levanta n dedos. Con 0 la mano se muestra eliminada.</summary>
        public void SetFingers(int n)
        {
            for (int i = 0; i < fingers.Length; i++)
            {
                fingers[i].EnableInClassList("finger--down", i >= n);
            }
            count.text = n > 0 ? n.ToString() : "X";
            button.EnableInClassList("hand--out", n == 0);
        }

        public void SetSelected(bool selected)
        {
            button.EnableInClassList("hand--selected", selected);
        }

        /// <summary>Borde verde: esta mano rival se puede atacar.</summary>
        public void SetTarget(bool target)
        {
            button.EnableInClassList("hand--target", target);
        }

        /// <summary>Pone una etiqueta (Ataca, +2...) y colorea el borde.</summary>
        public void SetMark(string text, Color color)
        {
            tag.text = text;
            tag.style.backgroundColor = color;
            tag.style.display = DisplayStyle.Flex;
            button.style.borderTopColor = color;
            button.style.borderBottomColor = color;
            button.style.borderLeftColor = color;
            button.style.borderRightColor = color;
        }

        public void ClearMark()
        {
            tag.style.display = DisplayStyle.None;
            button.style.borderTopColor = StyleKeyword.Null;
            button.style.borderBottomColor = StyleKeyword.Null;
            button.style.borderLeftColor = StyleKeyword.Null;
            button.style.borderRightColor = StyleKeyword.Null;
        }
    }
}
