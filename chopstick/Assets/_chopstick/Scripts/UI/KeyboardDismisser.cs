using UnityEngine;
using UnityEngine.UIElements;

namespace Chopsticks.UI
{
    /// <summary>
    /// Cierra el teclado del movil cuando ya no hace falta. Para asegurarse, usa tres cosas:
    ///   1. quita el foco al campo de texto,
    ///   2. desactiva el campo un instante (esto obliga a Unity a soltar el teclado),
    ///   3. le pide a Android que cierre el teclado, y lo repite 150 ms despues por si el primer intento llega pronto.
    /// Se activa al tocar cualquier cosa que no sea un campo de texto (cualquier boton, el fondo...),
    /// al pulsar Intro o Hecho, y al cambiar de pantalla.
    /// </summary>
    public class KeyboardDismisser
    {
        private const long RetryMs = 150;

        private readonly VisualElement root;

        public KeyboardDismisser(VisualElement root)
        {
            this.root = root;

            // TrickleDown = se ejecuta antes que los propios botones.
            root.RegisterCallback<PointerDownEvent>(OnPointerDown, TrickleDown.TrickleDown);
            root.RegisterCallback<ClickEvent>(OnClick, TrickleDown.TrickleDown);
            root.RegisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);
        }

        /// <summary>Cierra el teclado si esta abierto. Es seguro llamarlo siempre.</summary>
        public void Dismiss()
        {
            TextField field = FocusedTextField();
            bool keyboardOpen = field != null || TouchScreenKeyboard.visible;
            if (!keyboardOpen) return;

            Debug.Log("[KeyboardDismisser] Cerrando el teclado");

            if (field != null)
            {
                field.Blur();
                // Desactivar y volver a activar el campo obliga a soltar el teclado
                field.SetEnabled(false);
                field.schedule.Execute(() => field.SetEnabled(true)).StartingIn(50);
            }

            AndroidKeyboard.Hide();
            root.schedule.Execute(() =>
            {
                if (TouchScreenKeyboard.visible) AndroidKeyboard.Hide();
            }).StartingIn(RetryMs);
        }

        private TextField FocusedTextField()
        {
            if (root.panel == null) return null;
            var focused = root.panel.focusController.focusedElement as VisualElement;
            if (focused == null) return null;
            return focused as TextField ?? focused.GetFirstAncestorOfType<TextField>();
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            // Tocar dentro de un campo de texto no lo cierra
            if (IsInsideTextField(evt.target as VisualElement)) return;
            Dismiss();
        }

        private void OnClick(ClickEvent evt)
        {
            if (IsInsideTextField(evt.target as VisualElement)) return;
            Dismiss();
        }

        private void OnKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
            {
                Dismiss();
            }
        }

        private static bool IsInsideTextField(VisualElement element)
        {
            if (element == null) return false;
            return element is TextField || element.GetFirstAncestorOfType<TextField>() != null;
        }
    }
}
