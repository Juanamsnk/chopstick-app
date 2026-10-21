using UnityEngine;

namespace Chopsticks.UI
{
    /// <summary>Colores de cada jugador. Son los mismos que --player-0..3 de Theme.uss.</summary>
    public static class PlayerColors
    {
        private static readonly Color[] colors =
        {
            new Color32(0xFF, 0x7A, 0x2F, 255),
            new Color32(0x8E, 0x5C, 0xFF, 255),
            new Color32(0x34, 0xD9, 0x8A, 255),
            new Color32(0x3F, 0xA9, 0xFF, 255)
        };

        public static readonly Color Danger = new Color32(0xFF, 0x54, 0x70, 255);

        public static Color Get(int player)
        {
            return colors[player % colors.Length];
        }
    }
}
