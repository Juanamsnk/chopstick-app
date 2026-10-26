using System;
using System.Collections.Generic;
using Chopsticks.Career;
using Chopsticks.Shop;
using UnityEngine;

namespace Chopsticks.Social
{
    /// <summary>
    /// Todo lo que se guarda en la cuenta: niveles superados, monedas, estilos comprados, estilo puesto y tono.
    /// Es la misma informacion que CareerProgress y ShopInventory guardan en el dispositivo.
    /// </summary>
    public class SaveData
    {
        public int Completed;
        public int Coins;
        public string Owned = "";
        public string Equipped = "";
        public int Tone;
        public long Updated;   // cuando se cambio por ultima vez (ms desde 1970); gana el mas reciente

        /// <summary>Hay algo que merezca la pena conservar (si no, es un invitado recien instalado).</summary>
        public bool HasProgress
        {
            get
            {
                if (Completed > 0 || Coins > 0) return true;
                foreach (string id in (Owned ?? "").Split(','))
                {
                    if (id.Length > 0 && id != HandSkinCatalog.DefaultId) return true;
                }
                return false;
            }
        }

        /// <summary>Lee un hueco de PlayerPrefs ("" = invitado) sin tocar lo que hay cargado en pantalla.</summary>
        public static SaveData ReadSlot(string prefix)
        {
            return new SaveData
            {
                Completed = PlayerPrefs.GetInt(prefix + CareerProgress.Key, 0),
                Coins = PlayerPrefs.GetInt(prefix + ShopInventory.CoinsKey, 0),
                Owned = PlayerPrefs.GetString(prefix + ShopInventory.OwnedKey, ""),
                Equipped = PlayerPrefs.GetString(prefix + ShopInventory.EquippedKey, HandSkinCatalog.DefaultId),
                Tone = PlayerPrefs.GetInt(prefix + ShopInventory.ToneKey, 0)
            };
        }

        public static void ClearSlot(string prefix)
        {
            PlayerPrefs.DeleteKey(prefix + CareerProgress.Key);
            PlayerPrefs.DeleteKey(prefix + ShopInventory.CoinsKey);
            PlayerPrefs.DeleteKey(prefix + ShopInventory.OwnedKey);
            PlayerPrefs.DeleteKey(prefix + ShopInventory.EquippedKey);
            PlayerPrefs.DeleteKey(prefix + ShopInventory.ToneKey);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Une el progreso de invitado con el de la cuenta: el nivel mas alto, los estilos de los dos,
        /// las monedas sumadas y el estilo y tono de la cuenta. Despues el hueco de invitado se vacia,
        /// asi las monedas no se pueden sumar dos veces.
        /// </summary>
        public static SaveData Merge(SaveData account, SaveData guest)
        {
            var owned = new HashSet<string>();
            foreach (string id in (account.Owned ?? "").Split(',')) if (id.Length > 0) owned.Add(id);
            foreach (string id in (guest.Owned ?? "").Split(',')) if (id.Length > 0) owned.Add(id);

            return new SaveData
            {
                Completed = Math.Max(account.Completed, guest.Completed),
                Coins = account.Coins + guest.Coins,
                Owned = string.Join(",", owned),
                Equipped = account.Equipped,
                Tone = account.Tone
            };
        }
    }
}
