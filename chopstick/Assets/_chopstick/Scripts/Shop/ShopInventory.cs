using System;
using System.Collections.Generic;
using UnityEngine;

namespace Chopsticks.Shop
{
    /// <summary>
    /// Monedas, estilos comprados, estilo puesto y tono de piel. Todo se guarda en el dispositivo (PlayerPrefs).
    /// Invitado y cuenta usan "huecos" distintos (prefijo de la clave); ProgressSync cambia de hueco
    /// al iniciar o cerrar sesion y sube los datos a Firebase.
    /// </summary>
    public class ShopInventory
    {
        public const string CoinsKey = "chopsticks.shop.coins";
        public const string OwnedKey = "chopsticks.shop.owned";
        public const string EquippedKey = "chopsticks.shop.equipped";
        public const string ToneKey = "chopsticks.shop.tone";

        private readonly HashSet<string> owned = new HashSet<string>();
        private string prefix = "";

        public int Coins { get; private set; }
        public string EquippedId { get; private set; }
        public int Tone { get; private set; }

        /// <summary>Los estilos comprados, separados por comas (para guardarlos).</summary>
        public string OwnedCsv { get { return string.Join(",", owned); } }

        /// <summary>Cambio algo (monedas, compra, estilo o tono).</summary>
        public event Action Changed;

        public ShopInventory()
        {
            Load();
        }

        public bool Owns(string id) { return owned.Contains(id); }

        public HandSkin Equipped { get { return HandSkinCatalog.Find(EquippedId); } }

        public void AddCoins(int amount)
        {
            if (amount <= 0) return;
            Coins += amount;
            Save();
        }

        public bool CanBuy(HandSkin skin)
        {
            return skin != null && !Owns(skin.Id) && Coins >= skin.Price;
        }

        /// <summary>Compra un estilo y lo pone al momento.</summary>
        public bool TryBuy(HandSkin skin)
        {
            if (!CanBuy(skin)) return false;
            Coins -= skin.Price;
            owned.Add(skin.Id);
            EquippedId = skin.Id;
            Save();
            return true;
        }

        public bool Equip(string id)
        {
            if (!Owns(id)) return false;
            EquippedId = id;
            Save();
            return true;
        }

        public void SetTone(int tone)
        {
            Tone = SkinTones.Clamp(tone);
            Save();
        }

        /// <summary>Sustituye todo por los datos de la cuenta (lo usa la sincronizacion).</summary>
        public void Import(int coins, string ownedCsv, string equippedId, int tone)
        {
            Coins = Math.Max(0, coins);
            Tone = SkinTones.Clamp(tone);
            ParseOwned(ownedCsv);
            EquippedId = !string.IsNullOrEmpty(equippedId) && owned.Contains(equippedId) ? equippedId : HandSkinCatalog.DefaultId;
            Save();
        }

        /// <summary>Cambia de hueco de guardado ("" = invitado) y carga lo que haya en el.</summary>
        public void UseSlot(string slotPrefix)
        {
            prefix = slotPrefix ?? "";
            Load();
            if (Changed != null) Changed();
        }

        private void Load()
        {
            Coins = Math.Max(0, PlayerPrefs.GetInt(prefix + CoinsKey, 0));
            Tone = SkinTones.Clamp(PlayerPrefs.GetInt(prefix + ToneKey, 0));
            ParseOwned(PlayerPrefs.GetString(prefix + OwnedKey, ""));

            EquippedId = PlayerPrefs.GetString(prefix + EquippedKey, HandSkinCatalog.DefaultId);
            if (!owned.Contains(EquippedId)) EquippedId = HandSkinCatalog.DefaultId;
        }

        private void ParseOwned(string csv)
        {
            owned.Clear();
            foreach (string id in (csv ?? "").Split(','))
            {
                if (HandSkinCatalog.Find(id) != null) owned.Add(id);
            }
            owned.Add(HandSkinCatalog.DefaultId);
        }

        private void Save()
        {
            PlayerPrefs.SetInt(prefix + CoinsKey, Coins);
            PlayerPrefs.SetString(prefix + OwnedKey, string.Join(",", owned));
            PlayerPrefs.SetString(prefix + EquippedKey, EquippedId);
            PlayerPrefs.SetInt(prefix + ToneKey, Tone);
            PlayerPrefs.Save();
            if (Changed != null) Changed();
        }
    }
}
