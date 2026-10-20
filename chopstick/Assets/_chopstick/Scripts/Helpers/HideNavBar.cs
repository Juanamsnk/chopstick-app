#if UNITY_ANDROID
using UnityEngine;

public class HideNavBar : MonoBehaviour
{
    void Awake()
    {
        AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

        // Ejecutar en el Main Thread de Android
        currentActivity.Call("runOnUiThread", new AndroidJavaRunnable(() => {
            AndroidJavaObject window = currentActivity.Call<AndroidJavaObject>("getWindow");
            AndroidJavaObject decorView = window.Call<AndroidJavaObject>("getDecorView");

            int SYSTEM_UI_FLAG_HIDE_NAVIGATION = 4;
            int SYSTEM_UI_FLAG_IMMERSIVE_STICKY = 4096;
            int SYSTEM_UI_FLAG_LAYOUT_STABLE = 256;

            decorView.Call("setSystemUiVisibility",
                SYSTEM_UI_FLAG_LAYOUT_STABLE |
                SYSTEM_UI_FLAG_HIDE_NAVIGATION |
                SYSTEM_UI_FLAG_IMMERSIVE_STICKY);
        }));
    }
}
#endif