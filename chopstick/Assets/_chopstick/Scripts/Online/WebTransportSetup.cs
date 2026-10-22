using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace Chopsticks.Online
{
    /// <summary>
    /// Va en el GameObject "Network" (el que tiene el Network Manager y el Unity Transport).
    /// En la version web, el navegador solo puede conectarse con WebSockets, asi que activa
    /// "Use WebSockets" solo en esa compilacion. En Android y en el editor no cambia nada.
    /// </summary>
    [RequireComponent(typeof(UnityTransport))]
    public class WebTransportSetup : MonoBehaviour
    {
        private void Awake()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            GetComponent<UnityTransport>().UseWebSockets = true;
#endif
        }
    }
}
