using System;
using UnityEngine;

namespace Chopsticks.Career
{
    /// <summary>
    /// El progreso del modo carrera. Solo se guarda un numero: cuantos niveles seguidos has superado.
    /// El siguiente nivel se desbloquea al ganar el anterior.
    /// Se guarda en el dispositivo (PlayerPrefs). Invitado y cuenta usan "huecos" distintos (prefijo de la clave);
    /// ProgressSync cambia de hueco al iniciar o cerrar sesion y sube el progreso a Firebase.
    /// </summary>
    public class CareerProgress
    {
        public const string Key = "chopsticks.career.completed";

        private string prefix = "";

        /// <summary>Niveles superados (0 a 100). Se han superado todos los niveles hasta este.</summary>
        public int Completed { get; private set; }

        /// <summary>Cambio el progreso (nivel superado, reinicio o datos de la cuenta).</summary>
        public event Action Changed;

        public CareerProgress()
        {
            Load();
        }

        public bool IsCompleted(int level) { return level <= Completed; }
        public bool IsUnlocked(int level) { return level <= Completed + 1; }

        /// <summary>El nivel que toca jugar ahora (el ultimo si ya los has pasado todos).</summary>
        public int NextLevel { get { return Math.Min(Completed + 1, CareerLevels.Count); } }

        /// <summary>Apunta un nivel como superado y lo guarda. Devuelve true si es un avance nuevo.</summary>
        public bool Complete(int level)
        {
            if (level <= Completed || level > CareerLevels.Count) return false;
            SetCompleted(level);
            return true;
        }

        /// <summary>Pone el progreso directamente (lo usa la sincronizacion con la cuenta).</summary>
        public void SetCompleted(int value)
        {
            Completed = Math.Max(0, Math.Min(CareerLevels.Count, value));
            PlayerPrefs.SetInt(prefix + Key, Completed);
            PlayerPrefs.Save();
            if (Changed != null) Changed();
        }

        /// <summary>Cambia de hueco de guardado ("" = invitado) y carga lo que haya en el.</summary>
        public void UseSlot(string slotPrefix)
        {
            prefix = slotPrefix ?? "";
            Load();
            if (Changed != null) Changed();
        }

        /// <summary>Borra el progreso (por si mas adelante quieres un boton de reiniciar).</summary>
        public void Reset()
        {
            Completed = 0;
            PlayerPrefs.DeleteKey(prefix + Key);
            PlayerPrefs.Save();
            if (Changed != null) Changed();
        }

        private void Load()
        {
            Completed = Math.Max(0, Math.Min(CareerLevels.Count, PlayerPrefs.GetInt(prefix + Key, 0)));
        }
    }
}
