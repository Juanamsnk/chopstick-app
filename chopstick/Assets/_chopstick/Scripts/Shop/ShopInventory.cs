using System;
using System.Collections.Generic;
using Chopsticks.Career;
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
        private CareerProgress career;

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

        /// <summary>Conecta el progreso de la carrera: algunos estilos solo se pueden comprar al avanzar en ella.</summary>
        public void BindCareer(CareerProgress progress)
        {
            career = progress;
            if (AddCareerRewards().Count > 0) Save();   // manos de carrera que ya te habias ganado antes de existir el inventario
        }

        /// <summary>Niveles superados de verdad (0 si no hay carrera conectada). No confundir con CareerLevel.</summary>
        public int CompletedLevels { get { return career != null ? career.Completed : 0; } }

        /// <summary>Niveles de la carrera superados (si no hay carrera conectada, no se bloquea nada).</summary>
        public int CareerLevel { get { return career != null ? career.Completed : int.MaxValue; } }

        /// <summary>Tu rango segun el Modo carrera (Novato, Aprendiz...). null si no hay carrera conectada.</summary>
        public string RankName { get { return career != null ? CareerLevels.RankNameFor(career.Completed) : null; } }

        /// <summary>Tu rango para enviarlo por la red: posicion + 1 (0 = no se sabe).</summary>
        public int OnlineRank { get { return career != null ? CareerLevels.RankIndexFor(career.Completed) + 1 : 0; } }

        /// <summary>true si ya has avanzado lo suficiente en la carrera para poder comprar este estilo.</summary>
        public bool MeetsLevel(HandSkin skin) { return skin != null && CareerLevel >= skin.UnlockLevel; }

        public HandSkin Equipped { get { return HandSkinCatalog.Find(EquippedId); } }

        public void AddCoins(int amount)
        {
            if (amount <= 0) return;
            Coins += amount;
            Save();
        }

        /// <summary>Solo se compran las manos de la tienda; las de carrera se ganan venciendo a un rango.</summary>
        public bool CanBuy(HandSkin skin)
        {
            return skin != null && skin.Source == HandSkinSource.Shop && !Owns(skin.Id) && MeetsLevel(skin) && Coins >= skin.Price;
        }

        /// <summary>Compra un estilo y lo guarda en el inventario. NO lo equipa: eso se hace desde el inventario.</summary>
        public bool TryBuy(HandSkin skin)
        {
            if (!CanBuy(skin)) return false;
            Coins -= skin.Price;
            owned.Add(skin.Id);
            Save();
            return true;
        }

        /// <summary>
        /// Mete en el inventario las manos de carrera que ya te has ganado segun los niveles superados.
        /// Devuelve solo las nuevas (para avisar: "¡Mano nueva!"). Es seguro llamarlo las veces que haga falta.
        /// </summary>
        public List<HandSkin> GrantCareerRewards()
        {
            List<HandSkin> added = AddCareerRewards();
            if (added.Count > 0) Save();
            return added;
        }

        /// <summary>Lo mismo pero sin guardar (para usarlo mientras se carga).</summary>
        private List<HandSkin> AddCareerRewards()
        {
            var added = new List<HandSkin>();
            int done = CompletedLevels;
            foreach (HandSkin skin in HandSkinCatalog.All)
            {
                int level = skin.CareerRewardLevel;
                if (level > 0 && done >= level && owned.Add(skin.Id)) added.Add(skin);
            }
            return added;
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
            AddCareerRewards();
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
            AddCareerRewards();

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
