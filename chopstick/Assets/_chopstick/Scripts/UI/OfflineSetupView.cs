using System;
using Chopsticks.Game;
using UnityEngine.UIElements;

namespace Chopsticks.UI
{
    /// <summary>
    /// Pantalla de ajustes del modo offline. Lee los campos y, al pulsar Empezar,
    /// avisa con un GameSetup. No arranca la partida ella misma.
    /// </summary>
    public class OfflineSetupView
    {
        private readonly SegmentedControlView playersSelector;
        private readonly VisualElement[] nameContainers = new VisualElement[4];
        private readonly TextField[] nameFields = new TextField[4];
        private readonly Toggle splitToggle;

        public event Action<GameSetup> StartClicked;

        public OfflineSetupView(VisualElement screen)
        {
            playersSelector = new SegmentedControlView(screen.Q("players-count"));
            for (int i = 0; i < 4; i++)
            {
                nameContainers[i] = screen.Q("name-" + i);
                nameFields[i] = nameContainers[i].Q<TextField>("input");
            }
            splitToggle = screen.Q("opt-split").Q<Toggle>("toggle");

            // Las opciones son 2, 3 y 4 jugadores: segun la elegida se muestran mas o menos nombres.
            playersSelector.Changed += index => ShowNameFields(index + 2);
            ShowNameFields(2);

            screen.Q("action-row").Q<Button>("btn-primary").clicked += OnStart;
        }

        private void ShowNameFields(int count)
        {
            for (int i = 0; i < 4; i++)
            {
                nameContainers[i].style.display = i < count ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        private void OnStart()
        {
            int count = playersSelector.SelectedIndex + 2;
            var setup = new GameSetup { SplitAllowed = splitToggle.value, VsAI = false };

            for (int i = 0; i < count; i++)
            {
                string playerName = nameFields[i].value.Trim();
                setup.Names.Add(playerName.Length > 0 ? playerName : "Jugador " + (i + 1));
            }
            if (StartClicked != null) StartClicked(setup);
        }
    }
}
