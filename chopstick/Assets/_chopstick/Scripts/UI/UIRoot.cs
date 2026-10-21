using Chopsticks.Audio;
using Chopsticks.Game;
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
        private OnlineMenuView onlineMenu;
        private LobbyView lobbyView;
        private OnlineController onlineController;

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
            navigator.Register(ScreenId.OfflineSetup, root.Q("offlinesetup"));
            navigator.Register(ScreenId.AiSetup, root.Q("aisetup"));
            navigator.Register(ScreenId.OnlineMenu, root.Q("onlinemenu"));
            navigator.Register(ScreenId.Lobby, root.Q("lobby"));
            navigator.Register(ScreenId.Game, root.Q("game"));
            menuWiring = new MenuWiring(root, navigator, theme);

            // Partida (sirve para offline, contra la maquina y online)
            gameView = new GameScreenView(root.Q("game"));
            gameController = new GameController(gameView, navigator, new MoveSounds(soundManager));

            // Ajustes offline y contra la maquina
            offlineSetup = new OfflineSetupView(root.Q("offlinesetup"));
            offlineSetup.StartClicked += gameController.StartGame;
            aiSetup = new AiSetupView(root.Q("aisetup"));
            aiSetup.StartClicked += gameController.StartGame;

            // Online
            onlineMenu = new OnlineMenuView(root.Q("onlinemenu"));
            lobbyView = new LobbyView(root.Q("lobby"));
            onlineController = new OnlineController(navigator, onlineMenu, lobbyView, gameController);

            navigator.Show(ScreenId.MainMenu);
        }

        private void OnDisable()
        {
            // Si se cierra la interfaz, se cierra tambien la sala online
            if (onlineController != null) onlineController.CleanUp();
        }
    }
}
