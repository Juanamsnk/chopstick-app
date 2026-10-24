using System;
using System.Collections.Generic;
using Chopsticks.Core;

namespace Chopsticks.AI
{
    /// <summary>
    /// La maquina. Elige una jugada mirando N jugadas por delante.
    /// Cada vez que juega, sortea a cuantas jugadas por delante mira segun la dificultad:
    ///   Facil:  30% al azar, 70% mira 1
    ///   Medio:  15% mira 1, 70% mira 2, 15% mira 3
    ///   Dificil: 20% mira 2, 80% mira 3
    /// Ese sorteo es lo que la hace batible.
    /// El modo carrera usa un AiProfile propio para cada uno de los 100 niveles.
    /// </summary>
    public class AIPlayer
    {
        private readonly AiProfile profile;
        private readonly Random random = new Random();

        public AIPlayer(Difficulty difficulty) : this(AiProfile.FromDifficulty(difficulty))
        {
        }

        public AIPlayer(AiProfile profile)
        {
            this.profile = profile;
        }

        /// <summary>Devuelve la jugada que hace la maquina en el estado actual.</summary>
        public Move ChooseMove(GameState state)
        {
            List<Move> moves = GameRules.GetLegalMoves(state);
            int depth = PickDepth();

            // Profundidad 0 = jugada al azar
            if (depth == 0) return moves[random.Next(moves.Count)];

            int me = state.CurrentPlayer;
            int bestScore = int.MinValue;
            var bestMoves = new List<Move>();

            foreach (Move move in moves)
            {
                GameState next = state.Clone();
                GameRules.Apply(next, move);
                int score = Score(next, depth - 1, me);

                if (score > bestScore)
                {
                    bestScore = score;
                    bestMoves.Clear();
                    bestMoves.Add(move);
                }
                else if (score == bestScore)
                {
                    bestMoves.Add(move);
                }
            }

            // Si hay varias igual de buenas, elige una al azar
            return bestMoves[random.Next(bestMoves.Count)];
        }

        private int PickDepth()
        {
            return profile.PickDepth(random);
        }

        /// <summary>
        /// Puntuacion de un estado desde el punto de vista del jugador "me".
        /// Cuando le toca a "me" elige lo mejor; cuando le toca al rival, lo peor para "me".
        /// </summary>
        private int Score(GameState state, int depth, int me)
        {
            if (state.Status == GameStatus.Over)
            {
                // Ganar antes (mas profundidad restante) vale mas, perder antes vale menos.
                return state.Winner == me ? 1000 + depth : -(1000 + depth);
            }
            if (depth == 0) return Evaluate(state, me);

            bool maximize = state.CurrentPlayer == me;
            int best = maximize ? int.MinValue : int.MaxValue;

            foreach (Move move in GameRules.GetLegalMoves(state))
            {
                GameState next = state.Clone();
                GameRules.Apply(next, move);
                int score = Score(next, depth - 1, me);
                best = maximize ? Math.Max(best, score) : Math.Min(best, score);
            }
            return best;
        }

        /// <summary>Mas manos vivas mias y menos manos vivas del rival es mejor.</summary>
        private int Evaluate(GameState state, int me)
        {
            int mine = 0;
            int theirs = 0;
            for (int p = 0; p < state.Players.Count; p++)
            {
                int hands = (state.Players[p].Fingers[0] > 0 ? 1 : 0) + (state.Players[p].Fingers[1] > 0 ? 1 : 0);
                if (p == me) mine += hands; else theirs += hands;
            }
            return 20 * (mine - theirs);
        }
    }
}
