using System;

namespace Chopsticks.Social
{
    /// <summary>Un amigo ya aceptado.</summary>
    public class FriendInfo
    {
        public string Uid;
        public string Name;
    }

    /// <summary>Una peticion de amistad recibida que aun no se ha aceptado.</summary>
    public class FriendRequestInfo
    {
        public string Uid;
        public string Name;
    }

    /// <summary>Un reto a una partida: quien lo envia y el codigo de la sala a la que unirse.</summary>
    public class ChallengeInfo
    {
        public string FromUid;
        public string FromName;
        public string JoinCode;
    }

    /// <summary>Error con un mensaje listo para ensenar al jugador.</summary>
    public class SocialException : Exception
    {
        public SocialException(string message) : base(message) { }
    }
}
