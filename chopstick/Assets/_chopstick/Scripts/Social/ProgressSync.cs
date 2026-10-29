using System;
using System.Threading.Tasks;
using Chopsticks.Career;
using Chopsticks.Shop;
using UnityEngine;

namespace Chopsticks.Social
{
    /// <summary>
    /// Guarda el progreso (niveles, monedas, compras) segun quien juegue:
    ///  - Invitado: en el dispositivo (PlayerPrefs), como siempre.
    ///  - Con cuenta: en Firebase (saves/{uid}), y una copia en el dispositivo para jugar sin conexion.
    ///
    /// Al iniciar sesion:
    ///  - Si la cuenta ya tenia datos (por ejemplo, de otro movil), se usan esos.
    ///  - Si venias de invitado con progreso, se une al de la cuenta y el hueco de invitado se vacia.
    ///  - Si la cuenta era nueva, se queda con tu progreso de invitado.
    /// Mientras la sesion esta abierta, cada cambio se sube y los cambios hechos desde otro movil se reciben solos.
    /// Al cerrar sesion vuelves al hueco de invitado (limpio).
    /// </summary>
    public class ProgressSync
    {
        private const string GuestSlot = "";
        private const string AccountSlot = "acc.";
        private const string CacheUidKey = "acc.uid";
        private const string CacheUpdatedKey = "acc.updated";
        private const int PushDelayMs = 1500;
        private const int RetryDelayMs = 5000;
        private const int Attempts = 3;

        private readonly SocialBackend backend;
        private readonly CareerProgress career;
        private readonly ShopInventory shop;

        private string activeUid;       // cuenta cuyos datos se estan usando (null = invitado)
        private bool signingIn;
        private bool applying;          // se estan cargando datos: no hay que volver a subirlos
        private bool dirty;             // hay cambios sin subir
        private bool pushWaiting;
        private long localUpdated;

        /// <summary>Se esta cargando el progreso de la cuenta (al abrir la app con sesion iniciada).</summary>
        public bool IsSigningIn { get { return signingIn; } }

        /// <summary>Termino de cargar el progreso de la cuenta (bien o mal).</summary>
        public event Action SignInFinished;

        public ProgressSync(SocialBackend backend, CareerProgress career, ShopInventory shop)
        {
            this.backend = backend;
            this.career = career;
            this.shop = shop;

            career.Changed += OnLocalChanged;
            shop.Changed += OnLocalChanged;
            backend.SignInChanged += OnAccountChanged;
            backend.SaveChanged += OnRemoteSave;
        }

        // ------------------------------------------------------------
        // Sesion
        // ------------------------------------------------------------
        private void OnAccountChanged()
        {
            if (backend.IsSignedIn)
            {
                if (backend.Uid != activeUid && !signingIn) SignInAsync();
            }
            else if (activeUid != null)
            {
                SignedOut();
            }
        }

        private async void SignInAsync()
        {
            string uid = backend.Uid;
            signingIn = true;
            for (int attempt = 0; attempt < Attempts && backend.Uid == uid; attempt++)
            {
                try
                {
                    SaveData cloud = await backend.LoadSaveAsync();
                    if (backend.Uid != uid) break;   // cambio la sesion mientras se esperaba
                    Adopt(uid, cloud);
                    backend.ListenSave();
                    break;
                }
                catch (Exception e)
                {
                    Debug.LogWarning("[Sync] No se pudo leer la cuenta (intento " + (attempt + 1) + "): " + e.Message);
                    if (attempt < Attempts - 1) await Task.Delay(RetryDelayMs);
                }
            }
            signingIn = false;
            if (SignInFinished != null) SignInFinished();
        }

        /// <summary>Decide con que datos se queda esta cuenta en este dispositivo y los carga.</summary>
        private void Adopt(string uid, SaveData cloud)
        {
            SaveData guest = SaveData.ReadSlot(GuestSlot);
            bool guestHasProgress = guest.HasProgress;

            // Copia de esta misma cuenta en el dispositivo (por si hubo cambios sin conexion)
            SaveData cache = null;
            if (PlayerPrefs.GetString(CacheUidKey, "") == uid && PlayerPrefs.HasKey(AccountSlot + ShopInventory.CoinsKey))
            {
                cache = SaveData.ReadSlot(AccountSlot);
                cache.Updated = ReadCachedUpdated();
            }

            SaveData basis = cloud;
            bool push = false;
            if (cache != null && (cloud == null || cache.Updated > cloud.Updated))
            {
                basis = cache;
                push = true;
            }

            SaveData result;
            if (basis == null)
            {
                result = guest;
                result.Updated = guestHasProgress ? Now() : 0;
                push = guestHasProgress;
            }
            else if (guestHasProgress)
            {
                result = SaveData.Merge(basis, guest);
                result.Updated = Now();
                push = true;
            }
            else
            {
                result = basis;
                if (push) result.Updated = Now();
            }

            applying = true;
            career.UseSlot(AccountSlot);
            career.SetCompleted(result.Completed);
            shop.UseSlot(AccountSlot);
            shop.Import(result.Coins, result.Owned, result.Equipped, result.Tone);
            applying = false;

            activeUid = uid;
            SetLocalUpdated(result.Updated);
            PlayerPrefs.SetString(CacheUidKey, uid);
            PlayerPrefs.Save();

            // Ya esta en la cuenta (y en su copia local): el hueco de invitado se vacia para no sumarlo dos veces
            if (guestHasProgress) SaveData.ClearSlot(GuestSlot);

            if (push)
            {
                dirty = true;
                QueuePush();
            }
        }

        private void SignedOut()
        {
            activeUid = null;
            dirty = false;
            applying = true;
            career.UseSlot(GuestSlot);
            shop.UseSlot(GuestSlot);
            applying = false;
        }

        // ------------------------------------------------------------
        // Cambios propios (subir)
        // ------------------------------------------------------------
        private void OnLocalChanged()
        {
            if (applying || activeUid == null) return;
            SetLocalUpdated(Now());
            dirty = true;
            QueuePush();
        }

        /// <summary>Espera un momento (por si hay varios cambios seguidos, como ganar y cobrar) y sube todo junto.</summary>
        private async void QueuePush()
        {
            if (pushWaiting) return;
            pushWaiting = true;
            await Task.Delay(PushDelayMs);
            pushWaiting = false;
            await PushNowAsync();
        }

        private async Task PushNowAsync()
        {
            if (activeUid == null || !dirty || backend.Uid != activeUid) return;
            dirty = false;
            try
            {
                await backend.WriteSaveAsync(Snapshot());
            }
            catch (Exception e)
            {
                dirty = true;   // se reintenta con el siguiente cambio; la copia local no se pierde
                Debug.LogWarning("[Sync] No se pudo subir el progreso: " + e.Message);
            }
        }

        private SaveData Snapshot()
        {
            return new SaveData
            {
                Completed = career.Completed,
                Coins = shop.Coins,
                Owned = shop.OwnedCsv,
                Equipped = shop.EquippedId,
                Tone = shop.Tone,
                Updated = localUpdated
            };
        }

        // ------------------------------------------------------------
        // Cambios de otro movil (recibir)
        // ------------------------------------------------------------
        private void OnRemoteSave(SaveData remote)
        {
            if (activeUid == null || signingIn || remote == null) return;
            if (remote.Updated <= localUpdated) return;   // es el eco de lo que acabamos de subir, o algo mas viejo

            applying = true;
            career.SetCompleted(remote.Completed);
            shop.Import(remote.Coins, remote.Owned, remote.Equipped, remote.Tone);
            applying = false;
            SetLocalUpdated(remote.Updated);
        }

        // ------------------------------------------------------------
        // Ayudas
        // ------------------------------------------------------------
        private void SetLocalUpdated(long value)
        {
            localUpdated = value;
            PlayerPrefs.SetString(CacheUpdatedKey, value.ToString());
            PlayerPrefs.Save();
        }

        private static long ReadCachedUpdated()
        {
            long value;
            return long.TryParse(PlayerPrefs.GetString(CacheUpdatedKey, "0"), out value) ? value : 0;
        }

        private static long Now()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }
    }
}
