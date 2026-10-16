namespace Chopsticks.Core
{
    /// <summary>
    /// Lo que paso en la ultima jugada. La interfaz lo usa para mostrar las etiquetas
    /// (Ataca, +2, Reparte...) y los mensajes. No contiene textos, solo datos.
    /// </summary>
    public class MoveResult
    {
        public MoveType Type;
        public int Player;          // quien jugo

        // Ataque
        public int FromHand;
        public int TargetPlayer;
        public int TargetHand;
        public int AttackerFingers; // dedos que sumo el atacante
        public int NewValue;        // dedos que tiene ahora la mano atacada
        public bool Eliminated;     // la mano llego justo a 5
        public bool Wrapped;        // se paso de 5 y se queda con el resto

        // Reparto
        public int LeftFingers;
        public int RightFingers;
    }
}
