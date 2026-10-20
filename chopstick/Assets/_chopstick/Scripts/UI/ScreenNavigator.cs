using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Chopsticks.UI
{
    /// <summary>
    /// Se encarga solo de una cosa: mostrar una pantalla y ocultar las demas.
    /// No sabe nada de botones ni de reglas del juego.
    /// </summary>
    public class ScreenNavigator
    {
        private readonly Dictionary<ScreenId, VisualElement> screens = new Dictionary<ScreenId, VisualElement>();

        public ScreenId Current { get; private set; }

        /// <summary>Se avisa cada vez que se muestra una pantalla.</summary>
        public event Action<ScreenId> Shown;

        /// <summary>Guarda una pantalla para poder mostrarla despues.</summary>
        public void Register(ScreenId id, VisualElement screen)
        {
            screens[id] = screen;
        }

        /// <summary>Muestra la pantalla indicada y oculta todas las demas.</summary>
        public void Show(ScreenId id)
        {
            foreach (var pair in screens)
            {
                bool visible = pair.Key == id;
                pair.Value.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            }
            Current = id;
            if (Shown != null) Shown(id);
        }
    }
}
