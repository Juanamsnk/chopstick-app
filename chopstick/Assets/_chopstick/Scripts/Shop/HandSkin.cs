using System.Collections.Generic;
using UnityEngine;

namespace Chopsticks.Shop
{
    public enum SkinRarity { Comun = 0, Rara = 1, Epica = 2, Legendaria = 3 }

    /// <summary>Un estilo de mano de la tienda.</summary>
    public class HandSkin
    {
        public string Id;            // tambien es el principio del nombre de sus PNG
        public string Name;
        public string Description;
        public SkinRarity Rarity;
        public bool UsesSkinTone;    // true: es una mano con piel y cambia con el tono elegido

        public int Price { get { return Id == HandSkinCatalog.DefaultId ? 0 : HandSkinCatalog.PriceOf(Rarity); } }
    }

    /// <summary>Los 6 tonos de piel. Solo los usan los estilos con piel.</summary>
    public static class SkinTones
    {
        public const int Count = 6;
        public static readonly string[] Names = { "Muy claro", "Claro", "Medio", "Moreno", "Oscuro", "Muy oscuro" };
        private static readonly string[] Fills = { "#ffd3a8", "#f6c8a0", "#d9a066", "#b97a4a", "#8d5524", "#5c3a21" };
        private static readonly string[] Lines = { "#e39a62", "#d49a6a", "#a8703a", "#85522b", "#c58a4e", "#9a6a45" };

        public static Color Fill(int tone) { return Parse(Fills[Clamp(tone)]); }
        public static Color Line(int tone) { return Parse(Lines[Clamp(tone)]); }
        public static int Clamp(int tone) { return Mathf.Clamp(tone, 0, Count - 1); }

        private static Color Parse(string hex)
        {
            Color c;
            ColorUtility.TryParseHtmlString(hex, out c);
            return c;
        }
    }

    /// <summary>La lista de estilos y sus precios.</summary>
    public static class HandSkinCatalog
    {
        public const string DefaultId = "clasica";

        // Precio por rareza: cuanto mas rara, mas cara.
        public static int PriceOf(SkinRarity rarity)
        {
            switch (rarity)
            {
                case SkinRarity.Comun: return 150;
                case SkinRarity.Rara: return 400;
                case SkinRarity.Epica: return 900;
                default: return 2000;
            }
        }

        public static string RarityName(SkinRarity rarity)
        {
            switch (rarity)
            {
                case SkinRarity.Comun: return "Común";
                case SkinRarity.Rara: return "Rara";
                case SkinRarity.Epica: return "Épica";
                default: return "Legendaria";
            }
        }

        public static readonly IReadOnlyList<HandSkin> All = new List<HandSkin>
        {
            // Comunes
            Make("clasica", "Clásica", "La de siempre", SkinRarity.Comun, true),
            Make("manicura", "Manicura", "Uñas de colores", SkinRarity.Comun, true),
            Make("henna", "Henna", "Dibujos de boda", SkinRarity.Comun, true),
            Make("tatuajes", "Tatuajes", "Tinta tribal", SkinRarity.Comun, true),
            Make("golf", "Guante de golf", "Cuero blanco y velcro", SkinRarity.Comun, false),
            // Raras
            Make("anillos", "Anillos de oro", "Oro y gemas", SkinRarity.Rara, true),
            Make("zombi", "Zombi", "Puntos y hueso", SkinRarity.Rara, false),
            Make("esqueleto", "Esqueleto", "Hasta los huesos", SkinRarity.Rara, false),
            Make("pixel", "Pixel", "Estilo 8 bits", SkinRarity.Rara, true),
            Make("golem", "Golem", "Piedra y musgo", SkinRarity.Rara, false),
            // Epicas
            Make("vampiro", "Vampiro", "Uñas afiladas", SkinRarity.Epica, false),
            Make("robot", "Robot", "Metal con LED", SkinRarity.Epica, false),
            Make("hielo", "Hielo", "Cristales helados", SkinRarity.Epica, false),
            Make("armadura", "Armadura", "Guantelete de caballero", SkinRarity.Epica, false),
            Make("lentejuelas", "Lentejuelas", "Un solo guante brillante", SkinRarity.Epica, false),
            Make("princesa", "Princesa", "Satén, lazo y perlas", SkinRarity.Epica, false),
            Make("rayo", "Guante de rayo", "Héroe eléctrico", SkinRarity.Epica, false),
            // Legendarias
            Make("neon", "Neón", "Brilla en la noche", SkinRarity.Legendaria, false),
            Make("oro", "Oro macizo", "Dorado brillante", SkinRarity.Legendaria, false),
            Make("fantasma", "Fantasma", "Casi invisible", SkinRarity.Legendaria, false),
            Make("cosmico", "Puño cósmico", "Energía de las estrellas", SkinRarity.Legendaria, false),
            Make("superheroe", "Super Héroe", "Tu amable vecino", SkinRarity.Legendaria, false)
        };

        public static HandSkin Find(string id)
        {
            for (int i = 0; i < All.Count; i++)
            {
                if (All[i].Id == id) return All[i];
            }
            return null;
        }

        /// <summary>Lo que cuestan todos los estilos juntos.</summary>
        public static int TotalPrice
        {
            get
            {
                int total = 0;
                for (int i = 0; i < All.Count; i++) total += All[i].Price;
                return total;
            }
        }

        private static HandSkin Make(string id, string name, string description, SkinRarity rarity, bool tone)
        {
            return new HandSkin { Id = id, Name = name, Description = description, Rarity = rarity, UsesSkinTone = tone };
        }
    }
}
