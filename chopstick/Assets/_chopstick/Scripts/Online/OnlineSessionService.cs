using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;

namespace Chopsticks.Online
{
    /// <summary>
    /// Todo lo de Unity Gaming Services en un solo sitio: iniciar sesion (anonima),
    /// crear una sala con codigo, unirse con un codigo y salir.
    /// No sabe nada del juego ni de los mensajes.
    /// </summary>
    public class OnlineSessionService
    {
        private ISession session;

        /// <summary>Codigo para unirse (solo existe tras crear o unirse).</summary>
        public string Code { get { return session != null ? session.Code : null; } }

        /// <summary>Crea una sala y devuelve el codigo para compartir.</summary>
        public async Task<string> CreateAsync(int maxPlayers)
        {
            await SignInAsync();
            var options = new SessionOptions { MaxPlayers = maxPlayers }.WithRelayNetwork();
            session = await MultiplayerService.Instance.CreateSessionAsync(options);
            return session.Code;
        }

        /// <summary>Se une a una sala con su codigo.</summary>
        public async Task JoinAsync(string code)
        {
            await SignInAsync();
            session = await MultiplayerService.Instance.JoinSessionByCodeAsync(code);
        }

        public async Task LeaveAsync()
        {
            if (session == null) return;
            ISession old = session;
            session = null;
            try { await old.LeaveAsync(); }
            catch { /* si ya estaba cerrada no pasa nada */ }
        }

        // Sesion anonima: no hay cuentas ni registro.
        private static async Task SignInAsync()
        {
            if (UnityServices.State == ServicesInitializationState.Uninitialized)
            {
                await UnityServices.InitializeAsync();
            }
            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }
        }
    }
}
