using System.Collections.Generic;

namespace Chopsticks.Core
{
    public enum GameStatus
    {
        Playing,
        Over
    }

    /// <summary>
    /// Todo lo que define una partida en un momento dado.
    /// Se puede clonar, algo que necesitara la inteligencia artificial.
    /// </summary>
    public class GameState
    {
        public List<Player> Players = new List<Player>();
        public int CurrentPlayer;
        public GameStatus Status = GameStatus.Playing;
        public int Winner = -1;
        public bool SplitAllowed = true;
        public MoveResult LastMove;

        public GameState Clone()
        {
            var copy = new GameState
            {
                CurrentPlayer = CurrentPlayer,
                Status = Status,
                Winner = Winner,
                SplitAllowed = SplitAllowed,
                LastMove = LastMove
            };
            foreach (var p in Players) copy.Players.Add(p.Clone());
            return copy;
        }
    }
}
