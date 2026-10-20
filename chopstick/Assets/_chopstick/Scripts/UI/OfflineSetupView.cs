using System;
using Chopsticks.Game;
using UnityEngine.UIElements;

namespace Chopsticks.UI
{
    /// <summary>
    /// Pantalla de ajustes del modo offline (2 jugadores). Lee los nombres y la opcion de repartir
    /// y, al pulsar Empezar, avisa con un GameSetup. No arranca la partida ella misma.
    /// </summary>
    public class OfflineSetupView
    {
        private readonly TextField[] nameFields = new TextField[2];
        private readonly Toggle splitToggle;

        public event Action<GameSetup> StartClicked;

        public OfflineSetupView(VisualElement screen)
        {
            for (int i = 0; i < nameFields.Length; i++)
            {
                nameFields[i] = screen.Q("name-" + i).Q<TextField>("input");
            }
            splitToggle = screen.Q("opt-split").Q<Toggle>("toggle");

            screen.Q("action-row").Q<Button>("btn-primary").clicked += OnStart;
        }

        private void OnStart()
        {
            var setup = new GameSetup { SplitAllowed = splitToggle.value, VsAI = false };

            for (int i = 0; i < nameFields.Length; i++)
            {
                string playerName = nameFields[i].value.Trim();
                setup.Names.Add(playerName.Length > 0 ? playerName : "Jugador " + (i + 1));
            }
            if (StartClicked != null) StartClicked(setup);
        }
    }
}
