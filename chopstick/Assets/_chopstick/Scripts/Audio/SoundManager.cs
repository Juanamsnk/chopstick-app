using System.Collections;
using UnityEngine;

namespace Chopsticks.Audio
{
    /// <summary>
    /// Reproduce los sonidos y la musica de fondo del juego. Se anade como componente a un GameObject
    /// (por ejemplo el mismo "UI" que tiene el UIRoot). Cada sonido tiene su clip y su propio volumen,
    /// y la musica tiene el suyo. Si dejas un hueco vacio, ese sonido simplemente no suena.
    /// </summary>
    [DisallowMultipleComponent]
    public class SoundManager : MonoBehaviour
    {
        [Header("Boton: al pulsar un boton o elegir tu mano")]
        [SerializeField] private AudioClip buttonClip;
        [Range(0f, 1f)] [SerializeField] private float buttonVolume = 1f;

        [Header("Ataque: cuando atacas tu")]
        [SerializeField] private AudioClip attackClip;
        [Range(0f, 1f)] [SerializeField] private float attackVolume = 1f;

        [Header("Golpe: cuando te atacan")]
        [SerializeField] private AudioClip hitClip;
        [Range(0f, 1f)] [SerializeField] private float hitVolume = 1f;

        [Header("Mano perdida: cuando te eliminan una mano (suena en lugar del golpe)")]
        [SerializeField] private AudioClip handLostClip;
        [Range(0f, 1f)] [SerializeField] private float handLostVolume = 1f;

        [Header("Reparto: al repartir los dedos")]
        [SerializeField] private AudioClip splitClip;
        [Range(0f, 1f)] [SerializeField] private float splitVolume = 1f;

        [Header("Victoria: cuando ganas la partida")]
        [SerializeField] private AudioClip winClip;
        [Range(0f, 1f)] [SerializeField] private float winVolume = 1f;

        [Header("Derrota: cuando pierdes la partida")]
        [SerializeField] private AudioClip loseClip;
        [Range(0f, 1f)] [SerializeField] private float loseVolume = 1f;

        [Header("Musica de fondo (se repite en bucle)")]
        [SerializeField] private AudioClip musicClip;
        [Range(0f, 1f)] [SerializeField] private float musicVolume = 0.5f;
        [SerializeField] private bool playMusicOnStart = true;
        [Min(0f)] [SerializeField] private float musicFadeInSeconds = 1.5f;
        [SerializeField] private bool musicMuted;

        [Header("Volumen general (se multiplica por el de cada sonido y el de la musica)")]
        [Range(0f, 1f)] [SerializeField] private float volume = 1f;
        [Tooltip("Silencia los efectos de sonido (la musica tiene su propio interruptor)")]
        [SerializeField] private bool muted;

        private AudioSource source;        // efectos
        private AudioSource musicSource;   // musica
        private float musicFade;           // 0 a 1: sube poco a poco al empezar la musica

        /// <summary>Silencia o activa los efectos de sonido.</summary>
        public bool Muted
        {
            get { return muted; }
            set { muted = value; }
        }

        /// <summary>Silencia o activa la musica de fondo.</summary>
        public bool MusicMuted
        {
            get { return musicMuted; }
            set { musicMuted = value; }
        }

        private void Awake()
        {
            // Si el GameObject no tiene AudioSource, se crea solo.
            source = GetComponent<AudioSource>();
            if (source == null) source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;   // sonido 2D

            // La musica va en su propio AudioSource, dentro de un objeto hijo, para no mezclarse con los efectos
            var musicObject = new GameObject("Musica");
            musicObject.transform.SetParent(transform);
            musicSource = musicObject.AddComponent<AudioSource>();
            musicSource.playOnAwake = false;
            musicSource.loop = true;
            musicSource.spatialBlend = 0f;
        }

        private void Start()
        {
            if (FindAnyObjectByType<AudioListener>() == null)
            {
                Debug.LogWarning("[SoundManager] No hay ningun AudioListener en la escena (suele estar en la Main Camera). Sin el, no se oye nada.");
            }

            if (playMusicOnStart) PlayMusic();
        }

        private void Update()
        {
            if (musicSource == null) return;

            // El volumen de la musica se recalcula siempre: asi los cambios en el Inspector se oyen al momento
            musicFade = musicFadeInSeconds <= 0f ? 1f : Mathf.MoveTowards(musicFade, 1f, Time.unscaledDeltaTime / musicFadeInSeconds);
            musicSource.volume = musicMuted ? 0f : volume * musicVolume * musicFade;
        }

        /// <summary>Empieza la musica de fondo (si no esta ya sonando).</summary>
        public void PlayMusic()
        {
            if (musicSource == null || musicClip == null || musicSource.isPlaying) return;

            musicSource.clip = musicClip;
            musicFade = 0f;
            musicSource.Play();
        }

        /// <summary>Para la musica de fondo.</summary>
        public void StopMusic()
        {
            if (musicSource != null) musicSource.Stop();
        }

        public void Play(SoundId id)
        {
            if (muted || source == null) return;

            AudioClip clip = GetClip(id);
            if (clip == null) return;

            // PlayOneShot permite que varios sonidos suenen a la vez sin cortarse
            source.PlayOneShot(clip, volume * GetVolume(id));
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
                case SoundId.HandLost: return handLostClip;
                case SoundId.Split: return splitClip;
                case SoundId.Win: return winClip;
                case SoundId.Lose: return loseClip;
                default: return null;
            }
        }

        private float GetVolume(SoundId id)
        {
            switch (id)
            {
                case SoundId.Button: return buttonVolume;
                case SoundId.Attack: return attackVolume;
                case SoundId.Hit: return hitVolume;
                case SoundId.HandLost: return handLostVolume;
                case SoundId.Split: return splitVolume;
                case SoundId.Win: return winVolume;
                case SoundId.Lose: return loseVolume;
                default: return 1f;
            }
        }
    }
}
