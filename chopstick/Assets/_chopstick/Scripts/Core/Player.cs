namespace Chopsticks.Core
{
    /// <summary>
    /// Un jugador: nombre y dedos de cada mano.
    /// Fingers[0] es la mano izquierda y Fingers[1] la derecha.
    /// Una mano con 0 dedos esta eliminada.
    /// </summary>
    public class Player
    {
        public string Name;
        public int[] Fingers = { 1, 1 };

        public Player(string name)
        {
            Name = name;
        }

        public bool HandAlive(int hand)
        {
            return Fingers[hand] > 0;
        }

        /// <summary>El jugador sigue en la partida si alguna mano esta viva.</summary>
        public bool IsAlive
        {
            get { return Fingers[0] > 0 || Fingers[1] > 0; }
        }

        public Player Clone()
        {
            var copy = new Player(Name);
            copy.Fingers = new[] { Fingers[0], Fingers[1] };
            return copy;
        }
    }
}
