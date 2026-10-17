using Chopsticks.UI;

namespace Chopsticks.Game
{
    /// <summary>
    /// El director de la pantalla de partida. Recibe los toques, mantiene la mano elegida
    /// y el panel de repartir, se lo pide todo a la partida (IMatch) y le dice a la pantalla
    /// que se redibuje. No sabe si la partida es local u online.
    /// </summary>
    public class GameController
    {
        private readonly GameScreenView view;
        private readonly ScreenNavigator navigator;

        private IMatch match;
        private int selectedHand = -1;   // mano propia elegida para atacar (-1 = ninguna)
        private bool splitting;          // panel de repartir abierto

        public GameController(GameScreenView view, ScreenNavigator navigator)
        {
            this.view = view;
            this.navigator = navigator;

            view.HandClicked += OnHandClicked;
            view.SplitButtonClicked += OnSplitButton;
            view.SplitOptionChosen += OnSplitOption;
            view.ExitClicked += LeaveMatch;
        }

        // ------------------------------------------------------------
        // Empezar y salir
        // ------------------------------------------------------------
        /// <summary>Empieza una partida local (offline o contra la maquina).</summary>
        public void StartGame(GameSetup setup)
        {
            StartMatch(new LocalMatch(setup, view.Schedule));
        }

        /// <summary>Empieza cualquier partida (la usa tambien el modo online).</summary>
        public void StartMatch(IMatch newMatch)
        {
            LeaveMatch();

            match = newMatch;
            match.Changed += Refresh;
            selectedHand = -1;
            splitting = false;

            navigator.Show(ScreenId.Game);
            Refresh();
            match.Begin();
        }

        private void LeaveMatch()
        {
            if (match == null) return;
            match.Changed -= Refresh;
            match.Close();
            match = null;
        }

        // ------------------------------------------------------------
        // Toques
        // ------------------------------------------------------------
        private void OnHandClicked(int player, int hand)
        {
            if (match == null || !match.CanLocalAct) return;
            var state = match.State;

            // Mano propia: la selecciona o la deselecciona
            if (player == state.CurrentPlayer)
            {
                if (!state.Players[player].HandAlive(hand)) return;
                selectedHand = selectedHand == hand ? -1 : hand;
                splitting = false;
                Refresh();
                return;
            }

            // Mano rival: ataca si ya habia elegido una propia
            if (selectedHand < 0) return;
            if (match.TryAttack(selectedHand, player, hand)) AfterMove();
        }

        private void OnSplitButton()
        {
            if (match == null) return;

            // Con la partida terminada este boton es "Jugar otra vez"
            if (match.State.Status == Chopsticks.Core.GameStatus.Over)
            {
                if (!match.CanRestart) return;
                selectedHand = -1;
                splitting = false;
                match.Restart();
                Refresh();
                return;
            }

            if (!match.CanLocalAct || !match.State.SplitAllowed) return;
            splitting = !splitting;
            selectedHand = -1;
            Refresh();
        }

        private void OnSplitOption(int left, int right)
        {
            if (match == null || !match.CanLocalAct) return;
            if (match.TrySplit(left, right)) AfterMove();
        }

        private void AfterMove()
        {
            selectedHand = -1;
            splitting = false;
            Refresh();
        }

        // ------------------------------------------------------------
        // Dibujar
        // ------------------------------------------------------------
        private void Refresh()
        {
            if (match == null || match.State == null) return;

            var data = new GameViewData
            {
                State = match.State,
                BottomPlayer = match.BottomPlayer,
                SelectedHand = selectedHand,
                Splitting = splitting,
                CanAct = match.CanLocalAct,
                Thinking = match.Thinking,
                CanRestart = match.CanRestart,
                SplitOptions = splitting ? match.GetSplitOptions() : null
            };
            view.Render(data);
        }
    }
}
