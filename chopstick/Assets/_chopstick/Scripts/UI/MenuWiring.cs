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

        public MenuWiring(VisualElement root, ScreenNavigator navigator, ThemeManager theme)
        {
            this.root = root;
            this.navigator = navigator;
            this.theme = theme;

            WireMainMenu();
            WireBackButtons();
        }

        // ---- Menu principal, Jugar y Ajustes ----
        private void WireMainMenu()
        {
            var main = root.Q("mainmenu");
            OnClick(main.Q<Button>("btn-play"), () => navigator.Show(ScreenId.Play));
            OnClick(main.Q<Button>("btn-shop"), () => navigator.Show(ScreenId.Shop));
            OnClick(main.Q<Button>("btn-friends"), () => navigator.Show(ScreenId.Friends));
            OnClick(main.Q<Button>("btn-profile"), () => navigator.Show(ScreenId.Settings));
            OnClick(main.Q<Button>("btn-settings"), () => navigator.Show(ScreenId.Settings));
            OnClick(main.Q<Button>("btn-career-card"), () => navigator.Show(ScreenId.Career));   // tarjeta de Carrera: abre la lista de niveles

            // Pantalla Jugar: los modos de juego agrupados
            var play = root.Q("playmenu");
            OnClick(play.Q<Button>("btn-ai"), () => navigator.Show(ScreenId.AiSetup));
            OnClick(play.Q<Button>("btn-career"), () => navigator.Show(ScreenId.Career));
            OnClick(play.Q<Button>("btn-offline"), () => navigator.Show(ScreenId.OfflineSetup));
            OnClick(play.Q<Button>("btn-online"), () => navigator.Show(ScreenId.OnlineMenu));
            OnClick(play.Q<Button>("btn-back"), () => navigator.Show(ScreenId.MainMenu));

            OnClick(root.Q("settings").Q<Button>("btn-back"), () => navigator.Show(ScreenId.MainMenu));
        }

        // ---- Botones de volver / salir ----
        private void WireBackButtons()
        {
            // Las pantallas de un modo vuelven a "Jugar"
            BackFromActionRow("offlinesetup", ScreenId.Play);
            BackFromActionRow("aisetup", ScreenId.Play);
            BackFromActionRow("career", ScreenId.Play);
            BackFromActionRow("onlinemenu", ScreenId.Play);

            // El resto vuelven al menu principal
            BackFromActionRow("shop", ScreenId.MainMenu);
            BackFromActionRow("friends", ScreenId.MainMenu);
            BackFromActionRow("lobby", ScreenId.MainMenu);

            // En la partida el boton se llama btn-exit.
            OnClick(root.Q("game").Q<Button>("btn-exit"), () => navigator.Show(ScreenId.MainMenu));
        }

        private void BackFromActionRow(string screenName, ScreenId target)
        {
            var button = root.Q(screenName).Q("action-row").Q<Button>("btn-secondary");
            OnClick(button, () => navigator.Show(target));
        }

        // ---- Ayudas ----
        private static void OnClick(Button button, Action action)
        {
            if (button == null)
            {
                Debug.LogError("MenuWiring: no se encontro un boton. Revisa los nombres en el UXML.");
                return;
            }
            button.clicked += action;
        }
    }
}
