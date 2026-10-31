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
        public int UnlockLevelOverride = 0;    // nivel de la carrera que hay que superar para poder comprarla (0 = sin nivel)

        public int Price { get { return Id == HandSkinCatalog.DefaultId ? 0 : HandSkinCatalog.PriceOf(Rarity); } }

        /// <summary>
        /// Nivel del Modo carrera que hay que superar para GANAR esta mano (0 = no es de carrera).
        /// Salen de CareerOpponentSkins: la mano que lleva un rango se gana al superar su ultimo nivel.
        /// </summary>
        public int CareerRewardLevel { get { return CareerOpponentSkins.RewardLevelFor(Id); } }

        public HandSkinSource Source { get { return CareerRewardLevel > 0 ? HandSkinSource.Career : HandSkinSource.Shop; } }

        /// <summary>Niveles del Modo carrera que hay que haber superado para poder comprarlo (0 = desde el principio).</summary>
        public int UnlockLevel { get { return UnlockLevelOverride; } }
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

        // TIENDA: 23 manos, una por escalon de nivel. 3 sin nivel y despues de 5 en 5 hasta el 100 (5, 10, 15 ... 100).
        // El nivel es el de los niveles SUPERADOS del Modo carrera. Van de menos a mas rara a medida que subes el nivel.
        // INVENTARIO (carrera): 10 manos que se ganan al superar el ultimo nivel de cada rango (10, 20, 30 ... 100);
        // estan marcadas con "Rival de ..." y su nivel sale de CareerOpponentSkins, no de aqui.
        public static readonly IReadOnlyList<HandSkin> All = new List<HandSkin>
        {
            // Por defecto (gratis, ya es tuya)
            Make("clasica", "Clásica", "La de siempre", SkinRarity.Comun, true),

            // Tienda - sin nivel (3)
            Make("manicura", "Manicura", "Uñas de colores", SkinRarity.Comun, true, 0),
            Make("henna", "Henna", "Dibujos de boda", SkinRarity.Comun, true, 0),
            Make("queso", "Queso", "Lleno de agujeros", SkinRarity.Comun, false, 0),
            // Tienda - comunes
            Make("sandia", "Sandía", "Fresca y con pepitas", SkinRarity.Comun, false, 5),
            Make("chocolate", "Chocolate", "Onzas para morder", SkinRarity.Comun, false, 10),
            Make("tulipanes", "Tulipanes", "Un jardín en cada dedo", SkinRarity.Comun, false, 15),
            // Tienda - raras
            Make("anillos", "Anillos de oro", "Oro y gemas", SkinRarity.Rara, true, 20),
            Make("gato", "Gato", "Pelo naranja y patita", SkinRarity.Rara, false, 25),
            Make("esqueleto", "Esqueleto", "Hasta los huesos", SkinRarity.Rara, false, 30),
            Make("cactus", "Cactus", "Verde y espinoso", SkinRarity.Rara, false, 35),
            Make("pixel", "Pixel", "Estilo 8 bits", SkinRarity.Rara, true, 40),
            Make("futbol", "Balón de fútbol", "Pentágonos de gol", SkinRarity.Rara, false, 45),
            Make("baloncesto", "Baloncesto", "Bote y canasta", SkinRarity.Rara, false, 50),
            Make("boxeo", "Guante de boxeo", "Golpe de campeón", SkinRarity.Rara, false, 55),
            Make("girasol", "Girasol", "Siempre mirando al sol", SkinRarity.Rara, false, 60),
            // Tienda - epicas
            Make("vampiro", "Vampiro", "Uñas afiladas", SkinRarity.Epica, false, 65),
            Make("robot", "Robot", "Metal con LED", SkinRarity.Epica, false, 70),
            Make("lentejuelas", "Lentejuelas", "Un solo guante brillante", SkinRarity.Epica, false, 75),
            Make("princesa", "Princesa", "Satén, lazo y perlas", SkinRarity.Epica, false, 80),
            Make("rayo", "Guante de rayo", "Héroe eléctrico", SkinRarity.Epica, false, 85),
            Make("dragon", "Dragón", "Escamas y garras", SkinRarity.Epica, false, 90),
            // Tienda - legendarias
            Make("fantasma", "Fantasma", "Casi invisible", SkinRarity.Legendaria, false, 95),
            Make("cosmico", "Puño cósmico", "Energía de las estrellas", SkinRarity.Legendaria, false, 100),

            // Inventario - se ganan en el Modo carrera (nivel 10, 20 ... 100; ver CareerOpponentSkins)
            Make("calabaza", "Calabaza", "Truco o trato", SkinRarity.Comun, false),          // Novato     -> nivel 10
            Make("golf", "Guante de golf", "Cuero blanco y velcro", SkinRarity.Comun, false), // Aprendiz   -> nivel 20
            Make("tatuajes", "Tatuajes", "Tinta tribal", SkinRarity.Comun, true),             // Competidor -> nivel 30
            Make("zombi", "Zombi", "Puntos y hueso", SkinRarity.Rara, false),                 // Veterano   -> nivel 40
            Make("golem", "Golem", "Piedra y musgo", SkinRarity.Rara, false),                 // Experto    -> nivel 50
            Make("armadura", "Armadura", "Guantelete de caballero", SkinRarity.Epica, false), // Maestro    -> nivel 60
            Make("hielo", "Hielo", "Cristales helados", SkinRarity.Epica, false),             // Campeon    -> nivel 70
            Make("oro", "Oro macizo", "Dorado brillante", SkinRarity.Legendaria, false),      // Leyenda    -> nivel 80
            Make("neon", "Neón", "Brilla en la noche", SkinRarity.Legendaria, false),         // Gran maestro -> nivel 90
            Make("superheroe", "Super Héroe", "Tu amable vecino", SkinRarity.Legendaria, false) // Rey 5ticks -> nivel 100
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

        private static HandSkin Make(string id, string name, string description, SkinRarity rarity, bool tone, int unlockLevel = 0)
        {
            return new HandSkin { Id = id, Name = name, Description = description, Rarity = rarity, UsesSkinTone = tone, UnlockLevelOverride = unlockLevel };
        }
    }
}
