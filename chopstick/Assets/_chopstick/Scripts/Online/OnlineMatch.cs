using System;
using System.Collections.Generic;
using Chopsticks.Core;
using Chopsticks.Game;
using UnityEngine;

namespace Chopsticks.Online
{
    /// <summary>
    /// Una partida online. El anfitrion lleva las reglas (GameEngine) y es quien manda:
    ///  - los demas le envian la jugada que quieren hacer ("move");
    ///  - el la valida, la aplica y envia a todos el estado nuevo ("state").
    /// Antes de empezar, el anfitrion mantiene la lista de jugadores ("join" / "lobby").
    /// </summary>
    public class OnlineMatch : IMatch, IDisposable
    {
        private const string MsgJoin = "join";
        private const string MsgLobby = "lobby";
        private const string MsgState = "state";
        private const string MsgMove = "move";
        private const int MaxPlayers = 4;

        private readonly NetMessenger net;
        private readonly bool isHost;
        private readonly string myName;
        private readonly bool splitAllowed;

        // Sala
        private readonly List<string> names = new List<string>();
        private readonly List<ulong> clientIds = new List<ulong>();   // solo la usa el anfitrion
        private int localSeat;

        // Partida
        private GameEngine engine;      // solo anfitrion
        private GameState mirror;       // solo los demas: copia del estado del anfitrion
        private bool started;
        private bool awaiting;          // ya enviaste tu jugada y esperas la respuesta
        private bool closed;

        public event Action LobbyChanged;
        public event Action Started;
        public event Action Changed;
        public event Action Closed;
        public event Action<string> Disconnected;

        public OnlineMatch(NetMessenger net, bool isHost, string playerName, bool splitAllowed)
        {
            this.net = net;
            this.isHost = isHost;
            this.myName = playerName;
            this.splitAllowed = splitAllowed;

            net.ClientDisconnected += OnClientDisconnected;

            if (isHost)
            {
                names.Add(myName);
                clientIds.Add(net.LocalClientId);
                localSeat = 0;
                net.Register(MsgJoin, OnJoin);
                net.Register(MsgMove, OnMove);
            }
            else
            {
                net.Register(MsgLobby, OnLobby);
                net.Register(MsgState, OnState);
                net.WhenConnected(SendJoin);
            }
        }

        // ------------------------------------------------------------
        // Datos de la sala (para la pantalla de espera)
        // ------------------------------------------------------------
        public bool IsHost { get { return isHost; } }
        public IList<string> Names { get { return names; } }
        public int LocalSeat { get { return localSeat; } }

        // ------------------------------------------------------------
        // IMatch
        // ------------------------------------------------------------
        public GameState State { get { return isHost ? (engine != null ? engine.State : null) : mirror; } }
        public int BottomPlayer { get { return localSeat; } }
        public bool Thinking { get { return false; } }
        public bool CanRestart { get { return isHost; } }

        public bool CanLocalAct
        {
            get
            {
                GameState s = State;
                return started && s != null && s.Status == GameStatus.Playing
                    && s.CurrentPlayer == localSeat && !awaiting;
            }
        }

        public void Begin() { }

        public bool TryAttack(int fromHand, int targetPlayer, int targetHand)
        {
            return Play(Move.Attack(fromHand, targetPlayer, targetHand));
        }

        public bool TrySplit(int leftFingers, int rightFingers)
        {
            return Play(Move.Split(leftFingers, rightFingers));
        }

        public List<int[]> GetSplitOptions()
        {
            return GameRules.GetSplitOptions(State.Players[State.CurrentPlayer]);
        }

        public void Restart()
        {
            if (!isHost || engine == null) return;
            engine.Restart();
            BroadcastState();
            Raise();
        }

        public void Close()
        {
            if (closed) return;
            closed = true;
            if (Closed != null) Closed();
        }

        public void Dispose()
        {
            net.ClientDisconnected -= OnClientDisconnected;
            net.Dispose();
        }

        // ------------------------------------------------------------
        // Empezar (solo anfitrion)
        // ------------------------------------------------------------
        public void StartGame()
        {
            if (!isHost || started || names.Count < 2) return;
            engine = new GameEngine(names, splitAllowed);
            started = true;
            BroadcastState();
            if (Started != null) Started();
        }

        // ------------------------------------------------------------
        // Hacer una jugada
        // ------------------------------------------------------------
        private bool Play(Move move)
        {
            if (!CanLocalAct) return false;

            if (isHost)
            {
                MoveResult result;
                if (!engine.TryPlay(move, out result)) return false;
                BroadcastState();
                Raise();
                return true;
            }

            // Los demas no cambian nada: piden la jugada al anfitrion y esperan su respuesta.
            if (!GameRules.IsLegal(mirror, move)) return false;
            awaiting = true;
            net.SendToHost(MsgMove, OnlineSerializer.MoveToJson(move));
            return true;
        }

        // ------------------------------------------------------------
        // Mensajes que recibe el ANFITRION
        // ------------------------------------------------------------
        private void OnJoin(ulong sender, string json)
        {
            if (started || clientIds.Contains(sender) || names.Count >= MaxPlayers) return;

            var dto = JsonUtility.FromJson<JoinDto>(json);
            string playerName = string.IsNullOrEmpty(dto.name) ? "Jugador " + (names.Count + 1) : dto.name;
            names.Add(playerName);
            clientIds.Add(sender);

            BroadcastLobby();
            if (LobbyChanged != null) LobbyChanged();
        }

        private void OnMove(ulong sender, string json)
        {
            if (engine == null) return;

            // Solo vale si la jugada viene del jugador al que le toca
            int seat = clientIds.IndexOf(sender);
            if (seat < 0 || seat != engine.State.CurrentPlayer) return;

            MoveResult result;
            if (engine.TryPlay(OnlineSerializer.MoveFromJson(json), out result))
            {
                BroadcastState();
                Raise();
            }
        }

        private void BroadcastLobby()
        {
            // Cada jugador recibe la lista y su propio puesto (seat)
            for (int i = 1; i < clientIds.Count; i++)
            {
                var dto = new LobbyDto { names = names.ToArray(), seat = i };
                net.Send(MsgLobby, clientIds[i], JsonUtility.ToJson(dto));
            }
        }

        private void BroadcastState()
        {
            net.SendToAllClients(MsgState, OnlineSerializer.StateToJson(engine.State));
        }

        // ------------------------------------------------------------
        // Mensajes que reciben los DEMAS jugadores
        // ------------------------------------------------------------
        private void SendJoin()
        {
            net.SendToHost(MsgJoin, JsonUtility.ToJson(new JoinDto { name = myName }));
        }

        private void OnLobby(ulong sender, string json)
        {
            var dto = JsonUtility.FromJson<LobbyDto>(json);
            names.Clear();
            names.AddRange(dto.names);
            localSeat = dto.seat;
            if (LobbyChanged != null) LobbyChanged();
        }

        private void OnState(ulong sender, string json)
        {
            mirror = OnlineSerializer.StateFromJson(json);
            awaiting = false;

            if (!started)
            {
                started = true;
                if (Started != null) Started();
            }
            Raise();
        }

        // ------------------------------------------------------------
        // Desconexiones
        // ------------------------------------------------------------
        private void OnClientDisconnected(ulong clientId)
        {
            if (closed) return;

            if (!isHost)
            {
                if (clientId == net.LocalClientId && Disconnected != null)
                    Disconnected("Se perdió la conexión con el anfitrión.");
                return;
            }

            int seat = clientIds.IndexOf(clientId);
            if (seat <= 0) return;

            if (!started)
            {
                names.RemoveAt(seat);
                clientIds.RemoveAt(seat);
                BroadcastLobby();
                if (LobbyChanged != null) LobbyChanged();
            }
            else if (Disconnected != null)
            {
                Disconnected("Un jugador se desconectó.");
            }
        }

        private void Raise()
        {
            if (Changed != null) Changed();
        }
    }
}
