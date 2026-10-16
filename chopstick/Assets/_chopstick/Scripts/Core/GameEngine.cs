using System.Collections.Generic;

namespace Chopsticks.Core
{
    /// <summary>
    /// La partida en marcha. Es lo unico que usaran el resto de scripts:
    /// pide una jugada y devuelve si fue valida y que paso.
    /// </summary>
    public class GameEngine
    {
        private readonly List<string> names;

        public GameState State { get; private set; }

        public GameEngine(IList<string> playerNames, bool splitAllowed)
        {
            names = new List<string>(playerNames);
            State = GameRules.NewGame(names, splitAllowed);
        }

        /// <summary>Empieza otra partida con los mismos jugadores y ajustes.</summary>
        public void Restart()
        {
            State = GameRules.NewGame(names, State.SplitAllowed);
        }

        public bool TryAttack(int fromHand, int targetPlayer, int targetHand, out MoveResult result)
        {
            return TryPlay(Move.Attack(fromHand, targetPlayer, targetHand), out result);
        }

        public bool TrySplit(int leftFingers, int rightFingers, out MoveResult result)
        {
            return TryPlay(Move.Split(leftFingers, rightFingers), out result);
        }

        public bool TryPlay(Move move, out MoveResult result)
        {
            result = null;
            if (!GameRules.IsLegal(State, move)) return false;
            result = GameRules.Apply(State, move);
            return true;
        }

        public List<Move> GetLegalMoves()
        {
            return GameRules.GetLegalMoves(State);
        }

        public List<int[]> GetSplitOptions()
        {
            return GameRules.GetSplitOptions(State.Players[State.CurrentPlayer]);
        }
    }
}
