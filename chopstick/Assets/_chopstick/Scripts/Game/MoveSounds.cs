using Chopsticks.Audio;
using Chopsticks.Core;

namespace Chopsticks.Game
{
    /// <summary>
    /// Elige el sonido de cada jugada y del final de la partida. Se le pasa el estado cada vez
    /// que la pantalla se redibuja y suena solo cuando hay algo nuevo:
    ///   - atacas tu           -> Attack
    ///   - te atacan           -> Hit (o HandLost si te eliminan la mano)
    ///   - alguien reparte     -> Split
    ///   - termina la partida  -> Win si ganas, Lose si pierdes (un poco despues del golpe final)
    /// Sirve igual para offline, contra la maquina y online (tambien las jugadas del rival).
    /// </summary>
    public class MoveSounds
    {
        // Pausa antes del sonido de ganar o perder, para que no se pise con el ultimo golpe
        private const float EndSoundDelay = 0.5f;

        private readonly SoundManager sounds;
        private MoveResult lastSeen;
        private bool endSoundPlayed;

        public MoveSounds(SoundManager sounds)
        {
            this.sounds = sounds;
        }

        /// <summary>Se llama al empezar una partida nueva.</summary>
        public void Reset()
        {
            lastSeen = null;
            endSoundPlayed = false;
        }

        /// <summary>Sonido de pulsar un boton (lo usa el controlador al elegir tu mano).</summary>
        public void PlayClick()
        {
            if (sounds != null) sounds.Play(SoundId.Button);
        }

        /// <param name="localPlayer">El jugador de este dispositivo (el que va abajo).</param>
        public void Observe(GameState state, int localPlayer)
        {
            ObserveMove(state, localPlayer);
            ObserveEnd(state, localPlayer);
        }

        private void ObserveMove(GameState state, int localPlayer)
        {
            MoveResult move = state.LastMove;
            if (move == null)
            {
                lastSeen = null;
                return;
            }

            // Si es la misma jugada que ya se vio (la pantalla se redibuja varias veces), no suena otra vez.
            if (ReferenceEquals(move, lastSeen)) return;
            lastSeen = move;

            if (sounds == null) return;

            if (move.Type == MoveType.Split)
            {
                sounds.Play(SoundId.Split);
            }
            else if (move.Player == localPlayer)
            {
                sounds.Play(SoundId.Attack);
            }
            else if (move.TargetPlayer == localPlayer)
            {
                // Si te eliminan una mano suena su propio sonido; si no, el golpe normal
                sounds.Play(move.Eliminated ? SoundId.HandLost : SoundId.Hit);
            }
        }

        private void ObserveEnd(GameState state, int localPlayer)
        {
            if (state.Status != GameStatus.Over)
            {
                endSoundPlayed = false;   // la partida sigue (o se reinicio)
                return;
            }

            if (endSoundPlayed) return;
            endSoundPlayed = true;

            if (sounds == null) return;
            sounds.Play(state.Winner == localPlayer ? SoundId.Win : SoundId.Lose, EndSoundDelay);
        }
    }
}
