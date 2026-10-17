using System.Collections.Generic;
using Chopsticks.Core;

namespace Chopsticks.UI
{
    /// <summary>Todo lo que la pantalla de partida necesita para dibujarse.</summary>
    public class GameViewData
    {
        public GameState State;
        public int BottomPlayer;            // que jugador va en la tarjeta de abajo
        public int SelectedHand = -1;       // mano propia elegida (-1 = ninguna)
        public bool Splitting;              // panel de repartir abierto
        public bool CanAct;                 // el jugador puede tocar
        public bool Thinking;               // la maquina esta pensando
        public List<int[]> SplitOptions;    // repartos posibles (solo si Splitting)
    }
}
