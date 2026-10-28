using UnityEngine.UIElements;

namespace Chopsticks.UI
{
    /// <summary>
    /// Hace que todo lo que se escribe (o se pega) en un campo de texto se ponga en mayusculas al momento.
    /// Se usa en los campos de codigo, que siempre son en mayusculas.
    /// </summary>
    public static class UppercaseField
    {
        public static void Apply(TextField field)
        {
            if (field == null) return;

            field.RegisterValueChangedCallback(evt =>
            {
                string value = evt.newValue ?? "";
                string upper = value.ToUpperInvariant();
                if (upper == value) return;

                // cursorIndex no es fiable aqui (vale 0 tras el cambio y el texto salia al reves).
                // Se calcula comparando el valor anterior con el nuevo: el cursor queda justo
                // despues de lo que acabas de escribir o pegar.
                string previous = evt.previousValue ?? "";
                int cursor = CursorAfterEdit(previous, upper);

                field.SetValueWithoutNotify(upper);
                field.SelectRange(cursor, cursor);

                // Por si el campo reposiciona el cursor despues del callback
                field.schedule.Execute(() => field.SelectRange(cursor, cursor));
            });
        }

        private static int CursorAfterEdit(string oldText, string newText)
        {
            int minLen = oldText.Length < newText.Length ? oldText.Length : newText.Length;

            int prefix = 0;
            while (prefix < minLen && oldText[prefix] == newText[prefix]) prefix++;

            int suffix = 0;
            while (suffix < minLen - prefix &&
                   oldText[oldText.Length - 1 - suffix] == newText[newText.Length - 1 - suffix]) suffix++;

            return newText.Length - suffix;
        }
    }
}