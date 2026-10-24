using System.Collections.Generic;
using Chopsticks.AI;

namespace Chopsticks.Game
{
    /// <summary>
    /// Los ajustes con los que empieza una partida.
    /// Los rellenan las pantallas de ajustes y los recibe el controlador.
    /// </summary>
    public class GameSetup
    {
        public List<string> Names = new List<string>();
        public bool SplitAllowed = true;

        // Solo para el modo contra la maquina (la maquina es siempre el jugador 2)
        public bool VsAI;
        public Difficulty Difficulty = Difficulty.Medium;

        // Si no es null, manda sobre Difficulty (lo usa el modo carrera para cada nivel)
        public AiProfile AiProfile;
    }
}
