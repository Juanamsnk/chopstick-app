using Chopsticks.Career;

namespace Chopsticks.Shop
{
    /// <summary>
    /// Cuantas monedas da el modo carrera.
    /// Primera vez que superas un nivel: 30 + 3 por nivel (el 1 da 33 y el 100 da 330).
    /// Cada 10 niveles (al terminar un rango) hay un bonus extra.
    /// Volver a ganar un nivel que ya tenias superado da pocas monedas.
    /// Pasandote los 100 niveles consigues mas monedas de las que cuesta toda la tienda.
    /// </summary>
    public static class CareerRewards
    {
        public const int TierBonus = 150;
        public const int ReplayReward = 5;

        public static int FirstClear(int level)
        {
            int coins = 30 + 3 * level;
            if (level % CareerLevels.LevelsPerTier == 0) coins += TierBonus;
            return coins;
        }

        /// <summary>Todas las monedas que se consiguen superando los 100 niveles una sola vez.</summary>
        public static int TotalFromCareer
        {
            get
            {
                int total = 0;
                for (int level = 1; level <= CareerLevels.Count; level++) total += FirstClear(level);
                return total;
            }
        }
    }
}
