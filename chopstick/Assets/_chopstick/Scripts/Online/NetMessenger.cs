using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;

namespace Chopsticks.Online
{
    /// <summary>
    /// Enviar y recibir mensajes de texto entre dispositivos usando Netcode for GameObjects.
    /// Es lo unico que toca Netcode; el resto del juego solo ve "enviar texto" y "recibir texto".
    /// </summary>
    public class NetMessenger : IDisposable
    {
        private readonly List<string> registered = new List<string>();
        private Action whenConnected;

        public event Action<ulong> ClientDisconnected;

        private NetworkManager Manager { get { return NetworkManager.Singleton; } }

        public NetMessenger()
        {
            Manager.OnClientDisconnectCallback += OnDisconnected;
        }

        public ulong LocalClientId { get { return Manager.LocalClientId; } }
        public bool IsConnected { get { return Manager.IsConnectedClient; } }

        /// <summary>Ejecuta la accion cuando este dispositivo ya esta conectado (o ahora mismo si ya lo esta).</summary>
        public void WhenConnected(Action action)
        {
            if (IsConnected) { action(); return; }
            whenConnected = action;
            Manager.OnClientConnectedCallback += OnConnected;
        }

        // ---------------- Recibir ----------------
        public void Register(string messageName, Action<ulong, string> handler)
        {
            registered.Add(messageName);
            Manager.CustomMessagingManager.RegisterNamedMessageHandler(messageName, (senderId, reader) =>
            {
                string json;
                reader.ReadValueSafe(out json);
                handler(senderId, json);
            });
        }

        // ---------------- Enviar ----------------
        public void SendToHost(string messageName, string json)
        {
            Send(messageName, NetworkManager.ServerClientId, json);
        }

        public void Send(string messageName, ulong clientId, string json)
        {
            try
            {
                using (var writer = new FastBufferWriter(FastBufferWriter.GetWriteSize(json), Allocator.Temp))
                {
                    writer.WriteValueSafe(json);
                    Manager.CustomMessagingManager.SendNamedMessage(messageName, clientId, writer, NetworkDelivery.ReliableSequenced);
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogWarning("No se pudo enviar el mensaje: " + e.Message);
            }
        }

        /// <summary>Solo para el anfitrion: envia a todos los demas jugadores.</summary>
        public void SendToAllClients(string messageName, string json)
        {
            foreach (ulong id in Manager.ConnectedClientsIds)
            {
                if (id != Manager.LocalClientId) Send(messageName, id, json);
            }
        }

        // ---------------- Avisos de conexion ----------------
        private void OnConnected(ulong clientId)
        {
            if (clientId != Manager.LocalClientId) return;
            Manager.OnClientConnectedCallback -= OnConnected;
            if (whenConnected != null) whenConnected();
        }

        private void OnDisconnected(ulong clientId)
        {
            if (ClientDisconnected != null) ClientDisconnected(clientId);
        }

        public void Dispose()
        {
            var manager = Manager;
            if (manager == null) return;

            manager.OnClientDisconnectCallback -= OnDisconnected;
            manager.OnClientConnectedCallback -= OnConnected;
            if (manager.CustomMessagingManager != null)
            {
                foreach (string messageName in registered)
                {
                    manager.CustomMessagingManager.UnregisterNamedMessageHandler(messageName);
                }
            }
            registered.Clear();
        }
    }
}
