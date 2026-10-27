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

        private string challengeStatus;   // aviso del reto enviado a un amigo (null = sala normal con codigo)
        private string otherName;         // nombre del segundo jugador, cuando ya ha entrado

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

        /// <summary>Texto de espera cuando la sala es de un reto a un amigo. Pon null para una sala normal.</summary>
        public void SetChallengeStatus(string text)
        {
            challengeStatus = text;
        }

        public void SetPlayers(IList<string> names, int localSeat, IList<string> ranks = null)
        {
            otherName = names.Count > 1 ? names[1] : null;
            playerList.Clear();
            for (int i = 0; i < names.Count; i++)
            {
                string text = names[i];
                if (i == 0) text += " (anfitrión)";
                if (i == localSeat) text += " (tú)";
                if (ranks != null && i < ranks.Count && !string.IsNullOrEmpty(ranks[i])) text += "  ·  " + ranks[i];

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

            if (isHost && challengeStatus != null)
            {
                infoLabel.text = playerCount >= 2
                    ? (string.IsNullOrEmpty(otherName) ? "Tu amigo" : otherName) + " se ha unido. ¡Pulsa Empezar!"
                    : challengeStatus;
                return;
            }

            infoLabel.text = isHost
                ? "Comparte este código con tu rival para que se una a la partida."
                : "Esperando a que el anfitrión empiece la partida...";
        }
    }
}
