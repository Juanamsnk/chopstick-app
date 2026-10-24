using System;

namespace Chopsticks.AI
{
    /// <summary>
    /// Como de lista es la maquina: la probabilidad de mirar N jugadas por delante.
    /// Posicion 0 = jugada al azar, 1 = mira 1 jugada, 2 = mira 2... hasta MaxDepth.
    /// Sirve para las tres dificultades clasicas y para los 100 niveles del modo carrera.
    /// </summary>
    public class AiProfile
    {
        /// <summary>Profundidad maxima de busqueda (con mas, el movil tardaria demasiado en pensar).</summary>
        public const int MaxDepth = 5;

        private readonly double[] weights = new double[MaxDepth + 1];

        private AiProfile() { }

        public static AiProfile FromDifficulty(Difficulty difficulty)
        {
            var p = new AiProfile();
            switch (difficulty)
            {
                case Difficulty.Easy:
                    p.weights[0] = 0.30; p.weights[1] = 0.70;
                    break;
                case Difficulty.Medium:
                    p.weights[1] = 0.15; p.weights[2] = 0.70; p.weights[3] = 0.15;
                    break;
                default:
                    p.weights[2] = 0.20; p.weights[3] = 0.80;
                    break;
            }
            return p;
        }

        /// <summary>
        /// Perfil de un nivel de la carrera (1 a 100). Todo sube poco a poco:
        ///  - Jugadas al azar: 90% en el nivel 1 (casi regalada), baja rapido hasta 0% en el nivel 60.
        ///  - Profundidad media: de 1 jugada (nivel 1) a 5 jugadas (nivel 100).
        ///  - Despistes (mirar una jugada menos de lo normal): 25% al principio, 0% en el nivel 100.
        /// El nivel 100 es una maquina que mira 5 jugadas siempre y no falla.
        /// </summary>
        public static AiProfile FromCareerLevel(int level)
        {
            double t = Math.Max(0.0, Math.Min(1.0, (level - 1) / 99.0));

            double random = 0.90 * Math.Pow(Math.Max(0.0, 1.0 - t / 0.6), 1.5);
            double rest = 1.0 - random;

            double target = 1.0 + (MaxDepth - 1) * t;
            int low = Math.Min(MaxDepth, (int)Math.Floor(target));
            double frac = low >= MaxDepth ? 0.0 : target - low;

            var p = new AiProfile();
            p.weights[0] = random;
            p.weights[low] += rest * (1.0 - frac);
            if (low < MaxDepth) p.weights[low + 1] += rest * frac;

            // Despistes: una parte de cada profundidad baja un escalon (nunca por debajo de 1)
            double lapse = 0.25 * (1.0 - t);
            for (int d = 2; d <= MaxDepth; d++)
            {
                double moved = p.weights[d] * lapse;
                p.weights[d] -= moved;
                p.weights[d - 1] += moved;
            }
            return p;
        }

        /// <summary>Sortea a cuantas jugadas por delante mira esta vez.</summary>
        public int PickDepth(Random random)
        {
            double r = random.NextDouble();
            double sum = 0;
            for (int d = 0; d <= MaxDepth; d++)
            {
                sum += weights[d];
                if (r < sum) return d;
            }
            return MaxDepth;
        }
    }
}
