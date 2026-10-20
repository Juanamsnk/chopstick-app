using UnityEngine;
using UnityEngine.InputSystem;

public class AndroidBackButton : MonoBehaviour
{
    private void Update()
    {
#if UNITY_ANDROID
        if (Keyboard.current != null &&
            Keyboard.current[Key.Escape].wasPressedThisFrame)
        {
            Application.Quit();
        }
#endif
    }
}