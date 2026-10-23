using UnityEngine;
using UnityEngine.UIElements;

namespace Chopsticks.UI
{
    /// <summary>
    /// Los efectos visuales de un ataque, todos con animaciones cortas:
    ///   1. La mano que ataca se lanza hacia el rival y vuelve (crece un poco).
    ///   2. Cuando llega, la mano atacada tiembla y se tiñe de rojo un momento.
    ///   3. Si te atacan a ti, toda la pantalla da un destello rojo.
    ///   4. Si la mano queda eliminada, el temblor es mas fuerte, la mano se encoge y el destello dura mas.
    /// No cambia ningun dato de la partida: solo mueve y colorea elementos de forma temporal.
    /// </summary>
    public static class AttackEffects
    {
        private const float LungeSeconds = 0.28f;
        private const long HitDelayMs = 140;        // el golpe llega cuando la mano atacante esta en su punto mas lejano
        private const float LungeDistance = 30f;

        /// <param name="attackerIsBottom">true si la mano atacante esta abajo (se lanza hacia arriba).</param>
        /// <param name="flash">Capa roja de pantalla completa; null si el golpe no es contra ti.</param>
        public static void Play(HandView attacker, HandView target, bool attackerIsBottom, bool eliminated, VisualElement flash)
        {
            Lunge(attacker.Root, attackerIsBottom ? -1f : 1f);
            target.Root.schedule.Execute(() => Hit(target.Root, eliminated, flash)).StartingIn(HitDelayMs);
        }

        // La mano que ataca avanza hacia el rival y vuelve
        private static void Lunge(VisualElement hand, float directionY)
        {
            Tween.Run(hand, LungeSeconds,
                t =>
                {
                    float s = Mathf.Sin(t * Mathf.PI);   // 0 -> 1 -> 0
                    hand.style.translate = new Translate(0f, directionY * LungeDistance * s);
                    float grow = 1f + 0.12f * s;
                    hand.style.scale = new Scale(new Vector3(grow, grow, 1f));
                },
                () => Reset(hand));
        }

        // La mano atacada tiembla, se tiñe de rojo y, si la eliminan, se encoge
        private static void Hit(VisualElement hand, bool eliminated, VisualElement flash)
        {
            float amplitude = eliminated ? 18f : 11f;
            float seconds = eliminated ? 0.55f : 0.40f;

            Tween.Run(hand, seconds,
                t =>
                {
                    float fade = 1f - t;
                    float x = amplitude * Mathf.Sin(t * Mathf.PI * 7f) * fade;   // temblor que se va apagando
                    float size = eliminated ? 1f - 0.12f * Mathf.Sin(t * Mathf.PI) : 1f;
                    hand.style.translate = new Translate(x, 0f);
                    hand.style.scale = new Scale(new Vector3(size, size, 1f));
                    hand.style.backgroundColor = new Color(1f, 0.33f, 0.44f, 0.45f * fade);
                },
                () => Reset(hand));

            if (flash != null)
            {
                float strength = eliminated ? 1f : 0.7f;
                Tween.Run(flash, eliminated ? 0.7f : 0.45f,
                    t => flash.style.opacity = strength * (1f - t),
                    () => flash.style.opacity = 0f);
            }
        }

        private static void Reset(VisualElement hand)
        {
            hand.style.translate = StyleKeyword.Null;
            hand.style.scale = StyleKeyword.Null;
            hand.style.backgroundColor = StyleKeyword.Null;
        }
    }
}
