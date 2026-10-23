using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Chopsticks.UI
{
    /// <summary>
    /// Conecta los botones de los menus con la navegacion.
    /// Por ahora solo mueve entre pantallas y cambia el tema.
    /// Las acciones de juego (Empezar, Crear partida...) se conectaran mas adelante.
    /// </summary>
    public class MenuWiring
    {
        private readonly VisualElement root;
        private readonly ScreenNavigator navigator;
        private readonly ThemeManager theme;
        private Button themeButton;

        public MenuWiring(VisualElement root, ScreenNavigator navigator, ThemeManager theme)
        {
            this.root = root;
            this.navigator = navigator;
            this.theme = theme;

            WireMainMenu();
            WireBackButtons();
        }

        // ---- Menu principal ----
        private void WireMainMenu()
        {
            var main = root.Q("mainmenu");

            // Los MenuButton son plantillas: el boton real se llama "button" dentro de la plantilla.
            OnClick(MenuButton(main, "btn-offline"), () => navigator.Show(ScreenId.OfflineSetup));
            OnClick(MenuButton(main, "btn-ai"), () => navigator.Show(ScreenId.AiSetup));
            OnClick(MenuButton(main, "btn-online"), () => navigator.Show(ScreenId.OnlineMenu));

            //themeButton = main.Q<Button>("btn-theme");
            //OnClick(themeButton, () =>
            //{
            //    theme.Toggle();
            //    RefreshThemeText();
            //});
            //RefreshThemeText();
        }

        // ---- Botones de volver / salir ----
        private void WireBackButtons()
        {
            // En estas pantallas el boton "Volver" o "Salir" es el secundario de la fila de acciones.
            BackFromActionRow("offlinesetup");
            BackFromActionRow("aisetup");
            BackFromActionRow("onlinemenu");
            BackFromActionRow("lobby");

            // En la partida el boton se llama btn-exit.
            OnClick(root.Q("game").Q<Button>("btn-exit"), () => navigator.Show(ScreenId.MainMenu));
        }

        private void BackFromActionRow(string screenName)
        {
            var button = root.Q(screenName).Q("action-row").Q<Button>("btn-secondary");
            OnClick(button, () => navigator.Show(ScreenId.MainMenu));
        }

        // ---- Ayudas ----
        private static Button MenuButton(VisualElement screen, string instanceName)
        {
            return screen.Q(instanceName).Q<Button>("button");
        }

        private static void OnClick(Button button, Action action)
        {
            if (button == null)
            {
                Debug.LogError("MenuWiring: no se encontro un boton. Revisa los nombres en el UXML.");
                return;
            }
            button.clicked += action;
        }

        private void RefreshThemeText()
        {
            themeButton.text = theme.IsDark ? "Tema: oscuro" : "Tema: claro";
        }
    }
}
