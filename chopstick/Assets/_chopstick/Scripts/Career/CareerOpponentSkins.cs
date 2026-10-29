using Chopsticks.Shop;

namespace Chopsticks.Career
{
    /// <summary>
    /// La mano del rival en el modo carrera: cada rango tiene la suya y la lleva todo el rato.
    /// Van subiendo de rareza a medida que el rival es mas dificil.
    /// </summary>
    public static class CareerOpponentSkins
    {
        private struct Look
        {
            public string Id;
            public int Tone;   // solo cuenta en los estilos que usan tono de piel
            public Look(string id, int tone) { Id = id; Tone = tone; }
        }

        // Misma posicion que los rangos de CareerLevels: Novato, Aprendiz, Competidor, Veterano...
        private static readonly Look[] Looks =
        {
            new Look("clasica", 0),     // Novato: la mano normal
            new Look("golf", 0),        // Aprendiz
            new Look("tatuajes", 2),    // Competidor
            new Look("zombi", 0),       // Veterano
            new Look("golem", 0),       // Experto
            new Look("armadura", 0),    // Maestro
            new Look("hielo", 0),       // Campeon
            new Look("oro", 0),         // Leyenda
            new Look("neon", 0),        // Gran maestro
            new Look("superheroe", 0)   // Rey 5ticks: la mano final de la carrera
        };

        public static string SkinId(int level)
        {
            return Pick(level).Id;
        }

        public static int Tone(int level)
        {
            return Pick(level).Tone;
        }

        /// <summary>
        /// Rango (0 a 9) cuyo rival lleva esta mano, o -1 si ningun rival la lleva.
        /// La mano por defecto no cuenta: ya es tuya desde el principio.
        /// </summary>
        public static int TierOf(string skinId)
        {
            if (skinId == HandSkinCatalog.DefaultId) return -1;
            for (int tier = 0; tier < Looks.Length; tier++)
            {
                if (Looks[tier].Id == skinId) return tier;
            }
            return -1;
        }

        /// <summary>
        /// Nivel que hay que superar para ganar esta mano: el ultimo del rango de quien la lleva
        /// (Aprendiz lleva "golf" -> nivel 20, Veterano lleva "zombi" -> nivel 40...). 0 = no se gana en la carrera.
        /// </summary>
        public static int RewardLevelFor(string skinId)
        {
            int tier = TierOf(skinId);
            return tier < 0 ? 0 : (tier + 1) * CareerLevels.LevelsPerTier;
        }

        private static Look Pick(int level)
        {
            int tier = CareerLevels.TierIndex(level);
            if (tier < 0) tier = 0;
            if (tier >= Looks.Length) tier = Looks.Length - 1;
            return Looks[tier];
        }
    }
}
