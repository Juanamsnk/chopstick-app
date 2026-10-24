using UnityEngine;
using UnityEngine.UIElements;

namespace Chopsticks.UI
{
    /// <summary>La tarjeta de un jugador: nombre, color, etiqueta de turno y sus dos manos.</summary>
    public class PlayerCardView
    {
        private readonly VisualElement container;
        private readonly VisualElement card;
        private readonly VisualElement dot;
        private readonly Label nameLabel;
        private readonly Label badge;

        public HandView[] Hands { get; private set; }

        public PlayerCardView(VisualElement container)
        {
            this.container = container;
            card = container.Q("player-card");
            dot = container.Q("dot");
            nameLabel = container.Q<Label>("player-name");
            badge = container.Q<Label>("badge");
            Hands = new[]
            {
                new HandView(container.Q("hand-0")),
                new HandView(container.Q("hand-1"))
            };
        }

        public void Show(bool visible)
        {
            container.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public void SetPlayer(string playerName, Color color)
        {
            nameLabel.text = playerName;
            dot.style.backgroundColor = color;
        }

        /// <summary>El rival tiene las manos giradas con los dedos hacia abajo.</summary>
        public void SetOpponent(bool opponent)
        {
            card.EnableInClassList("player-card--opponent", opponent);
        }

        public void SetTurn(bool turn, Color color)
        {
            card.EnableInClassList("player-card--turn", turn);
            badge.style.display = turn ? DisplayStyle.Flex : DisplayStyle.None;
            badge.style.backgroundColor = color;

            if (turn)
            {
                card.style.borderTopColor = color;
                card.style.borderBottomColor = color;
                card.style.borderLeftColor = color;
                card.style.borderRightColor = color;
            }
            else
            {
                card.style.borderTopColor = StyleKeyword.Null;
                card.style.borderBottomColor = StyleKeyword.Null;
                card.style.borderLeftColor = StyleKeyword.Null;
                card.style.borderRightColor = StyleKeyword.Null;
            }
        }

        /// <summary>El estilo de las dos manos de esta tarjeta.</summary>
        public void SetSkin(string skinId, int tone)
        {
            for (int i = 0; i < Hands.Length; i++) Hands[i].SetSkin(skinId, tone);
        }

        public void SetDead(bool dead)
        {
            card.EnableInClassList("player-card--dead", dead);
        }
    }
}
