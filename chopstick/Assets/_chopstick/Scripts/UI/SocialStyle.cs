using UnityEngine;
using UnityEngine.UIElements;

namespace Chopsticks.UI
{
    /// <summary>
    /// Piezas pequenas que comparten Amigos y Clasificacion: avatar con inicial, chips, plurales.
    /// Aqui NO hay colores ni medidas: todo el aspecto esta en Styles/Social.uss.
    /// </summary>
    public static class SocialStyle
    {
        /// <summary>Crea un avatar (circulo con la inicial). sizeClass: avatar--sm, avatar--lg, avatar--xl o null.</summary>
        public static VisualElement Avatar(string name, string sizeClass = null)
        {
            var avatar = new VisualElement();
            avatar.AddToClassList("avatar");
            if (!string.IsNullOrEmpty(sizeClass)) avatar.AddToClassList(sizeClass);
            var initial = new Label();
            initial.AddToClassList("avatar__initial");
            avatar.Add(initial);
            SetAvatar(avatar, initial, name);
            return avatar;
        }

        /// <summary>Pone la inicial y el color (siempre el mismo para el mismo nombre) a un avatar ya existente.</summary>
        public static void SetAvatar(VisualElement avatar, Label initial, string name)
        {
            for (int i = 0; i < 4; i++) avatar.RemoveFromClassList("avatar--" + i);
            avatar.AddToClassList("avatar--" + ColorIndex(name));
            initial.text = Initial(name);
        }

        /// <summary>Etiqueta redondeada pequena. modifier: chip--win, chip--lose o null (neutra).</summary>
        public static Label Chip(string text, string modifier = null)
        {
            var chip = new Label(text);
            chip.AddToClassList("chip");
            if (!string.IsNullOrEmpty(modifier)) chip.AddToClassList(modifier);
            return chip;
        }

        public static string Plural(int count, string one, string many)
        {
            return count + " " + (count == 1 ? one : many);
        }

        public static int WinRate(int wins, int losses)
        {
            int total = wins + losses;
            return total == 0 ? 0 : Mathf.RoundToInt(100f * wins / total);
        }

        private static string Initial(string name)
        {
            string trimmed = string.IsNullOrEmpty(name) ? "" : name.Trim();
            return trimmed.Length == 0 ? "?" : trimmed.Substring(0, 1).ToUpperInvariant();
        }

        private static int ColorIndex(string name)
        {
            int sum = 0;
            if (!string.IsNullOrEmpty(name))
            {
                for (int i = 0; i < name.Length; i++) sum += name[i];
            }
            return sum % 4;
        }
    }
}
