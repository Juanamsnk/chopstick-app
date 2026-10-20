using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Chopsticks.UI
{
    /// <summary>Sala de espera: muestra el codigo y la lista de jugadores.</summary>
    public class LobbyView
    {
        private readonly Label codeLabel;
        private readonly Label infoLabel;
        private readonly VisualElement playerList;
        private readonly Button startButton;

        public event Action StartClicked;

        public LobbyView(VisualElement screen)
        {
            codeLabel = screen.Q<Label>("code");
            infoLabel = screen.Q<Label>(className: "lobby__info");
            playerList = screen.Q("player-list");
            startButton = screen.Q("action-row").Q<Button>("btn-primary");

            startButton.clicked += () => { if (StartClicked != null) StartClicked(); };
        }

        public void SetCode(string code)
        {
            codeLabel.text = code;
        }

        public void SetPlayers(IList<string> names, int localSeat)
        {
            playerList.Clear();
            for (int i = 0; i < names.Count; i++)
            {
                string text = names[i];
                if (i == 0) text += " (anfitrión)";
                if (i == localSeat) text += " (tú)";

                var row = new VisualElement();
                row.AddToClassList("player-row");

                var dot = new VisualElement();
                dot.AddToClassList("dot");
                dot.style.backgroundColor = PlayerColors.Get(i);

                var label = new Label(text);
                label.AddToClassList("player-row__name");

                row.Add(dot);
                row.Add(label);
                playerList.Add(row);
            }
        }

        /// <summary>El anfitrion ve el boton Empezar; los demas esperan.</summary>
        public void SetHostControls(bool isHost, int playerCount)
        {
            startButton.style.display = isHost ? DisplayStyle.Flex : DisplayStyle.None;
            startButton.text = "Empezar (" + playerCount + "/2)";
            startButton.SetEnabled(playerCount >= 2);

            infoLabel.text = isHost
                ? "Comparte este código con tu rival para que se una a la partida."
                : "Esperando a que el anfitrión empiece la partida...";
        }
    }
}
