using System;

namespace Chopsticks.Online
{
    // Mensajes que viajan por la red, escritos como texto JSON.
    // Son clases simples de datos, sin logica.

    [Serializable]
    public class JoinDto
    {
        public string name;
        public string skin;   // estilo de mano que lleva puesto
        public int tone;      // tono de piel de ese estilo
        public int rank;      // rango de la carrera + 1 (0 = no se sabe)
    }

    /// <summary>La lista de jugadores de la sala. "seat" es el puesto de quien lo recibe.</summary>
    [Serializable]
    public class LobbyDto
    {
        public string[] names;
        public string[] skins;   // estilo de mano de cada jugador (mismo orden que names)
        public int[] tones;
        public int[] ranks;      // rango de cada jugador + 1 (mismo orden que names)
        public int seat;
    }

    [Serializable]
    public class MoveDto
    {
        public int type;           // 0 = ataque, 1 = reparto
        public int fromHand;
        public int targetPlayer;
        public int targetHand;
        public int left;
        public int right;
    }

    /// <summary>Foto completa de la partida: el anfitrion la envia tras cada jugada.</summary>
    [Serializable]
    public class StateDto
    {
        public string[] names;
        public int[] fingers;      // 2 por jugador: izquierda, derecha
        public int current;
        public int status;         // 0 = jugando, 1 = terminada
        public int winner;
        public bool splitAllowed;
        public int moveCount;

        // Ultima jugada (para las etiquetas)
        public bool hasLast;
        public int lastType;
        public int lastPlayer;
        public int lastFromHand;
        public int lastTargetPlayer;
        public int lastTargetHand;
        public int lastAttackerFingers;
        public int lastNewValue;
        public bool lastEliminated;
        public bool lastWrapped;
        public int lastLeft;
        public int lastRight;
    }
}
