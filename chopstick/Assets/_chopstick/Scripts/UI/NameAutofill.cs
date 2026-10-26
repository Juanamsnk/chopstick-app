using UnityEngine.UIElements;

namespace Chopsticks.UI
{
    /// <summary>
    /// Rellena un campo de nombre con el de la cuenta cuando hay sesion iniciada.
    /// Solo lo cambia si el jugador no lo ha tocado: vacio, el nombre por defecto o el que puso esto mismo.
    /// Si cierras sesion, vuelve al nombre por defecto.
    /// </summary>
    public class NameAutofill
    {
        private const int MaxLength = 14;   // el mismo limite que los campos de nombre

        private readonly TextField field;
        private readonly string fallback;
        private string lastAuto;

        public NameAutofill(TextField field, string fallback)
        {
            this.field = field;
            this.fallback = fallback;
        }

        /// <param name="profileName">Nombre de la cuenta, o vacio si no hay sesion.</param>
        public void Apply(string profileName)
        {
            string current = field.value == null ? "" : field.value.Trim();
            bool untouched = current.Length == 0 || current == fallback || current == lastAuto;
            if (!untouched) return;

            if (string.IsNullOrEmpty(profileName))
            {
                field.value = fallback;
                lastAuto = null;
            }
            else
            {
                string name = profileName.Length > MaxLength ? profileName.Substring(0, MaxLength) : profileName;
                field.value = name;
                lastAuto = name;
            }
        }
    }
}
