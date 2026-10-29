using UnityEngine;
using UnityEngine.UIElements;

namespace Chopsticks.UI
{
    /// <summary>
    /// La pantalla de "Cargando" del arranque. Tapa toda la interfaz mientras se prepara y no se quita antes
    /// de MinSeconds, asi no hay un salto raro si todo carga muy rapido. Se quita cuando ya esta todo listo
    /// (MarkReady) o, como maximo, a los MaxSeconds (por si no hay conexion y Firebase se queda esperando).
    /// </summary>
    public class LoadingScreen
    {
        private const float MinSeconds = 2f;
        private const float MaxSeconds = 10f;

        private readonly VisualElement overlay;
        private readonly VisualElement fill;
        private readonly Label text;
        private readonly float startTime;
        private readonly IVisualElementScheduledItem tick;
        private bool ready;
        private bool closing;

        public LoadingScreen(VisualElement overlay)
        {
            this.overlay = overlay;
            if (overlay == null) return;   // si el UXML no la tiene, no pasa nada

            fill = overlay.Q("loading-fill");
            text = overlay.Q<Label>("loading-text");
            startTime = Time.realtimeSinceStartup;
            overlay.style.display = DisplayStyle.Flex;
            tick = overlay.schedule.Execute(Tick).Every(40);
        }

        /// <summary>La app ya esta lista (la cuenta y el progreso ya cargaron). Se quita cuando pasen los 2 segundos.</summary>
        public void MarkReady()
        {
            ready = true;
        }

        private void Tick()
        {
            if (closing) return;
            float t = Time.realtimeSinceStartup - startTime;
            bool done = (ready && t >= MinSeconds) || t >= MaxSeconds;

            // La barra sube sola hasta el 90 % en los 2 primeros segundos y se completa al terminar de verdad
            float percent = done ? 100f : 90f * Mathf.Clamp01(t / MinSeconds);
            fill.style.width = Length.Percent(percent);
            text.text = "Cargando" + new string('.', (int)(t * 2.5f) % 4);

            if (!done) return;
            closing = true;
            tick.Pause();
            overlay.pickingMode = PickingMode.Ignore;   // los toques ya pasan a la interfaz
            overlay.AddToClassList("loading--hide");    // se desvanece
            overlay.schedule.Execute(() => overlay.style.display = DisplayStyle.None).StartingIn(400);
        }
    }
}
