using System;
using UnityEngine.UIElements;

namespace Chopsticks.UI
{
    /// <summary>El selector de 3 opciones (numero de jugadores o dificultad).</summary>
    public class SegmentedControlView
    {
        private readonly Button[] items = new Button[3];

        public int SelectedIndex { get; private set; }
        public event Action<int> Changed;

        public SegmentedControlView(VisualElement container)
        {
            for (int i = 0; i < items.Length; i++)
            {
                int index = i;
                items[i] = container.Q<Button>("seg-" + i);
                items[i].clicked += () => Select(index);
            }
        }

        public void Select(int index)
        {
            SelectedIndex = index;
            for (int i = 0; i < items.Length; i++)
            {
                items[i].EnableInClassList("seg__item--on", i == index);
            }
            if (Changed != null) Changed(index);
        }
    }
}
