using System;
using Chopsticks.Audio;
using Chopsticks.Shop;
using Chopsticks.Social;
using UnityEngine;
using UnityEngine.UIElements;

namespace Chopsticks.UI
{
    /// <summary>
    /// Ajustes y perfil: tu usuario y tus monedas, iniciar o cerrar sesion, sonido, musica, tema
    /// y un acceso a la tienda. Solo dibuja y avisa con eventos; la sesion la gestiona SocialController.
    /// </summary>
    public class SettingsView
    {
        private const string SoundKey = "chopsticks_sound_on";
        private const string MusicKey = "chopsticks_music_on";

        private readonly ShopInventory inventory;
        private readonly PlayerProfile profile;

        private readonly Label avatar;
        private readonly Label nameLabel;
        private readonly Label statusLabel;
        private readonly Label coinsLabel;
        private readonly Label hintLabel;
        private readonly Label messageLabel;
        private readonly Label handLabel;
        private readonly Button googleButton;
        private readonly Button signOutButton;

        private bool firebaseReady;
        private string signedOutHint = "";

        public event Action GoogleClicked;
        public event Action SignOutClicked;
        public event Action ShopClicked;

        public SettingsView(VisualElement screen, ShopInventory inventory, PlayerProfile profile, SoundManager sounds, ThemeManager theme)
        {
            this.inventory = inventory;
            this.profile = profile;

            avatar = screen.Q<Label>("s-avatar");
            nameLabel = screen.Q<Label>("s-name");
            statusLabel = screen.Q<Label>("s-status");
            coinsLabel = screen.Q<Label>("s-coins");
            hintLabel = screen.Q<Label>("s-hint");
            messageLabel = screen.Q<Label>("s-msg");
            handLabel = screen.Q<Label>("s-hand");
            googleButton = screen.Q<Button>("btn-s-google");
            signOutButton = screen.Q<Button>("btn-s-signout");

            googleButton.clicked += () => { if (GoogleClicked != null) GoogleClicked(); };
            signOutButton.clicked += () => { if (SignOutClicked != null) SignOutClicked(); };
            screen.Q<Button>("btn-s-shop").clicked += () => { if (ShopClicked != null) ShopClicked(); };

            // Sonido y musica: se recuerdan entre partidas
            var soundOption = new CheckOptionView(screen.Q("opt-sound"), PlayerPrefs.GetInt(SoundKey, 1) == 1);
            var musicOption = new CheckOptionView(screen.Q("opt-music"), PlayerPrefs.GetInt(MusicKey, 1) == 1);
            if (sounds != null)
            {
                sounds.Muted = !soundOption.Value;
                sounds.MusicMuted = !musicOption.Value;
            }
            soundOption.Changed += on =>
            {
                if (sounds != null) sounds.Muted = !on;
                PlayerPrefs.SetInt(SoundKey, on ? 1 : 0);
                PlayerPrefs.Save();
            };
            musicOption.Changed += on =>
            {
                if (sounds != null) sounds.MusicMuted = !on;
                PlayerPrefs.SetInt(MusicKey, on ? 1 : 0);
                PlayerPrefs.Save();
            };

            // Tema claro / oscuro
            var darkOption = new CheckOptionView(screen.Q("opt-dark"), theme.IsDark);
            darkOption.Changed += on => theme.SetDark(on);

            profile.Changed += Refresh;
            inventory.Changed += Refresh;
            Refresh();
        }

        // ------------------------------------------------------------
        // Estado de la cuenta (lo pone SocialController)
        // ------------------------------------------------------------
        /// <summary>Sin sesion. "ready" = Firebase listo; si no, el boton espera o se explica por que.</summary>
        public void ShowSignedOut(bool ready, string hint)
        {
            firebaseReady = ready;
            signedOutHint = hint;
            Refresh();
        }

        public void ShowSignedIn()
        {
            firebaseReady = true;
            Refresh();
        }

        public void ShowMessage(string text, bool isError)
        {
            messageLabel.text = text;
            messageLabel.EnableInClassList("friends__msg--error", isError);
        }

        public void SetBusy(bool busy)
        {
            googleButton.SetEnabled(!busy && firebaseReady);
        }

        // ------------------------------------------------------------
        // Dibujo
        // ------------------------------------------------------------
        public void Refresh()
        {
            avatar.text = profile.Initial;
            nameLabel.text = profile.DisplayName;
            coinsLabel.text = inventory.Coins.ToString();
            handLabel.text = inventory.Equipped != null ? inventory.Equipped.Name : "";

            bool signedIn = profile.IsSignedIn;
            statusLabel.text = signedIn ? "Sesión iniciada" : "Sin sesión iniciada";

            googleButton.style.display = signedIn ? DisplayStyle.None : DisplayStyle.Flex;
            signOutButton.style.display = signedIn ? DisplayStyle.Flex : DisplayStyle.None;
            googleButton.SetEnabled(firebaseReady);

            hintLabel.text = signedIn
                ? "Tu progreso, tus monedas y tus compras se guardan en tu cuenta: los verás en cualquier móvil donde inicies sesión."
                : (string.IsNullOrEmpty(signedOutHint) ? "Inicia sesión para usar tu nombre y jugar con amigos." : signedOutHint);
        }
    }
}
