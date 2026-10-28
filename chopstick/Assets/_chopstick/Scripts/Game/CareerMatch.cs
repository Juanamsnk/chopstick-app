using System;
using System.Collections.Generic;
using Chopsticks.Career;
using Chopsticks.Core;
using Chopsticks.Shop;

namespace Chopsticks.Game
{
    /// <summary>
    /// Una partida del modo carrera: envuelve una LocalMatch contra la maquina del nivel.
    /// Al ganar apunta el nivel como superado (se guarda en el dispositivo) y el boton
    /// de fin de partida pasa a "Siguiente nivel". Al perder se puede "Reintentar".
    /// </summary>
    public class CareerMatch : IMatch
    {
        private readonly Action<long, Action> schedule;
        private readonly CareerProgress progress;
        private readonly ShopInventory shop;
        private readonly string playerName;   // tu nombre en la partida (null = "Jugador")

        private LocalMatch inner;
        private int level;
        private bool recorded;   // la victoria de esta partida ya se guardo
        private bool closed;
        private int reward;      // monedas ganadas en esta partida

        public event Action Changed;

        public CareerMatch(int level, Action<long, Action> schedule, CareerProgress progress, ShopInventory shop,
                           string playerName = null)
        {
            this.level = level;
            this.playerName = playerName;
            this.schedule = schedule;
            this.progress = progress;
            this.shop = shop;
            CreateInner();
        }

        public int Level { get { return level; } }

        public GameState State { get { return inner.State; } }
        public int BottomPlayer { get { return 0; } }
        public bool CanLocalAct { get { return inner.CanLocalAct; } }
        public bool Thinking { get { return inner.Thinking; } }
        public bool CanRestart { get { return true; } }

        // El rival lleva la mano de su rango durante todo el nivel
        public string OpponentSkinId { get { return CareerOpponentSkins.SkinId(level); } }
        public int OpponentTone { get { return CareerOpponentSkins.Tone(level); } }

        private bool Won
        {
            get { return State.Status == GameStatus.Over && State.Winner == BottomPlayer; }
        }

        public string RestartLabel
        {
            get
            {
                if (!Won) return "Reintentar";
                return level < CareerLevels.Count ? "Siguiente nivel" : "Repetir nivel";
            }
        }

        public string ResultNote
        {
            get { return Won && reward > 0 ? "+" + reward + " monedas" : null; }
        }

        public void Begin()
        {
            inner.Begin();
        }

        public bool TryAttack(int fromHand, int targetPlayer, int targetHand)
        {
            return inner.TryAttack(fromHand, targetPlayer, targetHand);
        }

        public bool TrySplit(int leftFingers, int rightFingers)
        {
            return inner.TrySplit(leftFingers, rightFingers);
        }

        public List<int[]> GetSplitOptions()
        {
            return inner.GetSplitOptions();
        }

        /// <summary>Tras ganar pasa al nivel siguiente; en cualquier otro caso repite el mismo.</summary>
        public void Restart()
        {
            if (Won && level < CareerLevels.Count) level++;
            CreateInner();
            inner.Begin();
            Raise();
        }

        public void Close()
        {
            closed = true;
            DisposeInner();
        }

        private void CreateInner()
        {
            DisposeInner();
            inner = new LocalMatch(CareerLevels.CreateSetup(level, playerName), schedule);
            inner.Changed += OnInnerChanged;
            recorded = false;
            reward = 0;
        }

        private void DisposeInner()
        {
            if (inner == null) return;
            inner.Changed -= OnInnerChanged;
            inner.Close();
        }

        private void OnInnerChanged()
        {
            if (!recorded && Won)
            {
                recorded = true;
                // Primera vez: premio completo. Repetir un nivel ya superado: premio pequeno.
                bool firstTime = progress.Complete(level);
                reward = firstTime ? CareerRewards.FirstClear(level) : CareerRewards.ReplayReward;
                shop.AddCoins(reward);
            }
            Raise();
        }

        private void Raise()
        {
            if (!closed && Changed != null) Changed();
        }
    }
}
