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
            new Look("cosmico", 0)      // Rey 5ticks
        };

        public static string SkinId(int level)
        {
            return Pick(level).Id;
        }

        public static int Tone(int level)
        {
            return Pick(level).Tone;
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
