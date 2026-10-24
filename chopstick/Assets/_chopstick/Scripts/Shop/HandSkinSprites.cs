using System.Collections.Generic;
using UnityEngine;

namespace Chopsticks.Shop
{
    /// <summary>
    /// Carga los PNG de las manos desde Resources/HandSkins.
    /// Nombres: "zombi_3.png" (3 dedos levantados) y, en los estilos con piel, "clasica_t2_3.png" (tono 2).
    /// </summary>
    public static class HandSkinSprites
    {
        private static readonly Dictionary<string, Texture2D> Cache = new Dictionary<string, Texture2D>();
        private static readonly HashSet<string> Reported = new HashSet<string>();

        /// <summary>Devuelve null si falta el PNG (la mano se dibuja entonces con el estilo basico).</summary>
        public static Texture2D Get(string skinId, int tone, int fingers)
        {
            HandSkin skin = HandSkinCatalog.Find(skinId) ?? HandSkinCatalog.Find(HandSkinCatalog.DefaultId);
            fingers = Mathf.Clamp(fingers, 0, 4);

            string file = skin.UsesSkinTone
                ? skin.Id + "_t" + SkinTones.Clamp(tone) + "_" + fingers
                : skin.Id + "_" + fingers;

            Texture2D texture;
            if (Cache.TryGetValue(file, out texture) && texture != null) return texture;

            texture = Resources.Load<Texture2D>("HandSkins/" + file);
            if (texture != null)
            {
                Cache[file] = texture;   // los que faltan NO se guardan, asi se reintentan
            }
            else if (Reported.Add(file))
            {
                Debug.LogWarning("[Manos] Falta el PNG: Resources/HandSkins/" + file + ".png");
            }
            return texture;
        }
    }
}
