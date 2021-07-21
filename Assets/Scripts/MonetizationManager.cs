using System;
using UnityEngine;
using UnityEngine.Advertisements;
using UnityEngine.Purchasing;
using UnityEngine.UI;
using System.Collections;

#if UNITY_ANDROID
using Unity.Notifications.Android;
#elif UNITY_IOS
using Unity.Notifications.iOS;
#endif

public class MonetizationManager : MonoBehaviour,
    IUnityAdsInitializationListener, IUnityAdsLoadListener, IUnityAdsShowListener

{
    public event Action<string> OnPurchased;
    public event Action<int> OnCoinsChanged;

    public GameObject backfillBanner;
    public GameObject backfillInterstitial;
    public GameObject backfillRewarded;
    public Button closeButton;
    public Image closeImage;

    int timeout = 3;

    bool debugMode = false;

    string gameID = "";
    string bannerID = "";
    string interstitialID = "";
    string rewardedID = "";
    
    public enum REWARD_TYPE
    {
        COINS_5,
        COINS_17,
        COINS_42
    };

    REWARD_TYPE selectedRewardType;

    bool giveUserReward = false;

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
        debugMode = Debug.isDebugBuild;

        Advertisement.Banner.SetPosition(BannerPosition.TOP_CENTER);
        closeButton.gameObject.SetActive(false);
        backfillInterstitial.SetActive(false);
        backfillRewarded.SetActive(false);

        //caso seja necessario mostrar banners logo no start
        backfillBanner.SetActive(true);

        //troque esses numeros pelos IDs do seu projeto
#if UNITY_IOS
        gameID ="ID_iOS_DO_PROJETO";//ID_iOS_DO_PROJETO;
        bannerID = "iOS_Banner";
        interstitialID = "iOS_Interstitial";
        rewardedID = "iOS_Rewarded";
#elif UNITY_ANDROID
        gameID = "ID_ANDROID_DO_PROJETO";//ID_ANDROID_DO_PROJETO";
        bannerID = "Android_Banner";
        interstitialID = "Android_Interstitial";
        rewardedID = "Android_Rewarded";
#endif

        Advertisement.debugMode = false;
        Advertisement.Initialize(gameID, debugMode, true, this);

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
    //IUnityAdsInitializationListener
    public void OnInitializationComplete()
    {
        if (debugMode) Debug.Log("[MonetizationManager] OnInitializationComplete");
        LoadBanner();
        Advertisement.Load(interstitialID, this);
        Advertisement.Load(rewardedID, this);
    }

    public void OnInitializationFailed(UnityAdsInitializationError error, string message)
    {
        if (debugMode) Debug.Log("[MonetizationManager] OnInitializationFailed: " + error.ToString() + " | " + message);
    }

    //-----------------------------------------------------------
    //IUnityAdsLoadListener
    public void OnUnityAdsAdLoaded(string placementId)
    {
        if (debugMode) Debug.Log("[MonetizationManager] OnUnityAdsAdLoaded: " + placementId);
    }

    public void OnUnityAdsFailedToLoad(string placementId, UnityAdsLoadError error, string message)
    {
        if (debugMode) Debug.Log("[MonetizationManager] OnUnityAdsFailedToLoad: " + placementId + " | " + error.ToString() + " | " + message);
    }

    //-----------------------------------------------------------
    //IUnityAdsShowListener
    public void OnUnityAdsShowFailure(string placementId, UnityAdsShowError error, string message)
    {
        if (debugMode) Debug.Log("[MonetizationManager] OnUnityAdsShowFailure: " + placementId + " | " + error.ToString() + " | " + message);
    }

    public void OnUnityAdsShowStart(string placementId)
    {
        if (debugMode) Debug.Log("[MonetizationManager] OnUnityAdsShowStart: " + placementId);
    }

    public void OnUnityAdsShowClick(string placementId)
    {
        if (debugMode) Debug.Log("[MonetizationManager] OnUnityAdsShowClick: " + placementId);
    }

    public void OnUnityAdsShowComplete(string placementId, UnityAdsShowCompletionState showCompletionState)
    {
        if (debugMode) Debug.Log("[MonetizationManager] OnUnityAdsShowComplete: " + placementId + " | " + showCompletionState.ToString());

        LoadBanner();

        if (placementId.Trim().Equals(rewardedID) && showCompletionState == UnityAdsShowCompletionState.COMPLETED)
        {
            giveUserReward = true;
        }

        Advertisement.Load(placementId, this);
    }

    //-----------------------------------------------------------
    public void Update()
    {
        if (closeImage.fillAmount < 1)
        {
            closeImage.fillAmount += Time.deltaTime / timeout;
        }
        else
        {
            closeButton.interactable = true;
        }

        if (giveUserReward == true)
        {
            giveUserReward = false;
            RewardUser();
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
        if (debugMode) Debug.Log("[MonetizationManager] ShowInterstitial");
        HideBanner();
        if (Advertisement.IsReady(interstitialID))
        {
            Advertisement.Show(interstitialID, this);
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
    public void ShowRewarded(REWARD_TYPE rewardType)
    {
        if (debugMode) Debug.Log("[MonetizationManager] ShowRewarded: " + rewardType);
        HideBanner();
        
        selectedRewardType = rewardType;

        if (Advertisement.IsReady(rewardedID))
        {
            Advertisement.Show(rewardedID, this);
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
    //Banners
    public void LoadBanner()
    {
        if (debugMode) Debug.Log("[MonetizationManager] LoadBanner");
        // Set up options to notify the SDK of load events:
        BannerLoadOptions options = new BannerLoadOptions
        {
            loadCallback = OnBannerLoaded,
            errorCallback = OnBannerError
        };

        // Load the Ad Unit with banner content:
        Advertisement.Banner.Load(bannerID, options);
    }

    private void OnBannerError(string message)
    {
        if (debugMode) Debug.Log("[MonetizationManager] OnBannerError: " + message);
        backfillBanner.SetActive(true);
    }

    private void OnBannerLoaded()
    {
        if (debugMode) Debug.Log("[MonetizationManager] OnBannerLoaded");

        //mostra o banner na tela exatamente no momento em que ele carrega
        ShowBanner();
    }

    public void ShowBanner()
    {
        if (PlayerPrefs.GetInt("PURCHASED_REMOVEADS") == 1) return;
        if (debugMode) Debug.Log("[MonetizationManager] ShowBanner");

        BannerOptions options = new BannerOptions
        {
            clickCallback = OnBannerClicked,
            hideCallback = OnBannerHidden,
            showCallback = OnBannerShown
        };

        backfillBanner.SetActive(false);
        // Show the loaded Banner Ad Unit:
        Advertisement.Banner.Show(bannerID, options);
    }

    private void OnBannerShown()
    {
        if (debugMode) Debug.Log("[MonetizationManager] OnBannerShown");
    }

    private void OnBannerHidden()
    {
        if (debugMode) Debug.Log("[MonetizationManager] OnBannerHidden");
    }

    private void OnBannerClicked()
    {
        if (debugMode) Debug.Log("[MonetizationManager] OnBannerClicked");
    }

    //-----------------------------------------------------------
    public void HideBanner()
    {
        if (debugMode) Debug.Log("[MonetizationManager] HideBanner");
        backfillBanner.SetActive(false);
        Advertisement.Banner.Hide();
    }

    //-----------------------------------------------------------
    public void RewardUser()
    {
        if (debugMode) Debug.Log("[MonetizationManager] RewardUser:" + selectedRewardType.ToString());

        switch (selectedRewardType)
        {
            case REWARD_TYPE.COINS_5:
                AddCoins(5);
                break;
            case REWARD_TYPE.COINS_17:
                AddCoins(17);
                break;
            case REWARD_TYPE.COINS_42:
                AddCoins(42);
                break;
        }
    }
    //-----------------------------------------------------------
    public void AddCoins(int coinsToAdd)
    {
        if (debugMode) Debug.Log("[MonetizationManager] AddCoins: " + coinsToAdd);
        int coins = PlayerPrefs.GetInt("COINS");
        coins += coinsToAdd;
        PlayerPrefs.SetInt("COINS", coins);
        PlayerPrefs.Save();
        OnCoinsChanged?.Invoke(coins);
    }

    //-----------------------------------------------------------
    public void OnPurchaseComplete(Product product)
    {
        if (debugMode) Debug.Log("[MonetizationManager] OnPurchaseComplete: " + product.ToString());

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
        if (Debug.isDebugBuild) Debug.Log("[MonetizationManager] CheckSubscription");
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
        if (debugMode) Debug.Log("[MonetizationManager] SendLocalNotification: " + title);
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