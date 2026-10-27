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
        public bool CanAct;                 // este dispositivo puede jugar ahora
        public bool Thinking;               // la maquina esta pensando
        public bool CanSplit;               // repartir esta permitido ahora (no al inicio de la partida)
        public bool CanRestart;             // se puede pedir "Jugar otra vez"
        public string RestartLabel = "Jugar otra vez";   // texto de ese boton
        public string ResultNote;           // linea extra al terminar (por ejemplo "+33 monedas")
        public string OpponentSkinId;       // estilo de mano del rival (null = clasica)
        public int OpponentTone;
        public string BottomRank;           // rango de quien juega abajo (null = no se muestra)
        public string OpponentRank;         // rango del rival (null = no se muestra)
        public List<int[]> SplitOptions;    // repartos posibles (solo si Splitting)
    }
}
