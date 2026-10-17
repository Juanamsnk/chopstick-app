using Chopsticks.AI;
using Chopsticks.Core;
using Chopsticks.UI;

namespace Chopsticks.Game
{
    /// <summary>
    /// El director de la partida. Recibe los toques de la pantalla, se los pasa a las reglas
    /// (GameEngine), decide cuando juega la maquina y le dice a la pantalla que se redibuje.
    /// No dibuja nada ni conoce las reglas por dentro.
    /// </summary>
    public class GameController
    {
        private const int AiPlayerIndex = 1;     // en el modo contra la maquina, ella es el jugador 2
        private const int BottomPlayer = 0;      // el jugador 1 siempre abajo
        private const long AiThinkMs = 900;      // pausa antes de que mueva la maquina

        private readonly GameScreenView view;
        private readonly ScreenNavigator navigator;

        private GameEngine engine;
        private AIPlayer ai;                     // null si no hay maquina
        private int selectedHand = -1;           // mano propia elegida para atacar (-1 = ninguna)
        private bool splitting;                  // true si el panel de repartir esta abierto
        private bool thinking;                   // true mientras la maquina "piensa"

        public GameController(GameScreenView view, ScreenNavigator navigator)
        {
            this.view = view;
            this.navigator = navigator;

            view.HandClicked += OnHandClicked;
            view.SplitButtonClicked += OnSplitButton;
            view.SplitOptionChosen += OnSplitOption;
        }

        // ------------------------------------------------------------
        // Empezar
        // ------------------------------------------------------------
        public void StartGame(GameSetup setup)
        {
            engine = new GameEngine(setup.Names, setup.SplitAllowed);
            ai = setup.VsAI ? new AIPlayer(setup.Difficulty) : null;
            ResetInteraction();

            navigator.Show(ScreenId.Game);
            Refresh();
            ScheduleAiIfNeeded();
        }

        private void ResetInteraction()
        {
            selectedHand = -1;
            splitting = false;
            thinking = false;
        }

        // ------------------------------------------------------------
        // Estado
        // ------------------------------------------------------------
        private bool IsOver { get { return engine.State.Status == GameStatus.Over; } }

        private bool IsAiTurn
        {
            get { return ai != null && !IsOver && engine.State.CurrentPlayer == AiPlayerIndex; }
        }

        /// <summary>El jugador humano puede tocar la pantalla.</summary>
        private bool CanAct { get { return !IsOver && !IsAiTurn; } }

        // ------------------------------------------------------------
        // Toques del jugador
        // ------------------------------------------------------------
        private void OnHandClicked(int player, int hand)
        {
            if (engine == null || !CanAct) return;
            GameState state = engine.State;

            // Toca una mano propia: la selecciona (o la deselecciona)
            if (player == state.CurrentPlayer)
            {
                if (!state.Players[player].HandAlive(hand)) return;
                selectedHand = selectedHand == hand ? -1 : hand;
                splitting = false;
                Refresh();
                return;
            }

            // Toca una mano rival: ataca si ya habia elegido una propia
            if (selectedHand < 0) return;
            MoveResult result;
            if (engine.TryAttack(selectedHand, player, hand, out result)) AfterMove();
        }

        private void OnSplitButton()
        {
            if (engine == null) return;

            // Con la partida terminada, este boton es "Jugar otra vez"
            if (IsOver)
            {
                engine.Restart();
                ResetInteraction();
                Refresh();
                ScheduleAiIfNeeded();
                return;
            }

            if (!CanAct || !engine.State.SplitAllowed) return;
            splitting = !splitting;
            selectedHand = -1;
            Refresh();
        }

        private void OnSplitOption(int left, int right)
        {
            if (engine == null || !CanAct) return;
            MoveResult result;
            if (engine.TrySplit(left, right, out result)) AfterMove();
        }

        private void AfterMove()
        {
            selectedHand = -1;
            splitting = false;
            Refresh();
            ScheduleAiIfNeeded();
        }

        // ------------------------------------------------------------
        // Maquina
        // ------------------------------------------------------------
        private void ScheduleAiIfNeeded()
        {
            if (!IsAiTurn) return;
            thinking = true;
            Refresh();
            view.Schedule(AiThinkMs, PlayAiMove);
        }

        private void PlayAiMove()
        {
            thinking = false;
            // Si el jugador salio al menu o la partida cambio, no hace nada
            if (engine == null || !view.IsVisible || !IsAiTurn) return;

            Move move = ai.ChooseMove(engine.State);
            MoveResult result;
            engine.TryPlay(move, out result);
            Refresh();
        }

        // ------------------------------------------------------------
        // Dibujar
        // ------------------------------------------------------------
        private void Refresh()
        {
            var data = new GameViewData
            {
                State = engine.State,
                BottomPlayer = BottomPlayer,
                SelectedHand = selectedHand,
                Splitting = splitting,
                CanAct = CanAct,
                Thinking = thinking,
                SplitOptions = splitting ? engine.GetSplitOptions() : null
            };
            view.Render(data);
        }
    }
}
