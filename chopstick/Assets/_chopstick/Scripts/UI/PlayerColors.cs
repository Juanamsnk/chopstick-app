using UnityEngine;

namespace Chopsticks.UI
{
    /// <summary>Colores de cada jugador. Son los mismos que --player-0..3 de Theme.uss.</summary>
    public static class PlayerColors
    {
        private static readonly Color[] colors =
        {
            new Color32(0xE8, 0x59, 0x0C, 255),
            new Color32(0x1C, 0x7E, 0xD6, 255),
            new Color32(0x2F, 0x9E, 0x44, 255),
            new Color32(0xAE, 0x3E, 0xC9, 255)
        };

        public static readonly Color Danger = new Color32(0xE0, 0x31, 0x31, 255);

        public static Color Get(int player)
        {
            return colors[player % colors.Length];
        }
    }
}
