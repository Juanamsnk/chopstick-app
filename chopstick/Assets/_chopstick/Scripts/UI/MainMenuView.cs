using Chopsticks.Shop;
using Chopsticks.Social;
using UnityEngine.UIElements;

namespace Chopsticks.UI
{
    /// <summary>
    /// Menu principal: arriba tu perfil (nombre y monedas); el centro se deja libre.
    /// Solo dibuja; los botones los conecta MenuWiring.
    /// </summary>
    public class MainMenuView
    {
        private readonly ShopInventory inventory;
        private readonly PlayerProfile profile;
        private readonly Label avatar;
        private readonly Label nameLabel;
        private readonly Label coinsLabel;

        public MainMenuView(VisualElement screen, ShopInventory inventory, PlayerProfile profile)
        {
            this.inventory = inventory;
            this.profile = profile;

            avatar = screen.Q<Label>("avatar");
            nameLabel = screen.Q<Label>("profile-name");
            coinsLabel = screen.Q<Label>("profile-coins");

            profile.Changed += Refresh;
            inventory.Changed += Refresh;
            Refresh();
        }

        public void Refresh()
        {
            avatar.text = profile.Initial;
            nameLabel.text = profile.DisplayName;
            coinsLabel.text = inventory.Coins.ToString();
        }
    }
}
