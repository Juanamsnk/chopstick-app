using System;
using Chopsticks.Career;
using Chopsticks.Shop;
using UnityEngine.UIElements;

namespace Chopsticks.UI
{
    /// <summary>
    /// Pantalla del modo carrera: los 100 niveles agrupados por rango.
    /// Verde = superado, amarillo = el que toca, apagado = bloqueado.
    /// El boton "Continuar" abre el nivel que toca.
    /// </summary>
    public class CareerView
    {
        private readonly CareerProgress progress;
        private readonly ScrollView scroll;
        private readonly Label progressLabel;
        private readonly Button continueButton;
        private readonly Button[] levelButtons = new Button[CareerLevels.Count + 1];   // posicion = numero de nivel
        private readonly Label[] rewardLabels = new Label[CareerLevels.Count + 1];

        public event Action<int> LevelChosen;

        public CareerView(VisualElement screen, CareerProgress progress)
        {
            this.progress = progress;
            scroll = screen.Q<ScrollView>("levels");
            progressLabel = screen.Q<Label>("career-progress");
            continueButton = screen.Q("action-row").Q<Button>("btn-primary");

            BuildLevels();
            continueButton.clicked += () => Choose(progress.NextLevel);
            Refresh();
        }

        private void BuildLevels()
        {
            for (int tier = 0; tier < CareerLevels.TierCount; tier++)
            {
                int first = tier * CareerLevels.LevelsPerTier + 1;
                int last = first + CareerLevels.LevelsPerTier - 1;

                int tierCoins = 0;
                for (int l = first; l <= last; l++) tierCoins += CareerRewards.FirstClear(l);

                var title = new Label(CareerLevels.TierNameByIndex(tier) + "  (" + first + " - " + last + ")  ·  " + tierCoins + " monedas");
                title.AddToClassList("career__tier");
                scroll.Add(title);

                var row = new VisualElement();
                row.AddToClassList("level-row");
                scroll.Add(row);

                for (int level = first; level <= last; level++)
                {
                    int chosen = level;
                    var button = new Button();
                    button.AddToClassList("level-btn");
                    if (level % CareerLevels.LevelsPerTier == 0) button.AddToClassList("level-btn--bonus");   // incluye bonus de rango

                    var num = new Label(level.ToString());
                    num.AddToClassList("level-btn__num");
                    num.pickingMode = PickingMode.Ignore;
                    button.Add(num);

                    var reward = new VisualElement();
                    reward.AddToClassList("level-btn__reward");
                    reward.pickingMode = PickingMode.Ignore;
                    var coin = new VisualElement();
                    coin.AddToClassList("level-btn__coin");
                    coin.pickingMode = PickingMode.Ignore;
                    var coins = new Label();
                    coins.AddToClassList("level-btn__coins");
                    coins.pickingMode = PickingMode.Ignore;
                    reward.Add(coin);
                    reward.Add(coins);
                    button.Add(reward);
                    rewardLabels[level] = coins;

                    button.clicked += () => Choose(chosen);
                    row.Add(button);
                    levelButtons[level] = button;
                }
            }
        }

        private void Choose(int level)
        {
            if (!progress.IsUnlocked(level)) return;
            if (LevelChosen != null) LevelChosen(level);
        }

        /// <summary>Repinta la lista con el progreso actual. Se llama cada vez que se muestra la pantalla.</summary>
        public void Refresh()
        {
            int next = progress.NextLevel;
            bool allDone = progress.Completed >= CareerLevels.Count;

            for (int level = 1; level <= CareerLevels.Count; level++)
            {
                Button b = levelButtons[level];
                b.EnableInClassList("level-btn--done", progress.IsCompleted(level));
                b.EnableInClassList("level-btn--current", !allDone && level == next);
                b.EnableInClassList("level-btn--locked", !progress.IsUnlocked(level));

                // Monedas que da este nivel: la primera vez lo normal, y despues solo la repeticion
                int coins = progress.IsCompleted(level) ? CareerRewards.ReplayReward : CareerRewards.FirstClear(level);
                rewardLabels[level].text = "+" + coins;
            }

            progressLabel.text = allDone
                ? "¡Carrera completada! 100 de 100"
                : "Superados " + progress.Completed + " de " + CareerLevels.Count;
            continueButton.text = allDone ? "Repetir nivel 100" : "Continuar · Nivel " + next;

            // Lleva la lista hasta el nivel que toca (espera un momento a que la pantalla tenga tamano)
            Button target = levelButtons[next];
            scroll.schedule.Execute(() => scroll.ScrollTo(target)).StartingIn(60);
        }
    }
}
