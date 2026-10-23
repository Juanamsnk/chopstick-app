using System;
using UnityEngine.UIElements;

namespace Chopsticks.UI
{
    /// <summary>
    /// Casilla de marcar ("Permitir repartir dedos"). Esta hecha con elementos sencillos
    /// (un boton con una etiqueta y un cuadrado), asi que se ve igual en todos los dispositivos.
    /// Toda la fila es pulsable. Empieza marcada.
    /// </summary>
    public class CheckOptionView
    {
        private readonly Button button;
        private bool value;

        public event Action<bool> Changed;

        public bool Value
        {
            get { return value; }
            set { SetValue(value); }
        }

        /// <param name="container">La instancia de CheckOption (por ejemplo, la llamada "opt-split").</param>
        public CheckOptionView(VisualElement container, bool initialValue = true)
        {
            button = container.Q<Button>("check");
            button.clicked += () => SetValue(!value);
            SetValue(initialValue);
        }

        private void SetValue(bool newValue)
        {
            value = newValue;
            button.EnableInClassList("check--on", newValue);
            if (Changed != null) Changed(newValue);
        }
    }
}
