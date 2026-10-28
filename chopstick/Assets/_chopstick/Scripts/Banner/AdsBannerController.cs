using UnityEngine;

public class AdsBannerController : MonoBehaviour
{
    private void Start()
    {
        Invoke(nameof(InitBanner), 2f);
    }

    public void InitBanner()
    {
        AdsManager.Instance.ShowBanner();
    }
}
