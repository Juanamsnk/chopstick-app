using UnityEngine;
using UnityEngine.UIElements;

namespace Chopsticks.UI
{
    /// <summary>
    /// Respeta la zona segura del movil (muesca de la camara, esquinas redondeadas y barras del sistema).
    /// Mueve hacia dentro los bordes de cada pantalla para que ningun boton quede tapado.
    /// Tambien deja libre arriba el hueco del banner de anuncios (AdsManager) para que no tape la interfaz.
    /// El fondo sigue ocupando toda la pantalla porque lo pinta el elemento "app".
    /// </summary>
    public class SafeAreaFitter
    {
        private readonly VisualElement app;

        public SafeAreaFitter(VisualElement app)
        {
            this.app = app;

            // Se recalcula cada vez que cambia el tamano (al arrancar, girar el movil, etc.)
            app.RegisterCallback<GeometryChangedEvent>(evt => Apply());

            // Cuando el banner carga, falla o se oculta, se recalcula el hueco de arriba
            AdsManager.OnReservedHeightChanged += Apply;
            app.RegisterCallback<DetachFromPanelEvent>(evt => AdsManager.OnReservedHeightChanged -= Apply);
            Apply();
        }

        public void Apply()
        {
            if (app.panel == null || Screen.width <= 0) return;

            // Screen.safeArea esta en pixeles de pantalla; la interfaz usa unidades escaladas
            // (por el Scale With Screen Size del Panel Settings), asi que hay que convertir.
            float panelWidth = app.panel.visualTree.layout.width;
            if (float.IsNaN(panelWidth) || panelWidth <= 0f) return;
            float scale = panelWidth / Screen.width;

            Rect safe = Screen.safeArea;
            float left = safe.xMin * scale;
            float right = (Screen.width - safe.xMax) * scale;
            float bottom = safe.yMin * scale;                       // el origen de Screen es abajo
            float top = (Screen.height - safe.yMax) * scale;

            // El banner va pegado arriba (AdPosition.Top): la interfaz empieza justo debajo de el
            top += AdsManager.ReservedHeightPx * scale;

            // Cada hijo de "app" es una pantalla (ocupa todo el espacio).
            foreach (VisualElement screen in app.Children())
            {
                screen.style.left = left;
                screen.style.right = right;
                screen.style.top = top;
                screen.style.bottom = bottom;
            }
        }
    }
}
