using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Chopsticks.UI
{
    /// <summary>
    /// Ayuda para animar elementos: llama a "step" muchas veces por segundo con un valor t que va de 0 a 1
    /// durante el tiempo indicado, y al terminar llama a "done".
    /// </summary>
    public static class Tween
    {
        public static void Run(VisualElement element, float seconds, Action<float> step, Action done = null)
        {
            float start = Time.realtimeSinceStartup;
            bool finished = false;

            element.schedule.Execute(() =>
            {
                if (finished) return;

                float t = Mathf.Clamp01((Time.realtimeSinceStartup - start) / seconds);
                step(t);

                if (t >= 1f)
                {
                    finished = true;
                    if (done != null) done();
                }
            }).Every(16).Until(() => finished);
        }
    }
}
