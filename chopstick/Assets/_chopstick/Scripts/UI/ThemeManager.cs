using UnityEngine;
using UnityEngine.UIElements;

namespace Chopsticks.UI
{
    /// <summary>
    /// Cambia entre tema oscuro y claro.
    /// Funciona poniendo la clase theme-dark o theme-light en el elemento raiz,
    /// y los colores salen de Theme.uss. Guarda la eleccion en el dispositivo.
    /// </summary>
    public class ThemeManager
    {
        private const string DarkClass = "theme-dark";
        private const string LightClass = "theme-light";
        private const string SaveKey = "chopsticks_theme_dark";

        private readonly VisualElement target;

        public bool IsDark { get; private set; }

        public ThemeManager(VisualElement target)
        {
            this.target = target;
            // Por defecto oscuro (1). Si el jugador ya eligio, se respeta.
            Apply(PlayerPrefs.GetInt(SaveKey, 1) == 1);
        }

        public void SetDark(bool dark)
        {
            Apply(dark);
        }

        public void Toggle()
        {
            Apply(!IsDark);
        }

        private void Apply(bool dark)
        {
            IsDark = dark;
            target.RemoveFromClassList(DarkClass);
            target.RemoveFromClassList(LightClass);
            target.AddToClassList(dark ? DarkClass : LightClass);

            PlayerPrefs.SetInt(SaveKey, dark ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
}
