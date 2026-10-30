using System.Collections.Generic;
using Chopsticks.Career;
using UnityEngine;

namespace Chopsticks.Shop
{
    public enum SkinRarity { Comun = 0, Rara = 1, Epica = 2, Legendaria = 3 }

    /// <summary>De donde sale una mano: se compra en la tienda o se gana venciendo a un rango de la carrera.</summary>
    public enum HandSkinSource { Shop, Career }

    /// <summary>Un estilo de mano de la tienda.</summary>
    public class HandSkin
    {
        public string Id;            // tambien es el principio del nombre de sus PNG
        public string Name;
        public string Description;
        public SkinRarity Rarity;
        public bool UsesSkinTone;    // true: es una mano con piel y cambia con el tono elegido

        public int Price { get { return Id == HandSkinCatalog.DefaultId ? 0 : HandSkinCatalog.PriceOf(Rarity); } }

        /// <summary>
        /// Nivel del Modo carrera que hay que superar para GANAR esta mano (0 = no es de carrera).
        /// Salen de CareerOpponentSkins: la mano que lleva un rango se gana al superar su ultimo nivel.
        /// </summary>
        public int CareerRewardLevel { get { return CareerOpponentSkins.RewardLevelFor(Id); } }

        public HandSkinSource Source { get { return CareerRewardLevel > 0 ? HandSkinSource.Career : HandSkinSource.Shop; } }

        /// <summary>Niveles del Modo carrera que hay que haber superado para poder comprarlo (0 = desde el principio).</summary>
        public int UnlockLevel { get { return HandSkinCatalog.UnlockLevelOf(Rarity); } }
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

        // Nivel de la carrera que hay que superar para poder comprar cada rareza. Coinciden con el final de un rango:
        // raras al acabar Novato (10), epicas al acabar Competidor (30) y legendarias al acabar Maestro (60).
        // Asi no se puede ahorrar para comprar un legendario nada mas empezar.
        public static int UnlockLevelOf(SkinRarity rarity)
        {
            switch (rarity)
            {
                case SkinRarity.Comun: return 0;
                case SkinRarity.Rara: return 10;
                case SkinRarity.Epica: return 30;
                default: return 60;
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
            Make("queso", "Queso", "Lleno de agujeros", SkinRarity.Comun, false),
            Make("sandia", "Sandía", "Fresca y con pepitas", SkinRarity.Comun, false),
            Make("chocolate", "Chocolate", "Onzas para morder", SkinRarity.Comun, false),
            Make("tulipanes", "Tulipanes", "Un jardín en cada dedo", SkinRarity.Comun, false),
            // Raras
            Make("anillos", "Anillos de oro", "Oro y gemas", SkinRarity.Rara, true),
            Make("zombi", "Zombi", "Puntos y hueso", SkinRarity.Rara, false),
            Make("esqueleto", "Esqueleto", "Hasta los huesos", SkinRarity.Rara, false),
            Make("pixel", "Pixel", "Estilo 8 bits", SkinRarity.Rara, true),
            Make("golem", "Golem", "Piedra y musgo", SkinRarity.Rara, false),
            Make("gato", "Gato", "Pelo naranja y patita", SkinRarity.Rara, false),
            Make("cactus", "Cactus", "Verde y espinoso", SkinRarity.Rara, false),
            Make("futbol", "Balón de fútbol", "Pentágonos de gol", SkinRarity.Rara, false),
            Make("baloncesto", "Baloncesto", "Bote y canasta", SkinRarity.Rara, false),
            Make("boxeo", "Guante de boxeo", "Golpe de campeón", SkinRarity.Rara, false),
            Make("girasol", "Girasol", "Siempre mirando al sol", SkinRarity.Rara, false),
            // Epicas
            Make("vampiro", "Vampiro", "Uñas afiladas", SkinRarity.Epica, false),
            Make("robot", "Robot", "Metal con LED", SkinRarity.Epica, false),
            Make("hielo", "Hielo", "Cristales helados", SkinRarity.Epica, false),
            Make("armadura", "Armadura", "Guantelete de caballero", SkinRarity.Epica, false),
            Make("lentejuelas", "Lentejuelas", "Un solo guante brillante", SkinRarity.Epica, false),
            Make("princesa", "Princesa", "Satén, lazo y perlas", SkinRarity.Epica, false),
            Make("rayo", "Guante de rayo", "Héroe eléctrico", SkinRarity.Epica, false),
            Make("dragon", "Dragón", "Escamas y garras", SkinRarity.Epica, false),
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
