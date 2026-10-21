using System;
using Chopsticks.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace Chopsticks.UI
{
    /// <summary>
    /// La pantalla de partida. Solo dibuja lo que le dicen (Render) y avisa de los toques
    /// con eventos. No decide nada del juego.
    /// </summary>
    public class GameScreenView
    {
        // Hay 2 huecos de tarjeta: 0 para el rival (arriba) y 1 para el jugador de abajo.
        private const int MySlot = 1;

        private readonly VisualElement root;
        private readonly TurnBannerView banner;
        private readonly Label log;
        private readonly PlayerCardView[] cards = new PlayerCardView[2];
        private readonly int[] slotPlayer = { -1, -1 };   // que jugador muestra cada hueco
        private readonly VisualElement splitPanel;
        private readonly VisualElement splitOptions;
        private readonly Button splitButton;
        private readonly VisualElement gameOverPanel;   // capa de fin de partida (puede no existir en el UXML)
        private readonly Label gameOverTitle;
        private readonly Label gameOverText;
        private readonly Button againButton;

        public event Action<int, int> HandClicked;       // (jugador, mano)
        public event Action SplitButtonClicked;
        public event Action ExitClicked;
        public event Action<int, int> SplitOptionChosen; // (izquierda, derecha)

        public bool IsVisible { get { return root.style.display.value != DisplayStyle.None; } }

        public GameScreenView(VisualElement gameScreen)
        {
            root = gameScreen;
            banner = new TurnBannerView(root.Q("banner"));
            log = root.Q<Label>("log");

            cards[0] = new PlayerCardView(root.Q("card-opp-0"));
            cards[MySlot] = new PlayerCardView(root.Q("card-me"));

            for (int s = 0; s < cards.Length; s++)
            {
                int slot = s;
                for (int h = 0; h < 2; h++)
                {
                    int hand = h;
                    cards[slot].Hands[hand].Clicked += () =>
                    {
                        if (HandClicked != null && slotPlayer[slot] >= 0) HandClicked(slotPlayer[slot], hand);
                    };
                }
            }

            splitPanel = root.Q("split-panel");
            splitOptions = root.Q("split-options");
            splitButton = root.Q<Button>("btn-split");
            splitButton.clicked += () => { if (SplitButtonClicked != null) SplitButtonClicked(); };
            root.Q<Button>("btn-exit").clicked += () => { if (ExitClicked != null) ExitClicked(); };

            // Boton Cancelar del panel de repartir (el mismo aviso que el boton Repartir: abre o cierra)
            Button cancelSplit = root.Q<Button>("btn-cancel-split");
            if (cancelSplit != null) cancelSplit.clicked += () => { if (SplitButtonClicked != null) SplitButtonClicked(); };

            // Capa de fin de partida
            gameOverPanel = root.Q("gameover");
            gameOverTitle = root.Q<Label>("gameover-title");
            gameOverText = root.Q<Label>("gameover-text");
            againButton = root.Q<Button>("btn-again");
            if (againButton != null) againButton.clicked += () => { if (SplitButtonClicked != null) SplitButtonClicked(); };
            Button exitOver = root.Q<Button>("btn-exit-over");
            if (exitOver != null) exitOver.clicked += () => { if (ExitClicked != null) ExitClicked(); };
        }

        /// <summary>Programa una accion para dentro de unos milisegundos.</summary>
        public void Schedule(long delayMs, Action action)
        {
            root.schedule.Execute(action).StartingIn(delayMs);
        }

        // ------------------------------------------------------------
        // Dibujar todo
        // ------------------------------------------------------------
        public void Render(GameViewData d)
        {
            GameState state = d.State;

            // Reparte los jugadores en los huecos: el de abajo en el suyo, los demas arriba.
            for (int i = 0; i < slotPlayer.Length; i++) slotPlayer[i] = -1;
            int opponentSlot = 0;
            for (int p = 0; p < state.Players.Count; p++)
            {
                int slot = p == d.BottomPlayer ? MySlot : opponentSlot++;
                slotPlayer[slot] = p;
            }

            for (int slot = 0; slot < cards.Length; slot++)
            {
                if (slotPlayer[slot] < 0) cards[slot].Show(false);
                else RenderCard(slot, slotPlayer[slot], d);
            }

            RenderLastMoveMarks(d);
            RenderBanner(d);
            log.text = LogText(state);
            RenderButtons(d);
            RenderSplitPanel(d);
            RenderGameOver(d);
        }

        /// <summary>Muestra la caja de Victoria o Derrota cuando termina la partida.</summary>
        private void RenderGameOver(GameViewData d)
        {
            if (gameOverPanel == null) return;

            bool over = d.State.Status == GameStatus.Over;
            gameOverPanel.style.display = over ? DisplayStyle.Flex : DisplayStyle.None;
            if (!over) return;

            bool won = d.State.Winner == d.BottomPlayer;
            gameOverTitle.text = won ? "Victoria" : "Derrota";
            gameOverTitle.EnableInClassList("gameover__title--lose", !won);
            gameOverText.text = d.State.Players[d.State.Winner].Name + " ha ganado la partida";
            againButton.style.display = d.CanRestart ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void RenderCard(int slot, int player, GameViewData d)
        {
            GameState state = d.State;
            Player p = state.Players[player];
            Color color = PlayerColors.Get(player);
            bool isCurrent = state.Status == GameStatus.Playing && state.CurrentPlayer == player;

            PlayerCardView card = cards[slot];
            card.Show(true);
            card.SetOpponent(slot != MySlot);
            card.SetPlayer(p.Name, color);
            card.SetTurn(isCurrent, color);
            card.SetDead(!p.IsAlive);

            for (int h = 0; h < 2; h++)
            {
                HandView hand = card.Hands[h];
                hand.SetFingers(p.Fingers[h]);
                hand.ClearMark();
                hand.SetSelected(d.CanAct && isCurrent && !d.Splitting && d.SelectedHand == h);
                hand.SetTarget(d.CanAct && !d.Splitting && d.SelectedHand >= 0 && !isCurrent && p.Fingers[h] > 0);
            }
        }

        /// <summary>Marca en las manos lo que paso en la ultima jugada.</summary>
        private void RenderLastMoveMarks(GameViewData d)
        {
            MoveResult m = d.State.LastMove;
            if (m == null || d.SelectedHand >= 0) return;   // mientras eliges una mano, se quitan las marcas

            if (m.Type == MoveType.Attack)
            {
                HandOf(m.Player, m.FromHand).SetMark("Ataca", PlayerColors.Get(m.Player));

                string text = "+" + m.AttackerFingers;
                if (m.Eliminated) text += " Eliminada";
                else if (m.Wrapped) text += " pasa a " + m.NewValue;
                HandOf(m.TargetPlayer, m.TargetHand).SetMark(text, PlayerColors.Danger);
            }
            else
            {
                Color color = PlayerColors.Get(m.Player);
                HandOf(m.Player, 0).SetMark("Reparte", color);
                HandOf(m.Player, 1).SetMark("Reparte", color);
            }
        }

        private HandView HandOf(int player, int hand)
        {
            for (int slot = 0; slot < slotPlayer.Length; slot++)
            {
                if (slotPlayer[slot] == player) return cards[slot].Hands[hand];
            }
            return cards[MySlot].Hands[hand];
        }

        private void RenderBanner(GameViewData d)
        {
            GameState state = d.State;

            if (state.Status == GameStatus.Over)
            {
                banner.Set(state.Players[state.Winner].Name + " gana", "Fin de la partida",
                    PlayerColors.Get(state.Winner), false);
                return;
            }

            Player current = state.Players[state.CurrentPlayer];
            string hint;
            if (d.Thinking) hint = "La máquina está pensando...";
            else if (!d.CanAct) hint = "Esperando la jugada de " + current.Name;
            else if (d.Splitting) hint = "Elige cómo repartir tus dedos";
            else if (d.SelectedHand < 0) hint = "1) Elige una de tus manos";
            else hint = "2) Toca una mano rival (borde verde)";

            banner.Set("Turno de " + current.Name, hint, PlayerColors.Get(state.CurrentPlayer), !d.CanAct);
        }

        private static string LogText(GameState state)
        {
            MoveResult m = state.LastMove;
            if (m == null) return "¡Empieza la partida!";

            string who = state.Players[m.Player].Name;
            if (m.Type == MoveType.Split)
            {
                return who + " repartió sus dedos: " + m.LeftFingers + " / " + m.RightFingers;
            }

            string target = state.Players[m.TargetPlayer].Name;
            string result = m.Eliminated ? "¡mano eliminada!" : "ahora tiene " + m.NewValue + " dedos";
            return who + " tocó a " + target + ": " + result;
        }

        private void RenderButtons(GameViewData d)
        {
            // Con la partida terminada, el boton se convierte en "Jugar otra vez".
            if (d.State.Status == GameStatus.Over)
            {
                splitButton.style.display = d.CanRestart ? DisplayStyle.Flex : DisplayStyle.None;
                splitButton.text = "Jugar otra vez";
                splitButton.SetEnabled(true);
                return;
            }

            splitButton.style.display = d.State.SplitAllowed ? DisplayStyle.Flex : DisplayStyle.None;
            splitButton.text = d.Splitting ? "Cancelar reparto" : "Repartir dedos";
            splitButton.SetEnabled(d.CanAct && d.CanSplit);
        }

        private void RenderSplitPanel(GameViewData d)
        {
            bool show = d.Splitting && d.CanAct && d.State.Status == GameStatus.Playing;
            splitPanel.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
            splitOptions.Clear();
            if (!show || d.SplitOptions == null) return;

            if (d.SplitOptions.Count == 0)
            {
                var none = new Label("No hay repartos posibles ahora.");
                none.AddToClassList("text");
                splitOptions.Add(none);
                return;
            }

            foreach (int[] option in d.SplitOptions)
            {
                int left = option[0];
                int right = option[1];
                var button = new Button();
                button.text = left + " / " + right;
                button.AddToClassList("btn");
                button.AddToClassList("btn--ghost");
                button.AddToClassList("split-option");
                button.clicked += () => { if (SplitOptionChosen != null) SplitOptionChosen(left, right); };
                splitOptions.Add(button);
            }
        }
    }
}
