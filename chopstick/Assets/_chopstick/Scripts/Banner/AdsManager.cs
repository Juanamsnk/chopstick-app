using System;
using GoogleMobileAds.Api;
using GoogleMobileAds.Common;
using UnityEngine;

public class AdsManager : MonoBehaviour
{
    public static float BannerHeightPx { get; private set; }
    public static event Action<float> OnBannerHeightChanged;

    /// <summary>
    /// Alto (en pixeles de pantalla) que la interfaz debe dejar libre arriba para que el banner no tape nada.
    /// Mientras el banner carga se reserva el tamano estandar (50 dp) para que la UI no "salte" al aparecer.
    /// Vale 0 si no hay anuncios (fallo al cargar, banner oculto o no esta en Android).
    /// </summary>
    public static float ReservedHeightPx
    {
        get
        {
            if (BannerHeightPx > 0f) return BannerHeightPx;
#if UNITY_ANDROID && !UNITY_EDITOR
            if (instance != null && !bannerFailed && !bannerHidden)
                return AdSize.Banner.Height * (Screen.dpi > 0f ? Screen.dpi : 160f) / 160f;
#endif
            return 0f;
        }
    }

    /// <summary>Se avisa cuando cambia el espacio que hay que reservar (ReservedHeightPx).</summary>
    public static event Action OnReservedHeightChanged;

    private static bool bannerFailed;
    private static bool bannerHidden;

    private static AdsManager instance;
    private BannerView bannerView;
    private bool bannerReady = false;

    private string adUnitIdAndroid = "ca-app-pub-6192873534407725/7671330122";

    public static AdsManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindAnyObjectByType<AdsManager>();
                if (instance == null)
                {
                    GameObject obj = new GameObject("AdsManager");
                    instance = obj.AddComponent<AdsManager>();
                }
            }
            return instance;
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
        OnReservedHeightChanged?.Invoke();   // ya hay anuncios en camino: la interfaz deja sitio arriba
    }

    private void Start()
    {
        InitializeMobileAds();
        LoadBannerAd();
    }

    private void InitializeMobileAds()
    {
        MobileAds.Initialize(initStatus =>
        {
            Debug.Log("Google Mobile Ads inicializado");
        });
    }

    private void LoadBannerAd()
    {
        var adRequest = new AdRequest();

#if UNITY_ANDROID
        string adUnitId = adUnitIdAndroid;
#elif UNITY_IOS
        string adUnitId = adUnitIdIOS;
#else
        string adUnitId = "unused";
#endif

        bannerView = new BannerView(adUnitId, AdSize.Banner, AdPosition.Top);

        bannerView.OnBannerAdLoaded += () =>
        {
            MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                Debug.Log("Banner cargado correctamente!");
                bannerReady = true;
                SetBannerHeight(bannerView.GetHeightInPixels());
            });
        };

        bannerView.OnBannerAdLoadFailed += (LoadAdError error) =>
        {
            Debug.LogError("Error cargando banner: " + error.GetMessage());
            MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                bannerFailed = true;                    // sin banner no hace falta dejar hueco
                OnReservedHeightChanged?.Invoke();
            });
        };

        Debug.Log("Cargando banner...");
        bannerView.LoadAd(adRequest);
    }

    public void ShowBanner()
    {
        if (!bannerReady)
        {
            Debug.LogWarning("Banner aún no está listo");
            return;
        }

        if (bannerView != null)
        {
            bannerHidden = false;
            bannerView.Show();
            SetBannerHeight(bannerView.GetHeightInPixels());
            Debug.Log("Banner mostrado");
        }
    }

    public void HideBanner()
    {
        if (bannerView != null)
        {
            bannerHidden = true;
            bannerView.Hide();
            SetBannerHeight(0f);
        }
    }

    private static void SetBannerHeight(float px)
    {
        BannerHeightPx = px;
        OnBannerHeightChanged?.Invoke(px);
        OnReservedHeightChanged?.Invoke();
    }

    private void OnDestroy()
    {
        bannerView?.Destroy();
    }
}