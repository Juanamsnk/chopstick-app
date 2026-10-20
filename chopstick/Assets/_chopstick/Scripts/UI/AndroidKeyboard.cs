using UnityEngine;

namespace Chopsticks.UI
{
    /// <summary>
    /// Cierra el teclado de Android llamando directamente a Android.
    /// Prueba dos caminos y escribe en el log cual funciono (filtra por "AndroidKeyboard"):
    ///   1. pedirle a Unity que cierre su teclado (hideSoftInput),
    ///   2. pedirle a Android que cierre el teclado de la ventana activa.
    /// En el editor y en otras plataformas no hace nada.
    /// </summary>
    public static class AndroidKeyboard
    {
        public static void Hide()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                {
                    // Los cambios de teclado hay que hacerlos en el hilo de interfaz de Android.
                    activity.Call("runOnUiThread", new AndroidJavaRunnable(HideOnUiThread));
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[AndroidKeyboard] No se pudo preparar el cierre: " + e.Message);
            }
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private static void HideOnUiThread()
        {
            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            {
                HideWithUnityPlayer(activity);
                HideWithAndroid(activity);
            }
        }

        // Camino 1: el teclado de Unity vive dentro de UnityPlayer; se le pide que lo cierre.
        private static void HideWithUnityPlayer(AndroidJavaObject activity)
        {
            try
            {
                using (var player = activity.Get<AndroidJavaObject>("mUnityPlayer"))
                {
                    player.Call("hideSoftInput");
                    Debug.Log("[AndroidKeyboard] hideSoftInput de UnityPlayer: OK");
                }
            }
            catch (System.Exception e)
            {
                Debug.Log("[AndroidKeyboard] hideSoftInput de UnityPlayer: fallo (" + e.Message + ")");
            }
        }

        // Camino 2: el gestor de teclados de Android, con la ventana de la actividad y con la vista que tiene el foco.
        private static void HideWithAndroid(AndroidJavaObject activity)
        {
            try
            {
                using (var inputManager = activity.Call<AndroidJavaObject>("getSystemService", "input_method"))
                {
                    using (var window = activity.Call<AndroidJavaObject>("getWindow"))
                    using (var decorView = window.Call<AndroidJavaObject>("getDecorView"))
                    using (var token = decorView.Call<AndroidJavaObject>("getWindowToken"))
                    {
                        inputManager.Call<bool>("hideSoftInputFromWindow", token, 0);
                    }

                    using (var focused = activity.Call<AndroidJavaObject>("getCurrentFocus"))
                    {
                        if (focused != null)
                        {
                            using (var token = focused.Call<AndroidJavaObject>("getWindowToken"))
                            {
                                inputManager.Call<bool>("hideSoftInputFromWindow", token, 0);
                            }
                        }
                    }
                    Debug.Log("[AndroidKeyboard] hideSoftInputFromWindow: OK");
                }
            }
            catch (System.Exception e)
            {
                Debug.Log("[AndroidKeyboard] hideSoftInputFromWindow: fallo (" + e.Message + ")");
            }
        }
#endif
    }
}
