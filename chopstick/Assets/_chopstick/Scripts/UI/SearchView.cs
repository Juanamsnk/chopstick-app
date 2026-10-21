using System;
using UnityEngine.UIElements;

namespace Chopsticks.UI
{
    /// <summary>
    /// Pantalla "Buscando rival...". Muestra el estado de la busqueda con unos puntos que se mueven,
    /// un boton para cancelar y, pasado un rato, la opcion de jugar contra la maquina.
    /// Solo dibuja y avisa con eventos.
    /// </summary>
    public class SearchView
    {
        private readonly VisualElement root;
        private readonly Label title;
        private readonly Label status;
        private readonly Button machineButton;
        private readonly IVisualElementScheduledItem dotsTimer;
        private int dots;

        public event Action CancelClicked;
        public event Action MachineClicked;

        public SearchView(VisualElement screen)
        {
            root = screen;
            title = screen.Q<Label>("search-title");
            status = screen.Q<Label>("search-status");
            machineButton = screen.Q<Button>("btn-search-ai");

            screen.Q<Button>("btn-cancel-search").clicked += () => { if (CancelClicked != null) CancelClicked(); };
            machineButton.clicked += () => { if (MachineClicked != null) MachineClicked(); };

            // Los puntos de "Buscando rival..." se mueven cada medio segundo mientras se busca
            dotsTimer = screen.schedule.Execute(Tick).Every(500);
            dotsTimer.Pause();
        }

        /// <summary>Empieza a mostrar la busqueda.</summary>
        public void Begin()
        {
            dots = 0;
            title.text = "Buscando rival";
            SetStatus("Estamos buscando a alguien con quien jugar.");
            ShowMachineOption(false);
            dotsTimer.Resume();
        }

        public void End()
        {
            dotsTimer.Pause();
        }

        public void SetStatus(string text)
        {
            status.text = text;
        }

        public void ShowMachineOption(bool visible)
        {
            machineButton.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        /// <summary>Programa una accion para dentro de unos milisegundos.</summary>
        public void Schedule(long delayMs, Action action)
        {
            root.schedule.Execute(action).StartingIn(delayMs);
        }

        private void Tick()
        {
            dots = (dots + 1) % 4;
            title.text = "Buscando rival" + new string('.', dots);
        }
    }
}
