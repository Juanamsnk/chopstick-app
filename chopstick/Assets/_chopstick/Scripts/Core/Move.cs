namespace Chopsticks.Core
{
    public enum MoveType
    {
        Attack,
        Split
    }

    /// <summary>
    /// Una jugada: o un ataque o un reparto de dedos. Se crea con Move.Attack o Move.Split.
    /// </summary>
    public class Move
    {
        public MoveType Type;

        // Datos de ataque
        public int FromHand;
        public int TargetPlayer;
        public int TargetHand;

        // Datos de reparto (como quedan las dos manos)
        public int LeftFingers;
        public int RightFingers;

        public static Move Attack(int fromHand, int targetPlayer, int targetHand)
        {
            return new Move
            {
                Type = MoveType.Attack,
                FromHand = fromHand,
                TargetPlayer = targetPlayer,
                TargetHand = targetHand
            };
        }

        public static Move Split(int leftFingers, int rightFingers)
        {
            return new Move
            {
                Type = MoveType.Split,
                LeftFingers = leftFingers,
                RightFingers = rightFingers
            };
        }
    }
}
