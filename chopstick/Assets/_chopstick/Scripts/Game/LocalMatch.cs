using System;
using System.Collections.Generic;
using Chopsticks.AI;
using Chopsticks.Core;

namespace Chopsticks.Game
{
    /// <summary>
    /// Partida en este dispositivo: offline entre varias personas o contra la maquina.
    /// Si hay maquina, ella es el jugador 2 y juega sola tras una pequena pausa.
    /// </summary>
    public class LocalMatch : IMatch
    {
        private const int AiPlayerIndex = 1;
        private const long AiThinkMs = 900;

        private readonly GameEngine engine;
        private readonly AIPlayer ai;                      // null si no hay maquina
        private readonly Action<long, Action> schedule;    // para esperar sin bloquear
        private bool thinking;
        private bool closed;

        public event Action Changed;

        public LocalMatch(GameSetup setup, Action<long, Action> schedule)
        {
            this.schedule = schedule;
            engine = new GameEngine(setup.Names, setup.SplitAllowed);
            ai = setup.VsAI ? new AIPlayer(setup.Difficulty) : null;
        }

        public GameState State { get { return engine.State; } }
        public int BottomPlayer { get { return 0; } }       // el jugador 1 siempre abajo
        public bool Thinking { get { return thinking; } }
        public bool CanRestart { get { return true; } }

        private bool IsOver { get { return engine.State.Status == GameStatus.Over; } }

        private bool IsAiTurn
        {
            get { return ai != null && !IsOver && engine.State.CurrentPlayer == AiPlayerIndex; }
        }

        public bool CanLocalAct { get { return !IsOver && !IsAiTurn; } }

        public void Begin()
        {
            ScheduleAiIfNeeded();
        }

        public bool TryAttack(int fromHand, int targetPlayer, int targetHand)
        {
            MoveResult result;
            if (!engine.TryAttack(fromHand, targetPlayer, targetHand, out result)) return false;
            AfterMove();
            return true;
        }

        public bool TrySplit(int leftFingers, int rightFingers)
        {
            MoveResult result;
            if (!engine.TrySplit(leftFingers, rightFingers, out result)) return false;
            AfterMove();
            return true;
        }

        public List<int[]> GetSplitOptions()
        {
            return engine.GetSplitOptions();
        }

        public void Restart()
        {
            engine.Restart();
            thinking = false;
            Raise();
            ScheduleAiIfNeeded();
        }

        public void Close()
        {
            closed = true;
        }

        private void AfterMove()
        {
            Raise();
            ScheduleAiIfNeeded();
        }

        private void ScheduleAiIfNeeded()
        {
            if (!IsAiTurn) return;
            thinking = true;
            Raise();
            schedule(AiThinkMs, PlayAiMove);
        }

        private void PlayAiMove()
        {
            thinking = false;
            if (closed || !IsAiTurn) return;

            MoveResult result;
            engine.TryPlay(ai.ChooseMove(engine.State), out result);
            Raise();
        }

        private void Raise()
        {
            if (Changed != null) Changed();
        }
    }
}
