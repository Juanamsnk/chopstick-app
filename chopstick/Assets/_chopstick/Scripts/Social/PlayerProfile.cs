using System;

namespace Chopsticks.Social
{
    /// <summary>
    /// Quien esta jugando: el nombre de la cuenta si has iniciado sesion, o invitado si no.
    /// Lo actualiza el SocialController y lo leen el menu, los ajustes y las pantallas de partida.
    /// </summary>
    public class PlayerProfile
    {
        public const string GuestName = "Invitado";

        /// <summary>Nombre de la cuenta; vacio si no hay sesion.</summary>
        public string Name { get; private set; }

        public bool IsSignedIn { get { return !string.IsNullOrEmpty(Name); } }

        /// <summary>Nombre para mostrar: el de la cuenta o "Invitado".</summary>
        public string DisplayName { get { return IsSignedIn ? Name : GuestName; } }

        /// <summary>Letra del circulo de perfil.</summary>
        public string Initial { get { return IsSignedIn ? Name.Substring(0, 1).ToUpperInvariant() : "?"; } }

        public event Action Changed;

        public PlayerProfile() { Name = ""; }

        /// <summary>Pon "" para indicar que no hay sesion.</summary>
        public void Set(string name)
        {
            name = string.IsNullOrWhiteSpace(name) ? "" : name.Trim();
            if (name == Name) return;
            Name = name;
            if (Changed != null) Changed();
        }
    }
}
