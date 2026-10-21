using System;
using System.Globalization;
using UnityEngine;

namespace Chopsticks.Online
{
    /// <summary>
    /// Etiqueta que se pone a las salas de busqueda rapida: pais del dispositivo + version de la app.
    /// Asi la busqueda prefiere rivales de tu mismo pais (mas cerca, mejor conexion)
    /// y solo empareja versiones de la app que se entienden entre si.
    /// </summary>
    public static class RegionTag
    {
        public static string Get()
        {
            string region = "XX";
            try
            {
                region = RegionInfo.CurrentRegion.TwoLetterISORegionName;
            }
            catch (Exception)
            {
                // Si el dispositivo no da pais, se usa XX (todos los que no lo dan se emparejan entre si)
            }
            return region + "-" + Application.version;
        }
    }
}
