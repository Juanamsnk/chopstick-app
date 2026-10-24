using System;
using Chopsticks.Shop;
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
        private readonly VisualElement fingersBox;
        private readonly VisualElement palm;
        private readonly VisualElement sprite;   // el dibujo del estilo equipado (PNG)
        private string skinId = HandSkinCatalog.DefaultId;
        private int tone;
        private int fingerCount = 4;

        public event Action Clicked;

        /// <summary>El elemento de la mano (para moverlo y animarlo).</summary>
        public VisualElement Root { get { return button; } }

        public HandView(VisualElement container)
        {
            button = container.Q<Button>("hand");
            for (int i = 0; i < fingers.Length; i++) fingers[i] = container.Q("finger-" + i);
            count = container.Q<Label>("count");
            tag = container.Q<Label>("tag");
            fingersBox = container.Q("fingers");
            palm = container.Q("palm");
            sprite = container.Q("sprite");

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
            fingerCount = n;
            ApplySprite();
        }

        /// <summary>Cambia el estilo de la mano (y el tono de piel si el estilo tiene piel).</summary>
        public void SetSkin(string newSkinId, int newTone)
        {
            if (newSkinId == skinId && newTone == tone) return;
            skinId = newSkinId;
            tone = newTone;
            ApplySprite();
        }

        /// <summary>
        /// Pone el PNG del estilo con los dedos que toquen. Si falta el PNG, se ve la mano basica de siempre.
        /// </summary>
        private void ApplySprite()
        {
            if (sprite == null) return;

            Texture2D texture = HandSkinSprites.Get(skinId, tone, fingerCount);
            bool useSprite = texture != null;
            sprite.style.display = useSprite ? DisplayStyle.Flex : DisplayStyle.None;
            if (fingersBox != null) fingersBox.style.display = useSprite ? DisplayStyle.None : DisplayStyle.Flex;
            if (palm != null) palm.style.display = useSprite ? DisplayStyle.None : DisplayStyle.Flex;
            if (useSprite) sprite.style.backgroundImage = new StyleBackground(texture);
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
