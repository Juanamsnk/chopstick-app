using System;
using System.Collections.Generic;
using UnityEngine;

namespace Chopsticks.Shop
{
    /// <summary>
    /// Monedas, estilos comprados, estilo puesto y tono de piel. Todo se guarda en el dispositivo (PlayerPrefs).
    /// </summary>
    public class ShopInventory
    {
        private const string CoinsKey = "chopsticks.shop.coins";
        private const string OwnedKey = "chopsticks.shop.owned";
        private const string EquippedKey = "chopsticks.shop.equipped";
        private const string ToneKey = "chopsticks.shop.tone";

        private readonly HashSet<string> owned = new HashSet<string>();

        public int Coins { get; private set; }
        public string EquippedId { get; private set; }
        public int Tone { get; private set; }

        /// <summary>Cambio algo (monedas, compra, estilo o tono).</summary>
        public event Action Changed;

        public ShopInventory()
        {
            Coins = Math.Max(0, PlayerPrefs.GetInt(CoinsKey, 0));
            Tone = SkinTones.Clamp(PlayerPrefs.GetInt(ToneKey, 0));

            string saved = PlayerPrefs.GetString(OwnedKey, "");
            foreach (string id in saved.Split(','))
            {
                if (HandSkinCatalog.Find(id) != null) owned.Add(id);
            }
            owned.Add(HandSkinCatalog.DefaultId);

            EquippedId = PlayerPrefs.GetString(EquippedKey, HandSkinCatalog.DefaultId);
            if (!owned.Contains(EquippedId)) EquippedId = HandSkinCatalog.DefaultId;
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

        private void Save()
        {
            PlayerPrefs.SetInt(CoinsKey, Coins);
            PlayerPrefs.SetString(OwnedKey, string.Join(",", owned));
            PlayerPrefs.SetString(EquippedKey, EquippedId);
            PlayerPrefs.SetInt(ToneKey, Tone);
            PlayerPrefs.Save();
            if (Changed != null) Changed();
        }
    }
}
