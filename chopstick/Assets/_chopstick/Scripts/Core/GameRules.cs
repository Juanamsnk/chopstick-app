using System.Collections.Generic;

namespace Chopsticks.Core
{
    /// <summary>
    /// Todas las reglas del juego en un solo sitio. Son funciones sobre un GameState.
    /// No hay nada de Unity ni de interfaz aqui.
    /// </summary>
    public static class GameRules
    {
        public const int EliminationValue = 5;
        public const int MaxFingersPerHand = 4;

        /// <summary>
        /// Se puede repartir si la opcion esta activada y cada jugador ya ha hecho su primera jugada.
        /// Al inicio de la partida (el primer turno de cada jugador) solo se puede atacar.
        /// </summary>
        public static bool CanSplit(GameState state)
        {
            return state.SplitAllowed
                && state.Status == GameStatus.Playing
                && state.MoveCount >= state.Players.Count;
        }

        // ------------------------------------------------------------
        // Crear partida
        // ------------------------------------------------------------
        public static GameState NewGame(IList<string> names, bool splitAllowed)
        {
            var state = new GameState { SplitAllowed = splitAllowed };
            foreach (var name in names) state.Players.Add(new Player(name));
            return state;
        }

        // ------------------------------------------------------------
        // Jugadas legales
        // ------------------------------------------------------------
        public static bool IsLegal(GameState state, Move move)
        {
            if (state.Status != GameStatus.Playing) return false;
            var me = state.Players[state.CurrentPlayer];

            if (move.Type == MoveType.Attack)
            {
                if (move.FromHand < 0 || move.FromHand > 1) return false;
                if (!me.HandAlive(move.FromHand)) return false;
                if (move.TargetPlayer < 0 || move.TargetPlayer >= state.Players.Count) return false;
                if (move.TargetPlayer == state.CurrentPlayer) return false;
                if (move.TargetHand < 0 || move.TargetHand > 1) return false;
                return state.Players[move.TargetPlayer].HandAlive(move.TargetHand);
            }

            // Reparto
            if (!CanSplit(state)) return false;
            foreach (var option in GetSplitOptions(me))
            {
                if (option[0] == move.LeftFingers && option[1] == move.RightFingers) return true;
            }
            return false;
        }

        public static List<Move> GetLegalMoves(GameState state)
        {
            var moves = new List<Move>();
            if (state.Status != GameStatus.Playing) return moves;
            var me = state.Players[state.CurrentPlayer];

            for (int hand = 0; hand < 2; hand++)
            {
                if (!me.HandAlive(hand)) continue;
                for (int p = 0; p < state.Players.Count; p++)
                {
                    if (p == state.CurrentPlayer) continue;
                    for (int target = 0; target < 2; target++)
                    {
                        if (state.Players[p].HandAlive(target)) moves.Add(Move.Attack(hand, p, target));
                    }
                }
            }

            if (CanSplit(state))
            {
                foreach (var option in GetSplitOptions(me)) moves.Add(Move.Split(option[0], option[1]));
            }
            return moves;
        }

        // ------------------------------------------------------------
        // Repartos posibles
        // ------------------------------------------------------------
        /// <summary>
        /// Devuelve los repartos posibles como pares {izquierda, derecha}.
        /// Reglas: se conserva el total de dedos, cada mano queda entre 0 y 4,
        /// el reparto tiene que ser distinto del actual y no se pueden intercambiar
        /// las manos (1-3 no puede pasar a 3-1). Si las dos manos son iguales,
        /// se puede elegir cual queda con menos dedos.
        /// </summary>
        public static List<int[]> GetSplitOptions(Player player)
        {
            var options = new List<int[]>();
            int left = player.Fingers[0];
            int right = player.Fingers[1];
            int total = left + right;
            int smaller = left < right ? left : right;
            int bigger = left < right ? right : left;
            bool swapped = left > right;

            for (int a = 0; a <= MaxFingersPerHand; a++)
            {
                int b = total - a;
                if (b < a || b > MaxFingersPerHand) continue;       // a siempre es la menor
                if (a == smaller && b == bigger) continue;          // es el mismo reparto

                // Mantiene el orden: la mano que tenia menos sigue teniendo menos.
                options.Add(swapped ? new[] { b, a } : new[] { a, b });

                // Con manos iguales las dos orientaciones son validas.
                if (smaller == bigger && a != b) options.Add(swapped ? new[] { a, b } : new[] { b, a });
            }
            return options;
        }

        // ------------------------------------------------------------
        // Aplicar una jugada (se asume que es legal)
        // ------------------------------------------------------------
        public static MoveResult Apply(GameState state, Move move)
        {
            var me = state.Players[state.CurrentPlayer];
            var result = new MoveResult { Type = move.Type, Player = state.CurrentPlayer };

            if (move.Type == MoveType.Attack)
            {
                var target = state.Players[move.TargetPlayer];
                int sum = target.Fingers[move.TargetHand] + me.Fingers[move.FromHand];

                result.FromHand = move.FromHand;
                result.TargetPlayer = move.TargetPlayer;
                result.TargetHand = move.TargetHand;
                result.AttackerFingers = me.Fingers[move.FromHand];
                result.Eliminated = sum == EliminationValue;
                result.Wrapped = sum > EliminationValue;

                if (result.Eliminated) sum = 0;
                else if (result.Wrapped) sum -= EliminationValue;

                target.Fingers[move.TargetHand] = sum;
                result.NewValue = sum;
            }
            else
            {
                me.Fingers[0] = move.LeftFingers;
                me.Fingers[1] = move.RightFingers;
                result.LeftFingers = move.LeftFingers;
                result.RightFingers = move.RightFingers;
            }

            state.LastMove = result;
            state.MoveCount++;
            AdvanceTurn(state);
            return result;
        }

        // ------------------------------------------------------------
        // Turnos y fin de partida
        // ------------------------------------------------------------
        private static void AdvanceTurn(GameState state)
        {
            int alive = 0;
            int lastAlive = -1;
            for (int i = 0; i < state.Players.Count; i++)
            {
                if (state.Players[i].IsAlive) { alive++; lastAlive = i; }
            }

            if (alive <= 1)
            {
                state.Status = GameStatus.Over;
                state.Winner = lastAlive;
                return;
            }

            // Siguiente jugador que siga vivo
            do
            {
                state.CurrentPlayer = (state.CurrentPlayer + 1) % state.Players.Count;
            }
            while (!state.Players[state.CurrentPlayer].IsAlive);
        }
    }
}
