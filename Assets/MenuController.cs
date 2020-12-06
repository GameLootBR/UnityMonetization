using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuController : MonoBehaviour
{
    public Text coinsText;
    public GameObject playStage2Button;
    public GameObject buyStage2Button;
    public GameObject removeadsButton;
    public GameObject interstitialButton;

    void Start()
    {

        MonetizationManager.Instance.OnPurchased += MonetizationManager_OnPurchased;
        MonetizationManager.Instance.OnCoinsChanged += MonetizationManager_OnCoinsChanged;

        MonetizationManager_OnPurchased(null);
        MonetizationManager_OnCoinsChanged(PlayerPrefs.GetInt("COINS"));
    }

    private void OnDestroy()
    {
        if (MonetizationManager.Instance != null)
        {
            MonetizationManager.Instance.OnPurchased -= MonetizationManager_OnPurchased;
            MonetizationManager.Instance.OnCoinsChanged -= MonetizationManager_OnCoinsChanged;
        }
    }

    private void MonetizationManager_OnCoinsChanged(int coins)
    {
        coinsText.text = coins.ToString("N0");
    }

    private void MonetizationManager_OnPurchased(string productId)
    {
        bool purchasedStage2 = PlayerPrefs.GetInt("PURCHASED_STAGE2") == 1;
        bool purchasedRemoveAds = PlayerPrefs.GetInt("PURCHASED_REMOVEADS") == 1;

        removeadsButton.SetActive(!purchasedRemoveAds);
        interstitialButton.SetActive(!purchasedRemoveAds);
        playStage2Button.SetActive(purchasedStage2);
        buyStage2Button.SetActive(!purchasedStage2);
    }

    public void OnInterstitialButtonClick()
    {
        MonetizationManager.Instance.ShowInterstitial();
    }

    public void OnRewardedButtonClick()
    {
        MonetizationManager.Instance.ShowRewarded();
    }

    public void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    public void SendNotification()
    {
        MonetizationManager.Instance.SendLocalNotification("Volte logo!", "Venha ver as novidades...");
    }
}
