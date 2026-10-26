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
        private readonly CheckOptionView splitOption;
        private readonly NameAutofill autoName;

        public event Action<GameSetup> StartClicked;

        public OfflineSetupView(VisualElement screen)
        {
            for (int i = 0; i < nameFields.Length; i++)
            {
                nameFields[i] = screen.Q("name-" + i).Q<TextField>("input");
            }
            splitOption = new CheckOptionView(screen.Q("opt-split"));
            autoName = new NameAutofill(nameFields[0], "Jugador 1");   // el jugador 1 eres tu

            screen.Q("action-row").Q<Button>("btn-primary").clicked += OnStart;
        }

        /// <summary>Pone tu nombre de cuenta en el jugador 1 ("" = sin sesion).</summary>
        public void SetProfileName(string name)
        {
            autoName.Apply(name);
        }

        private void OnStart()
        {
            var setup = new GameSetup { SplitAllowed = splitOption.Value, VsAI = false };

            for (int i = 0; i < nameFields.Length; i++)
            {
                string playerName = nameFields[i].value.Trim();
                setup.Names.Add(playerName.Length > 0 ? playerName : "Jugador " + (i + 1));
            }
            if (StartClicked != null) StartClicked(setup);
        }
    }
}
