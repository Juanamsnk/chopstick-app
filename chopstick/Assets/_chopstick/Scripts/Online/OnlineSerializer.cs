using Chopsticks.Core;
using UnityEngine;

namespace Chopsticks.Online
{
    /// <summary>Convierte el estado y las jugadas del juego a texto JSON y al reves.</summary>
    public static class OnlineSerializer
    {
        // ---------------- Jugadas ----------------
        public static string MoveToJson(Move move)
        {
            var dto = new MoveDto
            {
                type = (int)move.Type,
                fromHand = move.FromHand,
                targetPlayer = move.TargetPlayer,
                targetHand = move.TargetHand,
                left = move.LeftFingers,
                right = move.RightFingers
            };
            return JsonUtility.ToJson(dto);
        }

        public static Move MoveFromJson(string json)
        {
            var dto = JsonUtility.FromJson<MoveDto>(json);
            if (dto.type == (int)MoveType.Attack) return Move.Attack(dto.fromHand, dto.targetPlayer, dto.targetHand);
            return Move.Split(dto.left, dto.right);
        }

        // ---------------- Estado ----------------
        public static string StateToJson(GameState state)
        {
            int n = state.Players.Count;
            var dto = new StateDto
            {
                names = new string[n],
                fingers = new int[n * 2],
                current = state.CurrentPlayer,
                status = (int)state.Status,
                winner = state.Winner,
                splitAllowed = state.SplitAllowed,
                moveCount = state.MoveCount
            };

            for (int i = 0; i < n; i++)
            {
                dto.names[i] = state.Players[i].Name;
                dto.fingers[i * 2] = state.Players[i].Fingers[0];
                dto.fingers[i * 2 + 1] = state.Players[i].Fingers[1];
            }

            MoveResult m = state.LastMove;
            if (m != null)
            {
                dto.hasLast = true;
                dto.lastType = (int)m.Type;
                dto.lastPlayer = m.Player;
                dto.lastFromHand = m.FromHand;
                dto.lastTargetPlayer = m.TargetPlayer;
                dto.lastTargetHand = m.TargetHand;
                dto.lastAttackerFingers = m.AttackerFingers;
                dto.lastNewValue = m.NewValue;
                dto.lastEliminated = m.Eliminated;
                dto.lastWrapped = m.Wrapped;
                dto.lastLeft = m.LeftFingers;
                dto.lastRight = m.RightFingers;
            }
            return JsonUtility.ToJson(dto);
        }

        public static GameState StateFromJson(string json)
        {
            var dto = JsonUtility.FromJson<StateDto>(json);
            var state = new GameState
            {
                CurrentPlayer = dto.current,
                Status = (GameStatus)dto.status,
                Winner = dto.winner,
                SplitAllowed = dto.splitAllowed,
                MoveCount = dto.moveCount
            };

            for (int i = 0; i < dto.names.Length; i++)
            {
                var player = new Player(dto.names[i]);
                player.Fingers[0] = dto.fingers[i * 2];
                player.Fingers[1] = dto.fingers[i * 2 + 1];
                state.Players.Add(player);
            }

            if (dto.hasLast)
            {
                state.LastMove = new MoveResult
                {
                    Type = (MoveType)dto.lastType,
                    Player = dto.lastPlayer,
                    FromHand = dto.lastFromHand,
                    TargetPlayer = dto.lastTargetPlayer,
                    TargetHand = dto.lastTargetHand,
                    AttackerFingers = dto.lastAttackerFingers,
                    NewValue = dto.lastNewValue,
                    Eliminated = dto.lastEliminated,
                    Wrapped = dto.lastWrapped,
                    LeftFingers = dto.lastLeft,
                    RightFingers = dto.lastRight
                };
            }
            return state;
        }
    }
}
