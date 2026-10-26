using Chopsticks.Audio;
using Chopsticks.Career;
using Chopsticks.Game;
using Chopsticks.Shop;
using Chopsticks.Social;
using UnityEngine;
using UnityEngine.UIElements;

namespace Chopsticks.UI
{
    /// <summary>
    /// Punto de entrada de la interfaz. Va en el GameObject que tiene el UIDocument.
    /// Crea todas las piezas y las conecta entre si. No tiene logica propia.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class UIRoot : MonoBehaviour
    {
        private ScreenNavigator navigator;
        private ThemeManager theme;
        private SafeAreaFitter safeArea;
        private KeyboardDismisser keyboard;
        private SoundManager soundManager;
        private ButtonSounds buttonSounds;
        private MenuWiring menuWiring;
        private GameScreenView gameView;
        private GameController gameController;
        private OfflineSetupView offlineSetup;
        private AiSetupView aiSetup;
        private CareerProgress careerProgress;
        private CareerView careerView;
        private ShopInventory shopInventory;
        private ShopView shopView;
        private OnlineMenuView onlineMenu;
        private LobbyView lobbyView;
        private SearchView searchView;
        private OnlineController onlineController;
        private FriendsView friendsView;
        private ChallengePopupView challengePopup;
        private SocialController socialController;
        private PlayerProfile profile;
        private MainMenuView mainMenuView;
        private SettingsView settingsView;

        private void OnEnable()
        {
            VisualElement root = GetComponent<UIDocument>().rootVisualElement;
            VisualElement app = root.Q("app");

            // Sonidos: el SoundManager se anade como componente y los clips se arrastran en el Inspector
            soundManager = GetComponent<SoundManager>();
            if (soundManager == null) soundManager = FindAnyObjectByType<SoundManager>();
            if (soundManager == null) Debug.LogWarning("[UIRoot] No hay ningun SoundManager en la escena: el juego no tendra sonido.");
            buttonSounds = new ButtonSounds(root, soundManager);

            // Tema, zona segura y navegacion
            theme = new ThemeManager(app);
            safeArea = new SafeAreaFitter(app);
            keyboard = new KeyboardDismisser(root);
            navigator = new ScreenNavigator();
            navigator.Shown += id => keyboard.Dismiss();   // al cambiar de pantalla se cierra el teclado
            navigator.Register(ScreenId.MainMenu, root.Q("mainmenu"));
            navigator.Register(ScreenId.Play, root.Q("playmenu"));
            navigator.Register(ScreenId.Settings, root.Q("settings"));
            navigator.Register(ScreenId.OfflineSetup, root.Q("offlinesetup"));
            navigator.Register(ScreenId.AiSetup, root.Q("aisetup"));
            navigator.Register(ScreenId.Career, root.Q("career"));
            navigator.Register(ScreenId.Shop, root.Q("shop"));
            navigator.Register(ScreenId.Friends, root.Q("friends"));
            navigator.Register(ScreenId.OnlineMenu, root.Q("onlinemenu"));
            navigator.Register(ScreenId.Lobby, root.Q("lobby"));
            navigator.Register(ScreenId.Game, root.Q("game"));
            navigator.Register(ScreenId.Search, root.Q("searching"));
            menuWiring = new MenuWiring(root, navigator, theme);

            // Partida (sirve para offline, contra la maquina y online)
            shopInventory = new ShopInventory();
            profile = new PlayerProfile();
            gameView = new GameScreenView(root.Q("game"), shopInventory);
            careerProgress = new CareerProgress();
            gameController = new GameController(gameView, navigator, new MoveSounds(soundManager), careerProgress, shopInventory);

            // Ajustes offline y contra la maquina
            offlineSetup = new OfflineSetupView(root.Q("offlinesetup"));
            offlineSetup.StartClicked += gameController.StartGame;
            aiSetup = new AiSetupView(root.Q("aisetup"));
            aiSetup.StartClicked += gameController.StartGame;

            // Modo carrera: al mostrar la lista se repinta con el progreso guardado
            careerView = new CareerView(root.Q("career"), careerProgress);
            careerView.LevelChosen += gameController.StartCareerLevel;
            navigator.Shown += id => { if (id == ScreenId.Career) careerView.Refresh(); };

            // Tienda: estilos de mano que se compran con las monedas del modo carrera
            shopView = new ShopView(root.Q("shop"), shopInventory);
            navigator.Shown += id => { if (id == ScreenId.Shop) shopView.Refresh(); };

            // Online (crear, unirse con codigo y buscar partida)
            onlineMenu = new OnlineMenuView(root.Q("onlinemenu"));
            lobbyView = new LobbyView(root.Q("lobby"));
            searchView = new SearchView(root.Q("searching"));
            onlineController = new OnlineController(navigator, onlineMenu, lobbyView, searchView, gameController, shopInventory);

            // Menu principal y ajustes (usuario, monedas, sonido, tema)
            mainMenuView = new MainMenuView(root.Q("mainmenu"), shopInventory, profile);
            navigator.Shown += id => { if (id == ScreenId.MainMenu) mainMenuView.Refresh(); };
            settingsView = new SettingsView(root.Q("settings"), shopInventory, profile, soundManager, theme);
            settingsView.ShopClicked += () => navigator.Show(ScreenId.Shop);
            navigator.Shown += id => { if (id == ScreenId.Settings) settingsView.Refresh(); };

            // Si has iniciado sesion, tu nombre se rellena solo en las pantallas de partida
            profile.Changed += ApplyProfileName;
            ApplyProfileName();

            // Amigos (Firebase): iniciar sesion con Google, anadir amigos y retarlos
            friendsView = new FriendsView(root.Q("friends"));
            challengePopup = new ChallengePopupView(root.Q("challenge"));
            socialController = new SocialController(navigator, friendsView, challengePopup, onlineController, profile, settingsView);
            socialController.Start();

            navigator.Show(ScreenId.MainMenu);
        }

        private void ApplyProfileName()
        {
            string name = profile.IsSignedIn ? profile.Name : "";
            offlineSetup.SetProfileName(name);
            aiSetup.SetProfileName(name);
            onlineMenu.SetProfileName(name);
        }

        private void OnDisable()
        {
            // Si se cierra la interfaz, se cierra tambien la sala online
            if (onlineController != null) onlineController.CleanUp();
            if (socialController != null) socialController.Stop();
        }
    }
}
