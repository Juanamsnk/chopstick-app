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
    /// un acceso a la tienda y el boton de apoyo (Ko-fi). Solo dibuja y avisa con eventos; la sesion la gestiona SocialController.
    /// </summary>
    public class SettingsView
    {
        private const string SoundKey = "chopsticks_sound_on";
        private const string MusicKey = "chopsticks_music_on";
        private const string KofiUrl = "https://ko-fi.com/juanmasnk";

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
        private readonly VisualElement nameCard;
        private readonly TextField nameField;
        private readonly Button nameSaveButton;
        private readonly Label nameMessageLabel;
        private readonly Label versionLabel;

        private bool firebaseReady;
        private string signedOutHint = "";
        private string lastShownName;   // nombre que se puso por ultima vez en el campo (para no pisar lo que escribes)

        public event Action GoogleClicked;
        public event Action SignOutClicked;
        public event Action<string> NameSaveClicked;
        public event Action ShopClicked;
        public event Action InventoryClicked;

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
            nameCard = screen.Q("s-name-card");
            nameField = screen.Q("s-name-edit").Q<TextField>("input");
            nameField.maxLength = PlayerProfile.MaxNameLength;
            nameSaveButton = screen.Q<Button>("btn-s-name-save");
            nameMessageLabel = screen.Q<Label>("s-name-msg");
            versionLabel = screen.Q<Label>("s-version");

            // La version sale directa de Project Settings > Player > Version
            versionLabel.text = "Versión " + Application.version;

            googleButton.clicked += () => { if (GoogleClicked != null) GoogleClicked(); };
            signOutButton.clicked += () => { if (SignOutClicked != null) SignOutClicked(); };
            nameSaveButton.clicked += OnNameSave;
            screen.Q<Button>("btn-s-shop").clicked += () => { if (ShopClicked != null) ShopClicked(); };
            screen.Q<Button>("btn-s-inventory").clicked += () => { if (InventoryClicked != null) InventoryClicked(); };
            screen.Q<Button>("btn-s-kofi").clicked += () => Application.OpenURL(KofiUrl);   // abre el navegador del movil

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

        /// <summary>Aviso debajo del campo del nombre (vacio = nada).</summary>
        public void ShowNameMessage(string text, bool isError)
        {
            nameMessageLabel.text = text;
            nameMessageLabel.EnableInClassList("friends__msg--error", isError);
        }

        public void SetBusy(bool busy)
        {
            googleButton.SetEnabled(!busy && firebaseReady);
            nameSaveButton.SetEnabled(!busy);
        }

        private void OnNameSave()
        {
            string typed = (nameField.value ?? "").Trim();
            if (typed.Length == 0)
            {
                ShowNameMessage("Escribe un nombre.", true);
                return;
            }
            if (typed == profile.Name)
            {
                ShowNameMessage("Ese ya es tu nombre.", false);
                return;
            }
            ShowNameMessage("", false);
            if (NameSaveClicked != null) NameSaveClicked(typed);
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

            // Editar el nombre solo tiene sentido con cuenta: se guarda ahi
            nameCard.style.display = signedIn ? DisplayStyle.Flex : DisplayStyle.None;
            if (signedIn)
            {
                if (profile.Name != lastShownName)
                {
                    nameField.value = profile.Name;
                    lastShownName = profile.Name;
                }
            }
            else if (lastShownName != null)
            {
                lastShownName = null;
                ShowNameMessage("", false);
            }

            googleButton.style.display = signedIn ? DisplayStyle.None : DisplayStyle.Flex;
            signOutButton.style.display = signedIn ? DisplayStyle.Flex : DisplayStyle.None;
            googleButton.SetEnabled(firebaseReady);

            hintLabel.text = signedIn
                ? "Tu progreso, tus monedas y tus compras se guardan en tu cuenta: los verás en cualquier móvil donde inicies sesión."
                : (string.IsNullOrEmpty(signedOutHint) ? "Inicia sesión para usar tu nombre y jugar con amigos." : signedOutHint);
        }
    }
}
