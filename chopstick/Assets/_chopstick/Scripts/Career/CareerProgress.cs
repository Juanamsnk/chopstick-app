using System;
using UnityEngine;

namespace Chopsticks.Career
{
    /// <summary>
    /// El progreso del modo carrera, guardado en el dispositivo (PlayerPrefs).
    /// Solo se guarda un numero: cuantos niveles seguidos has superado.
    /// El siguiente nivel se desbloquea al ganar el anterior.
    /// </summary>
    public class CareerProgress
    {
        private const string Key = "chopsticks.career.completed";

        /// <summary>Niveles superados (0 a 100). Se han superado todos los niveles hasta este.</summary>
        public int Completed { get; private set; }

        public CareerProgress()
        {
            Completed = Math.Max(0, Math.Min(CareerLevels.Count, PlayerPrefs.GetInt(Key, 0)));
        }

        public bool IsCompleted(int level) { return level <= Completed; }
        public bool IsUnlocked(int level) { return level <= Completed + 1; }

        /// <summary>El nivel que toca jugar ahora (el ultimo si ya los has pasado todos).</summary>
        public int NextLevel { get { return Math.Min(Completed + 1, CareerLevels.Count); } }

        /// <summary>Apunta un nivel como superado y lo guarda. Devuelve true si es un avance nuevo.</summary>
        public bool Complete(int level)
        {
            if (level <= Completed || level > CareerLevels.Count) return false;
            Completed = level;
            PlayerPrefs.SetInt(Key, Completed);
            PlayerPrefs.Save();
            return true;
        }

        /// <summary>Borra el progreso (por si mas adelante quieres un boton de reiniciar).</summary>
        public void Reset()
        {
            Completed = 0;
            PlayerPrefs.DeleteKey(Key);
            PlayerPrefs.Save();
        }
    }
}
