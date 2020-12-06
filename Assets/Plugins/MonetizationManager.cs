using System;
using UnityEngine;
using UnityEngine.Advertisements;
using UnityEngine.Purchasing;
using UnityEngine.UI;

#if UNITY_ANDROID
using Unity.Notifications.Android;
#elif UNITY_IOS
using Unity.Notifications.iOS;
#endif

public class MonetizationManager : MonoBehaviour, IUnityAdsListener
{
    public event Action<string> OnPurchased;
    public event Action<int> OnCoinsChanged;

    public GameObject backfillBanner;
    public GameObject backfillInterstitial;
    public GameObject backfillRewarded;
    public Button closeButton;
    public Image closeImage;

    int timeout = 3;

    private static MonetizationManager _Instance;
    public static MonetizationManager Instance
    {
        get
        {
            if (_Instance == null)
            {
                _Instance = FindObjectOfType<MonetizationManager>();

                if (_Instance == null)
                {
                    GameObject monetizationObject = Instantiate(Resources.Load<GameObject>("MonetizationManager"));
                    _Instance = monetizationObject.GetComponent<MonetizationManager>();
                }
            }
            
            return _Instance;
        }
    }
 
    //-----------------------------------------------------------
    void Awake()
    {
        System.GC.Collect();

        DontDestroyOnLoad(this);
        closeButton.gameObject.SetActive(false);
        backfillBanner.SetActive(false);
        backfillInterstitial.SetActive(false);
        backfillRewarded.SetActive(false);

        //caso seja necessario mostrar banners logo no start
        ShowBanner();

        string gameId = "";

        //troque esses numeros pelos IDs do seu projeto
#if UNITY_IOS
        gameId = "ID_iOS_DO_PROJETO";
#elif UNITY_ANDROID
        gameId = "ID_ANDROID_DO_PROJETO";
#endif
        Advertisement.AddListener(this);
        Advertisement.Initialize(gameId, Debug.isDebugBuild);
#if UNITY_ANDROID
        var c = new AndroidNotificationChannel()
        {
            Id = "channel_id",
            Name = "Default Channel",
            Importance = Importance.High,
            Description = "Generic notifications",
        };
        AndroidNotificationCenter.RegisterNotificationChannel(c);
#endif
    }

    //-----------------------------------------------------------
    private void Update()
    {
        if (closeImage.fillAmount < 1)
        {
            closeImage.fillAmount += Time.deltaTime / timeout;
        }
        else
        {
            closeButton.interactable = true;
        }
    }

    //-----------------------------------------------------------
    public void OpenURL(string url)
    {
        Application.OpenURL(url);
    }

    //-----------------------------------------------------------
    public void ShowInterstitial()
    {
        if (PlayerPrefs.GetInt("PURCHASED_REMOVEADS") == 1) return;

        HideBanner();
        if (Advertisement.IsReady("interstitial"))
        {
            Advertisement.Show("interstitial");
        }
        else
        {
            timeout = 3;
            closeButton.gameObject.SetActive(true);
            closeButton.interactable = false;
            closeImage.fillAmount = 0;
            backfillInterstitial.SetActive(true);
        }
    }

    //-----------------------------------------------------------
    public void ShowRewarded()
    {
        HideBanner();
        if (Advertisement.IsReady("rewarded"))
        {
            Advertisement.Show("rewarded");
        }
        else
        {
            timeout = 15;
            closeButton.gameObject.SetActive(true);
            closeButton.interactable = false;
            closeImage.fillAmount = 0;
            backfillRewarded.SetActive(true);
            Invoke("RewardUser", timeout);
        }
    }

    //-----------------------------------------------------------
    public void ShowBanner()
    {
        Debug.Log("ShowBanner");
        if (PlayerPrefs.GetInt("PURCHASED_REMOVEADS") == 1) return;

        if (Advertisement.IsReady("banner"))
        {
            backfillBanner.SetActive(false);
            Advertisement.Banner.SetPosition(BannerPosition.TOP_CENTER);
            Advertisement.Banner.Show("banner");
        }
        else
        {
            backfillBanner.SetActive(true);
            Advertisement.Banner.Hide();
        }
    }

    //-----------------------------------------------------------
    public void HideBanner()
    {
        Debug.Log("HideBanner");
        backfillBanner.SetActive(false);
        Advertisement.Banner.Hide();
    }

    //-----------------------------------------------------------
    public void OnUnityAdsReady(string placementId)
    {
        //executa quando um placementId esta pronto para ser mostrado na tela
        if (placementId.Equals("banner"))
        {
            ShowBanner();
        }
    }

    //-----------------------------------------------------------
    public void OnUnityAdsDidError(string message)
    {
        Debug.LogError("UNITY ADS ERROR: " + message);
    }

    //-----------------------------------------------------------
    public void OnUnityAdsDidStart(string placementId)
    {
        //executa quando um video comeca a ser mostrado na tela
    }

    //-----------------------------------------------------------
    public void OnUnityAdsDidFinish(string placementId, ShowResult showResult)
    {
        ShowBanner();
        if (placementId == "rewarded" && showResult == ShowResult.Finished)
        {
            RewardUser();
        }
    }

    //-----------------------------------------------------------
    public void RewardUser()
    {
        AddCoins(5);
    }

    //-----------------------------------------------------------
    public void AddCoins(int coinsToAdd)
    {
        int coins = PlayerPrefs.GetInt("COINS");
        coins += coinsToAdd;
        PlayerPrefs.SetInt("COINS", coins);
        PlayerPrefs.Save();

        OnCoinsChanged?.Invoke(coins);
    }

    //-----------------------------------------------------------
    public void OnPurchaseComplete(Product product)
    {
        if (product.definition.id.Equals("coinspack1"))
        {
            AddCoins(100);
        }
        else if (product.definition.id.Equals("coinspack2"))
        {
            AddCoins(500);
        }
        else if (product.definition.id.Equals("stage2"))
        {
            PlayerPrefs.SetInt("PURCHASED_STAGE2", 1);
            PlayerPrefs.Save();
        }
        else if (product.definition.id.Equals("removeads"))
        {
            PlayerPrefs.SetInt("PURCHASED_REMOVEADS", 1);
            PlayerPrefs.Save();
            HideBanner();
        }

        if (OnPurchased != null)
        {
            OnPurchased(product.definition.id);
        }
    }

    //-----------------------------------------------------------
    public static void CheckSubscription()
    {
        Debug.Log("CheckSubscription");
#if !UNITY_EDITOR
        Product removeads = CodelessIAPStoreListener.Instance.GetProduct("removeads");
        if (removeads.hasReceipt)
        {
            SubscriptionManager removeAdsManager = new SubscriptionManager(removeads, null);
            if (removeAdsManager.getSubscriptionInfo().isSubscribed() != Result.True)
            {
                PlayerPrefs.SetInt("PURCHASED_REMOVEADS", 0);
                PlayerPrefs.Save();
                FindObjectOfType<MonetizationManager>().ShowBanner();
            }
        }
#endif
    }

    //-----------------------------------------------------------
    public void SendLocalNotification(string title, string msg, int seconds = 10)
    {
#if UNITY_ANDROID
        var notification = new AndroidNotification
        {
            Title = title,
            Text = msg,
            LargeIcon = "large_icon",
            FireTime = System.DateTime.Now.AddSeconds(seconds)
        };

        AndroidNotificationCenter.SendNotification(notification, "channel_id");
#elif UNITY_IOS
        var timeTrigger = new iOSNotificationTimeIntervalTrigger()
        {
            TimeInterval = new TimeSpan(0, 0, seconds),
            Repeats = false
        };

        var notification = new iOSNotification()
        {
            // You can optionally specify a custom identifier which can later be 
            // used to cancel the notification, if you don't set one, a unique 
            // string will be generated automatically.
            Identifier = "_notification_01",
            Title = title,
            Body = msg,
            Subtitle = Application.productName,
            ShowInForeground = true,
            ForegroundPresentationOption = (PresentationOption.Alert | PresentationOption.Sound),
            CategoryIdentifier = "category_a",
            ThreadIdentifier = "thread1",
            Trigger = timeTrigger,
        };

        iOSNotificationCenter.ScheduleNotification(notification);
#endif
    }
}
