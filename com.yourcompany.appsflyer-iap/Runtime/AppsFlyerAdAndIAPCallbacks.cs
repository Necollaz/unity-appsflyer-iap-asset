using System.Collections.Generic;
using UnityEngine;
using AppsFlyerConnector;
using AppsFlyerSDK;

public class AppsFlyerAdAndIAPCallbacks : MonoBehaviour, IAppsFlyerConversionData
{
#if UNITY_ANDROID && AMAZON_STORE
    private const Store CURRENT_STORE = Store.AMAZON;
#elif UNITY_ANDROID
        private const Store CURRENT_STORE = Store.GOOGLE;
#else
        private const Store CURRENT_STORE = Store.GOOGLE;
#endif
    
    private static AppsFlyerAdAndIAPCallbacks _instance;

    [Header("AppsFlyer Credentials")]
    [SerializeField, Tooltip("Dev Key из панели AppsFlyer (All Platforms)")] private string _appsFlyerDevKey = "e2GAFEK3u92ZRGPjhAq3r8";
    [SerializeField, Tooltip("iOS App ID без префикса 'id' — используется только для iOS")] private string _iosAppId = "123456789";
    [SerializeField, Tooltip("Включить детальные логи SDK (оставьте true для отладки)")] private bool _enableDebugLogs = true;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;

        DontDestroyOnLoad(gameObject);
        InitAppsFlyer();
        InitPurchaseConnector();

        AppsFlyer.startSDK();
    }

    private void InitAppsFlyer()
    {
#if !DEVELOPMENT_BUILD && !UNITY_EDITOR
        enableDebugLogs = false;
#endif
        AppsFlyer.setIsDebug(_enableDebugLogs);
        
#if UNITY_IOS && !UNITY_EDITOR
            AppsFlyer.initSDK(appsFlyerDevKey, iosAppId, this);
#else
        AppsFlyer.initSDK(_appsFlyerDevKey, "", this);
#endif
    }

    private void InitPurchaseConnector()
    {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
        AppsFlyerPurchaseConnector.setIsSandbox(true);
#endif
        AppsFlyerPurchaseConnector.init(this, CURRENT_STORE);
        AppsFlyerPurchaseConnector.setAutoLogPurchaseRevenue(
            AppsFlyerAutoLogPurchaseRevenueOptions.AppsFlyerAutoLogPurchaseRevenueOptionsInAppPurchases,
            AppsFlyerAutoLogPurchaseRevenueOptions.AppsFlyerAutoLogPurchaseRevenueOptionsAutoRenewableSubscriptions
        );
        AppsFlyerPurchaseConnector.setPurchaseRevenueValidationListeners(true);
        AppsFlyerPurchaseConnector.build();
        AppsFlyerPurchaseConnector.startObservingTransactions();
    }

    private void OnEnable()
    {
        MaxSdkCallbacks.Interstitial.OnAdRevenuePaidEvent += OnAdRevenuePaid;
        MaxSdkCallbacks.Rewarded.OnAdRevenuePaidEvent += OnAdRevenuePaid;
        MaxSdkCallbacks.Banner.OnAdRevenuePaidEvent += OnAdRevenuePaid;
        MaxSdkCallbacks.MRec.OnAdRevenuePaidEvent += OnAdRevenuePaid;
        MaxSdkCallbacks.AppOpen.OnAdRevenuePaidEvent += OnAdRevenuePaid;
    }

    private void OnDisable()
    {
        MaxSdkCallbacks.Interstitial.OnAdRevenuePaidEvent -= OnAdRevenuePaid;
        MaxSdkCallbacks.Rewarded.OnAdRevenuePaidEvent -= OnAdRevenuePaid;
        MaxSdkCallbacks.Banner.OnAdRevenuePaidEvent -= OnAdRevenuePaid;
        MaxSdkCallbacks.MRec.OnAdRevenuePaidEvent -= OnAdRevenuePaid;
        MaxSdkCallbacks.AppOpen.OnAdRevenuePaidEvent -= OnAdRevenuePaid;
    }
    
    private void OnAdRevenuePaid(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        var data = new AFAdRevenueData(adInfo.NetworkName, MediationNetwork.ApplovinMax, "USD", adInfo.Revenue);
        var extras = new Dictionary<string, string>();
        
        if (!string.IsNullOrEmpty(adUnitId))
            extras[AdRevenueScheme.AD_UNIT] = adUnitId;
        
        if (!string.IsNullOrEmpty(adInfo.AdFormat))
            extras[AdRevenueScheme.AD_TYPE] = adInfo.AdFormat;
        
        if (!string.IsNullOrEmpty(adInfo.Placement))
            extras[AdRevenueScheme.PLACEMENT] = adInfo.Placement;
        
        AppsFlyer.logAdRevenue(data, extras);
    }

    public void onConversionDataSuccess(string conversionData)
    {
        Debug.Log("[AF] ConversionData: " + conversionData);
    }

    public void onConversionDataFail(string error)
    {
        Debug.LogWarning("[AF] ConversionFail: " + error);
    }

    public void onAppOpenAttribution(string attributionData)
    {
        Debug.Log("[AF] AppOpenAttribution: " + attributionData);
    }

    public void onAppOpenAttributionFailure(string error)
    {
        Debug.LogWarning("[AF] AppOpenAttrFail: " + error);
    }
    
    public void didReceivePurchaseRevenueValidationInfo(string validationInfo)
    {
        Debug.Log("[AF] PurchaseValidation: " + validationInfo);
    }

    public void didReceivePurchaseRevenueError(string error)
    {
        Debug.LogWarning("[AF] PurchaseError: " + error);
    }
}