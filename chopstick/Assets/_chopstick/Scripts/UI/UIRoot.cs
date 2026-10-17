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
        private MenuWiring menuWiring;
        private GameScreenView gameView;
        private GameController gameController;
        private OfflineSetupView offlineSetup;
        private AiSetupView aiSetup;

        private void OnEnable()
        {
            VisualElement root = GetComponent<UIDocument>().rootVisualElement;
            VisualElement app = root.Q("app");

            // Tema, zona segura y navegacion
            theme = new ThemeManager(app);
            safeArea = new SafeAreaFitter(app);
            navigator = new ScreenNavigator();
            navigator.Register(ScreenId.MainMenu, root.Q("mainmenu"));
            navigator.Register(ScreenId.OfflineSetup, root.Q("offlinesetup"));
            navigator.Register(ScreenId.AiSetup, root.Q("aisetup"));
            navigator.Register(ScreenId.OnlineMenu, root.Q("onlinemenu"));
            navigator.Register(ScreenId.Lobby, root.Q("lobby"));
            navigator.Register(ScreenId.Game, root.Q("game"));
            menuWiring = new MenuWiring(root, navigator, theme);

            // Partida
            gameView = new GameScreenView(root.Q("game"));
            gameController = new GameController(gameView, navigator);

            // Las pantallas de ajustes avisan al controlador cuando pulsas Empezar
            offlineSetup = new OfflineSetupView(root.Q("offlinesetup"));
            offlineSetup.StartClicked += gameController.StartGame;
            aiSetup = new AiSetupView(root.Q("aisetup"));
            aiSetup.StartClicked += gameController.StartGame;

            navigator.Show(ScreenId.MainMenu);
        }
    }
}
