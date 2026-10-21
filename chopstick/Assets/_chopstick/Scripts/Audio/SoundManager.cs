using System.Collections;
using UnityEngine;

namespace Chopsticks.Audio
{
    /// <summary>
    /// Reproduce los sonidos del juego. Se anade como componente a un GameObject
    /// (por ejemplo el mismo "UI" que tiene el UIRoot) y los sonidos se arrastran desde el Inspector.
    /// Si dejas un hueco vacio, ese sonido simplemente no suena.
    /// </summary>
    [DisallowMultipleComponent]
    public class SoundManager : MonoBehaviour
    {
        [Header("Sonidos (arrastra aqui los AudioClip)")]
        [SerializeField] private AudioClip buttonClip;   // al pulsar un boton
        [SerializeField] private AudioClip attackClip;   // cuando atacas tu
        [SerializeField] private AudioClip hitClip;      // cuando te atacan
        [SerializeField] private AudioClip splitClip;    // al repartir los dedos
        [SerializeField] private AudioClip winClip;      // cuando ganas la partida
        [SerializeField] private AudioClip loseClip;     // cuando pierdes la partida

        [Header("Volumen")]
        [Range(0f, 1f)] [SerializeField] private float volume = 1f;
        [SerializeField] private bool muted;

        private AudioSource source;

        public bool Muted
        {
            get { return muted; }
            set { muted = value; }
        }

        private void Awake()
        {
            // Si el GameObject no tiene AudioSource, se crea solo.
            source = GetComponent<AudioSource>();
            if (source == null) source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;   // sonido 2D
        }

        private void Start()
        {
            if (FindAnyObjectByType<AudioListener>() == null)
            {
                Debug.LogWarning("[SoundManager] No hay ningun AudioListener en la escena (suele estar en la Main Camera). Sin el, no se oye nada.");
            }
        }

        public void Play(SoundId id)
        {
            if (muted || source == null) return;

            AudioClip clip = GetClip(id);
            if (clip == null) return;

            // PlayOneShot permite que varios sonidos suenen a la vez sin cortarse
            source.PlayOneShot(clip, volume);
        }

        /// <summary>Reproduce el sonido despues de unos segundos (por ejemplo, para no pisar el golpe final).</summary>
        public void Play(SoundId id, float delaySeconds)
        {
            if (delaySeconds <= 0f) { Play(id); return; }
            StartCoroutine(PlayAfter(id, delaySeconds));
        }

        private IEnumerator PlayAfter(SoundId id, float delaySeconds)
        {
            yield return new WaitForSeconds(delaySeconds);
            Play(id);
        }

        private AudioClip GetClip(SoundId id)
        {
            switch (id)
            {
                case SoundId.Button: return buttonClip;
                case SoundId.Attack: return attackClip;
                case SoundId.Hit: return hitClip;
                case SoundId.Split: return splitClip;
                case SoundId.Win: return winClip;
                case SoundId.Lose: return loseClip;
                default: return null;
            }
        }
    }
}
