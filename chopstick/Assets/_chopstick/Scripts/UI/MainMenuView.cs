using System;
using Chopsticks.Career;
using Chopsticks.Shop;
using Chopsticks.Social;
using UnityEngine.UIElements;

namespace Chopsticks.UI
{
    /// <summary>
    /// Menu principal: arriba tu perfil (nombre y monedas) y en el centro la tarjeta de Carrera,
    /// que muestra tu proximo rival y tiene un boton para jugarlo directamente.
    /// Solo dibuja; los botones de navegacion los conecta MenuWiring.
    /// </summary>
    public class MainMenuView
    {
        private readonly ShopInventory inventory;
        private readonly PlayerProfile profile;
        private readonly CareerProgress career;
        private readonly Label avatar;
        private readonly Label nameLabel;
        private readonly Label coinsLabel;
        private readonly Label rankLabel;   // puede no existir en el UXML

        // Tarjeta de Carrera (pueden no existir si el UXML es antiguo)
        private readonly VisualElement careerCard;
        private readonly Label tierLabel;
        private readonly Label badgeLabel;
        private readonly Label opponentLabel;
        private readonly Label rewardLabel;
        private readonly Label progressLabel;
        private readonly VisualElement fill;
        private readonly Button playButton;

        /// <summary>Se pulso "Siguiente duelo": llega el nivel que hay que jugar.</summary>
        public event Action<int> CareerPlayClicked;

        public MainMenuView(VisualElement screen, ShopInventory inventory, PlayerProfile profile, CareerProgress career)
        {
            this.inventory = inventory;
            this.profile = profile;
            this.career = career;

            avatar = screen.Q<Label>("avatar");
            nameLabel = screen.Q<Label>("profile-name");
            coinsLabel = screen.Q<Label>("profile-coins");
            rankLabel = screen.Q<Label>("profile-rank");

            careerCard = screen.Q("career-card");
            tierLabel = screen.Q<Label>("cc-tier");
            badgeLabel = screen.Q<Label>("cc-badge");
            opponentLabel = screen.Q<Label>("cc-opponent");
            rewardLabel = screen.Q<Label>("cc-reward");
            progressLabel = screen.Q<Label>("cc-progress");
            fill = screen.Q("cc-fill");
            playButton = screen.Q<Button>("btn-career-play");

            if (playButton != null)
                playButton.clicked += () => { if (CareerPlayClicked != null) CareerPlayClicked(career.NextLevel); };

            profile.Changed += Refresh;
            inventory.Changed += Refresh;
            Refresh();
        }

        public void Refresh()
        {
            avatar.text = profile.Initial;
            nameLabel.text = profile.DisplayName;
            coinsLabel.text = inventory.Coins.ToString();
            if (rankLabel != null) rankLabel.text = inventory.RankName ?? "";

            RefreshCareerCard();
        }

        private void RefreshCareerCard()
        {
            if (careerCard == null || career == null) return;

            int next = career.NextLevel;
            bool allDone = career.Completed >= CareerLevels.Count;

            tierLabel.text = CareerLevels.TierName(next);
            badgeLabel.text = next.ToString();
            opponentLabel.text = CareerLevels.OpponentName(next);

            int coins = career.IsCompleted(next) ? CareerRewards.ReplayReward : CareerRewards.FirstClear(next);
            rewardLabel.text = "+" + coins;

            progressLabel.text = allDone
                ? "¡Carrera completada! 100 de 100"
                : career.Completed + " de " + CareerLevels.Count + " niveles";
            fill.style.width = new Length(100f * career.Completed / CareerLevels.Count, LengthUnit.Percent);

            if (playButton != null) playButton.text = allDone ? "Repetir duelo" : "Siguiente duelo";
        }
    }
}
