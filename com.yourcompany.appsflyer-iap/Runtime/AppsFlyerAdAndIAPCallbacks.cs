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
        AppsFlyer.startSDK();
        InitPurchaseConnector();
    }
    
    private void OnEnable()
    {
        MaxSdkCallbacks.Interstitial.OnAdRevenuePaidEvent += OnAdRevenuePaid;
    }

    private void OnDisable()
    {
        MaxSdkCallbacks.Interstitial.OnAdRevenuePaidEvent -= OnAdRevenuePaid;
    }
    
    private void InitAppsFlyer()
    {
        Debug.Log("[AF] InitAppsFlyer()");
        
#if !DEVELOPMENT_BUILD && !UNITY_EDITOR
        _enableDebugLogs = false;
#endif
        AppsFlyer.setIsDebug(_enableDebugLogs);
        
#if UNITY_IOS && !UNITY_EDITOR
        AppsFlyer.initSDK(appsFlyerDevKey, _iosAppId, this);
#else
        AppsFlyer.initSDK(_appsFlyerDevKey, "", this);
#endif
    }

    private void InitPurchaseConnector()
    {
        Debug.Log("[AF] InitPurchaseConnector()");
        
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

    private void OnAdRevenuePaid(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        Debug.Log($"[AF AdRevenue] unit={adUnitId} revenue={adInfo.Revenue}");
        
        double revenue = adInfo.Revenue;
        string network = adInfo.NetworkName;
        string placement = adInfo.Placement;
        string format = adInfo.AdFormat;

        Debug.Log($"[AF AdRevenue] unit={adUnitId} network={network} format={format} revenue={revenue}");
        
        var data = new AFAdRevenueData(network, MediationNetwork.ApplovinMax, "USD", revenue);
        var extras = new Dictionary<string, string> { [AdRevenueScheme.AD_UNIT]   = adUnitId, [AdRevenueScheme.AD_TYPE]   = format, [AdRevenueScheme.PLACEMENT] = placement };
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