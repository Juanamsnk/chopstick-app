using System;
using System.Collections.Generic;
using Chopsticks.Core;

namespace Chopsticks.Game
{
    /// <summary>
    /// Una partida vista desde la pantalla. Puede ser local (offline o contra la maquina)
    /// o online. GameController solo habla con esta interfaz, asi que no le importa cual es.
    /// </summary>
    public interface IMatch
    {
        GameState State { get; }
        int BottomPlayer { get; }       // jugador que se dibuja abajo (el de este dispositivo)
        bool CanLocalAct { get; }       // este dispositivo puede jugar ahora mismo
        bool Thinking { get; }          // la maquina esta pensando
        bool CanRestart { get; }        // este dispositivo puede pedir "Jugar otra vez"
        string RestartLabel { get { return "Jugar otra vez"; } }   // texto del boton al terminar (el modo carrera lo cambia)

        string ResultNote { get { return null; } }   // linea extra en la caja de fin de partida (monedas ganadas)

        // Mano del rival. null = la clasica de siempre (offline y contra la maquina normal)
        string OpponentSkinId { get { return null; } }
        int OpponentTone { get { return 0; } }

        // Rango del rival en el modo carrera (online: el que tiene en su dispositivo). null = no se muestra
        string OpponentRank { get { return null; } }

        event Action Changed;           // el estado cambio: hay que redibujar

        void Begin();                   // se llama cuando la pantalla ya esta lista
        bool TryAttack(int fromHand, int targetPlayer, int targetHand);
        bool TrySplit(int leftFingers, int rightFingers);
        List<int[]> GetSplitOptions();
        void Restart();
        void Close();                   // se llama al salir de la partida
    }
}
