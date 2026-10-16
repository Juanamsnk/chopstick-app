using UnityEngine;
using UnityEngine.UIElements;

namespace Chopsticks.UI
{
    /// <summary>
    /// Punto de entrada de la interfaz. Va en el GameObject que tiene el UIDocument.
    /// Solo prepara las piezas (navegador, tema y botones) y arranca en el menu principal.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class UIRoot : MonoBehaviour
    {
        private ScreenNavigator navigator;
        private ThemeManager theme;
        private MenuWiring menuWiring;

        private void OnEnable()
        {
            VisualElement root = GetComponent<UIDocument>().rootVisualElement;

            // "app" es el elemento de App.uxml que lleva la clase del tema.
            VisualElement app = root.Q("app");

            theme = new ThemeManager(app);

            navigator = new ScreenNavigator();
            navigator.Register(ScreenId.MainMenu, root.Q("mainmenu"));
            navigator.Register(ScreenId.OfflineSetup, root.Q("offlinesetup"));
            navigator.Register(ScreenId.AiSetup, root.Q("aisetup"));
            navigator.Register(ScreenId.OnlineMenu, root.Q("onlinemenu"));
            navigator.Register(ScreenId.Lobby, root.Q("lobby"));
            navigator.Register(ScreenId.Game, root.Q("game"));

            menuWiring = new MenuWiring(root, navigator, theme);

            navigator.Show(ScreenId.MainMenu);
        }
    }
}
