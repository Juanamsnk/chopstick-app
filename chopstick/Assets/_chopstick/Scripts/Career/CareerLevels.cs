using Chopsticks.AI;
using Chopsticks.Game;

namespace Chopsticks.Career
{
    /// <summary>Los 100 niveles del modo carrera: 10 rangos de 10 niveles cada uno.</summary>
    public static class CareerLevels
    {
        public const int Count = 100;
        public const int LevelsPerTier = 10;

        private static readonly string[] Tiers =
        {
            "Novato", "Aprendiz", "Competidor", "Veterano", "Experto",
            "Maestro", "Campeón", "Leyenda", "Gran maestro", "Rey 5ticks"
        };

        public static int TierCount { get { return Tiers.Length; } }

        /// <summary>Rango de un nivel (1 a 100).</summary>
        public static string TierName(int level)
        {
            return Tiers[TierIndex(level)];
        }

        public static int TierIndex(int level)
        {
            return (level - 1) / LevelsPerTier;
        }

        /// <summary>
        /// Rango de un jugador (0 a 9) segun los niveles que lleva superados: el rango del nivel que le toca jugar.
        /// Con 0 niveles es Novato; al superar el 10 pasa a Aprendiz; con los 100 superados es Rey 5ticks.
        /// </summary>
        public static int RankIndexFor(int completed)
        {
            int next = System.Math.Min(System.Math.Max(completed, 0) + 1, Count);
            return TierIndex(next);
        }

        public static string RankNameFor(int completed)
        {
            return Tiers[RankIndexFor(completed)];
        }

        /// <summary>Rango por posicion (0 a 9), para los titulos de la lista de niveles.</summary>
        public static string TierNameByIndex(int tierIndex)
        {
            return Tiers[tierIndex];
        }

        /// <summary>Nombre del rival en la partida, por ejemplo "Aprendiz Nv.12".</summary>
        public static string OpponentName(int level)
        {
            return TierName(level) + " Nv." + level;
        }

        /// <summary>Ajustes de la partida de un nivel: tu contra la maquina de ese nivel.</summary>
        public static GameSetup CreateSetup(int level)
        {
            var setup = new GameSetup
            {
                SplitAllowed = true,
                VsAI = true,
                AiProfile = AiProfile.FromCareerLevel(level)
            };
            setup.Names.Add("Jugador");
            setup.Names.Add(OpponentName(level));
            return setup;
        }
    }
}
