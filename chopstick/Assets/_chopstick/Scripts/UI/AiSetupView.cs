using System;
using Chopsticks.AI;
using Chopsticks.Game;
using UnityEngine.UIElements;

namespace Chopsticks.UI
{
    /// <summary>Pantalla de ajustes contra la maquina: dificultad, nombre y repartir dedos.</summary>
    public class AiSetupView
    {
        private readonly SegmentedControlView difficultySelector;
        private readonly TextField nameField;
        private readonly Toggle splitToggle;

        public event Action<GameSetup> StartClicked;

        public AiSetupView(VisualElement screen)
        {
            difficultySelector = new SegmentedControlView(screen.Q("difficulty"));
            difficultySelector.Select(1);   // empieza en Medio

            nameField = screen.Q("name-0").Q<TextField>("input");
            splitToggle = screen.Q("opt-split").Q<Toggle>("toggle");

            screen.Q("action-row").Q<Button>("btn-primary").clicked += OnStart;
        }

        private void OnStart()
        {
            string playerName = nameField.value.Trim();
            var setup = new GameSetup
            {
                SplitAllowed = splitToggle.value,
                VsAI = true,
                // Los botones son Facil, Medio, Dificil: 0, 1, 2 coinciden con el enum Difficulty
                Difficulty = (Difficulty)difficultySelector.SelectedIndex
            };
            setup.Names.Add(playerName.Length > 0 ? playerName : "Jugador 1");
            setup.Names.Add("Máquina");

            if (StartClicked != null) StartClicked(setup);
        }
    }
}
