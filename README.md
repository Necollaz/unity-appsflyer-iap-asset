# AppsFlyer IAP Connector (Unity Package)

Пакет содержит только:
- Unity SDK AppsFlyer
- AppsFlyer Purchase Connector

## Установка

1. Скачайте данный архир и установите в корень проекта.
2. Если в проекте не имеется плагина **AppLovin MAX**, скачать по ссылке: https://github.com/AppLovin/AppLovin-MAX-Unity-Plugin/releases.
С подробной настройкой **AppLovin MAX** можете ознакомиться по ссылке: https://ubiquitous-shrimp-656.notion.site/AppLovin-MAX-1fb71854e7b18028afaada7333383e93?pvs=74

## Как пользоваться

1. В **Player Settings → Other Settings → Scripting Define Symbols (Android)** добавьте `AMAZON_STORE`, если собираете под Amazon.  
2. В сцене создайте пустой GameObject и прикрепите к нему компонент `AppsFlyerAdAndIAPCallbacks`.  
3. В инспекторе заполните поля:
    - **AppsFlyer Dev Key** — ваш Dev Key из панели AppsFlyer (All Platforms).
    - **iOS App ID** — ваш Apple ID в App Store (без префикса `id`).
    - **Enable Debug Logs** — оставьте `true` для логирования в консоль (рекомендуется в процессе отладки).
4. Соберите и запустите на нужной платформе — SDK сам:
   - Инициализируется,
   - Запускает Purchase Connector,
   - Начинает слушать IAP-транзакции и авто-отправку “first launch” и revenue-ивентов.

## Детальное использование скрипта AppsFlyerAdAndIAPCallbacks

Ниже разбор того, **что и когда** внутри этого компонента вызывается, чтобы вы понимали логику и не было ошибок:

### 1. Жизненный цикл и инициализация

<details>
<summary>Сам жизненый цикл</summary>

```csharp
private void Awake()
{
    // Singleton-паттерн — чтобы не было дубликатов при смене сцен
    if (_instance != null && _instance != this)
    {
        Destroy(gameObject);
        return;
    }
    _instance = this;
    DontDestroyOnLoad(gameObject);

    // 1) Инициализируем AppsFlyer SDK
    InitAppsFlyer();

    // 2) Стартуем сбор и отправку данных
    AppsFlyer.startSDK();

    // 3) Инициализируем Purchase Connector (IAP)
    InitPurchaseConnector();
}
```
</details>

1.InitAppsFlyer()
   - Включает или выключает отладочные логи через `AppsFlyer.setIsDebug(_enableDebugLogs)`.
   - Вызывает `AppsFlyer.initSDK(devKey, appId, this)`:
     - Под Android передаётся пустая строка вместо appId.
     - Под iOS — оба параметра (devKey + iosAppId).

2.`AppsFlyer.startSDK()` — отправляет внутри “first launch” и запускает загрузку кэша и конфигов.
3.`InitPurchaseConnector()`

   - В режиме разработки (`DEVELOPMENT_BUILD` или в редакторе) включает sandbox-режим:
```csharp
AppsFlyerPurchaseConnector.setIsSandbox(true);
```

   - Инициализирует Connector и указывает текущий стор:
```csharp
AppsFlyerPurchaseConnector.init(this, CURRENT_STORE);
```
где `CURRENT_STORE` = `Store.AMAZON` или `Store.GOOGLE` по дефайну.

   - Авто-лог покупки и подписок:
```csharp
AppsFlyerPurchaseConnector.setAutoLogPurchaseRevenue(
    AppsFlyerAutoLogPurchaseRevenueOptions.AppsFlyerAutoLogPurchaseRevenueOptionsInAppPurchases,
    AppsFlyerAutoLogPurchaseRevenueOptions.AppsFlyerAutoLogPurchaseRevenueOptionsAutoRenewableSubscriptions
);
```

   - Включает лисенеры валидации:
```csharp
AppsFlyerPurchaseConnector.setPurchaseRevenueValidationListeners(true);
```

   - Собирает конфигурацию и запускает наблюдение за транзакциями:
```csharp
AppsFlyerPurchaseConnector.build();
AppsFlyerPurchaseConnector.startObservingTransactions();
```

### 2. Подписка на рекламные события (Ad Revenue)
В методах `OnEnable`/`OnDisable` мы привязываем и отвязываем ваши колбэки:
```csharp
MaxSdkCallbacks.Interstitial.OnAdRevenuePaidEvent += OnAdRevenuePaid;
// то же для Rewarded, Banner, MRec, AppOpen
```
<details>
<summary>Метод обработки:</summary>

```csharp
private void OnAdRevenuePaid(string adUnitId, MaxSdkBase.AdInfo adInfo)
{
    Debug.Log($"[AF AdRevenue] unit={adUnitId} net={adInfo.NetworkName} fmt={adInfo.AdFormat} rev={adInfo.Revenue}");
    
    var data = new AFAdRevenueData(
        adInfo.NetworkName,
        MediationNetwork.ApplovinMax,
        "USD",
        adInfo.Revenue
    );
    var extras = new Dictionary<string, string>
    {
        [AdRevenueScheme.AD_UNIT] = adUnitId,
        [AdRevenueScheme.AD_TYPE] = adInfo.AdFormat,
        [AdRevenueScheme.PLACEMENT] = adInfo.Placement
    };
    AppsFlyer.logAdRevenue(data, extras);
}
```

- Обязательно вызываем `AppsFlyer.logAdRevenue` с `AFAdRevenueData` и картой `extras`.
- Логи появляются с префиксом `[AF AdRevenue]` — их легко фильтровать.
</details>

### 3. Реализация IAppsFlyerConversionData
Интерфейс нужен для получения данных атрибуции и валидации покупок:
```csharp
public void onConversionDataSuccess(string conversionData) { … }
public void onConversionDataFail(string error) { … }
public void onAppOpenAttribution(string attributionData) { … }
public void onAppOpenAttributionFailure(string error) { … }
public void didReceivePurchaseRevenueValidationInfo(string validationInfo) { … }
public void didReceivePurchaseRevenueError(string error) { … }
```

   - **ConversionData** — первые данные об установке/кампании.
   - **PurchaseValidationInfo** — ответ от сервера AppsFlyer по каждой верифицированной покупке.
   - *В логах они помечены `[AF] PurchaseValidation` или `[AF] PurchaseError`.

### 4. Возможные ошибки и куда смотреть в логах
1. **Нет сообщений “AppsFlyer…”**
   - Проверьте, что `InitAppsFlyer()` вызывается в `Awake`, до `startSDK()`.
   - Убедитесь, что Dev Key и App ID корректны.
   - Включите `Enable Debug Logs = true`.


2. **“first launch” ушёл, но нет валидации IAP**
   - Убедитесь, что `setAutoLogPurchaseRevenue` вызван ДО `build()` и `startObservingTransactions()`.
   - Проверьте, что передаёте правильный `CURRENT_STORE` под вашу платформу.


3. **Нет колбэков `didReceivePurchaseRevenueValidationInfo`**
   - На устройствах iOS в редакторе не работает реальная валидация (sandbox).
   - В редакторе/DEVELOPMENT_BUILD включён sandbox, но в релизе отключён: проверьте условную компиляцию.

4. **Ошибки в Ad Revenue**
   - Если `OnAdRevenuePaid` не вызывается — скорее всего, не настроены SDK callback’ы MaxSdk.
   - Посмотрите, появляются ли события в логах Applovin MAX.

## Когда нужно вызывать что-то в других классах (если нужно ручное управление)

Единственная ситуация, когда придётся обращаться к AppsFlyer в своём коде — это **ручная отправка покупки**, если вы не используете Unity IAP или хотите полный контроль над параметрами. В этом случае после успешного платежа нужно вызвать:
```csharp
AppsFlyer.validateAndTrackInAppPurchase(
    productId:      receipt.productID,            // ваш product ID
    price:          receipt.localizedPrice.ToString(),
    currency:       receipt.isoCurrencyCode,
    transactionId:  receipt.transactionID,
    receipt:        receipt.receipt,              // iOS- строчка квитанции
    signature:      receipt.transactionReceiptSignature // Android- signature
);
```
- `productId`, `price`, `currency` и `transactionId` берутся из вашего объекта покупки.
- Для **iOS** нужен только `receipt`, для **Android** — `receipt` + `signature`.

<details>
<summary>Пример</summary>

```csharp
public class MyPurchaseManager : MonoBehaviour
{
    public void OnPurchaseComplete(Product product)
    {
        #if UNITY_IOS
        var receiptJson = product.receipt;
        AppsFlyer.validateAndTrackInAppPurchase(
            product.definition.id,
            product.metadata.localizedPrice.ToString(),
            product.metadata.isoCurrencyCode,
            product.transactionID,
            receiptJson,
            null
        );
        #elif UNITY_ANDROID
        // парсим JSON, чтобы достать signature
        var googleReceipt = JsonUtility.FromJson<GooglePlayPurchaseReceipt>(product.receipt);
        AppsFlyer.validateAndTrackInAppPurchase(
            product.definition.id,
            product.metadata.localizedPrice.ToString(),
            product.metadata.isoCurrencyCode,
            product.transactionID,
            googleReceipt.json,
            googleReceipt.signature
        );
        #endif

        // ваш остальной код успешной покупки…
    }
}
```
</details>

**ВАЖНО**: если вы **НЕ** делаете вручную `validateAndTrackInAppPurchase`, а полагаетесь на `AppsFlyerPurchaseConnector`, 
то **НИКАКИХ** дополнительных вызовов из других классов делать не нужно — Connector автоматически отловит транзакцию и отправит её в AppsFlyer.

## Настройка LVL (License Verification Library)
Пошаговая инструкция по добавлению проверки лицензии Google Play (LVL) для Android-версии.
### Получение лицензионного ключа (Public Key)
- Открыть Google Play Console → проект → Setup → App integrity.
- В разделе License key скопировать строку вида MIIBIjANBgkqhki….
- Сохраните её — она понадобится в плагине.
### Добавление google-play-licensing.jar
- В Android SDK путь к библиотеке:<Android SDK>/extras/google/play_licensing/lib/google-play-licensing.jar.
- Скопировать этот файл в папку Assets/Plugins/Android/libs/ вашего проекта.
  - Если папки нет, создайте её вручную.
### Java‑плагин для Unity
В Assets/Plugins/Android/src/com/yourcompany/licensing/ создать два файла:
<details>
<summary>ObfuscatorUtil.java</summary>

   ```csharp
package com.yourcompany.licensing;

import android.content.Context;
import android.provider.Settings;

public class ObfuscatorUtil
{
    public static String getDeviceId(Context context)
    {
        return Settings.Secure.getString(context.getContentResolver(), Settings.Secure.ANDROID_ID);
    }
}
```
</details>
<details>
<summary>LicenseCheckerBridge.java</summary>

 ```csharp
package com.yourcompany.licensing;

import android.app.Activity;
import android.widget.Toast;
import com.android.vending.licensing.*;

public class LicenseCheckerBridge {
    private static final String BASE64_PUBLIC_KEY = "ВАШ_ПУБЛИЧНЫЙ_КЛЮЧ";
    private static LicenseChecker mChecker;
    private static Policy mPolicy;

    public static void init(Activity activity) {
        mPolicy = new ServerManagedPolicy(
            activity,
            new AESObfuscator(
                ObfuscatorUtil.getDeviceId(activity).getBytes(),
                activity.getPackageName(),
                BASE64_PUBLIC_KEY
            )
        );
        mChecker = new LicenseChecker(activity, mPolicy, BASE64_PUBLIC_KEY);
    }

    public static void checkLicense(final Activity activity) {
        if (mChecker == null) init(activity);
        mChecker.checkAccess(new LicenseCheckerCallback() {
            @Override
            public void allow(int reason) {
                activity.runOnUiThread(() ->
                    Toast.makeText(activity, "The license has been confirmed", Toast.LENGTH_SHORT).show()
                );
            }

            @Override
            public void dontAllow(int reason) {
                activity.runOnUiThread(() ->
                    Toast.makeText(activity, "Please purchase the game from Google Play.", Toast.LENGTH_LONG).show()
                );
                activity.finish();
            }

            @Override
            public void applicationError(int errorCode) {
                activity.runOnUiThread(() ->
                    Toast.makeText(
                        activity,
                        "License verification error: " + errorCode,
                        Toast.LENGTH_LONG
                    ).show()
                );
            }
        });
    }
}
```
</details>

### C#‑обёртка в Unity
В папке Assets/Scripts/ создайте файл:

<details>
<summary>LicenseManager.cs</summary>

```csharp
using UnityEngine;

public class LicenseManager : MonoBehaviour
{
    void Start()
    {
        var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
        var bridge = new AndroidJavaClass("com.yourcompany.licensing.LicenseCheckerBridge");

        bridge.CallStatic("init", activity);
        bridge.CallStatic("checkLicense", activity);
    }
}
```

- Путь к вашему Java-классу
    ```csharp
    var bridge = new AndroidJavaClass("com.yourcompany.licensing.LicenseCheckerBridge");
    ```
  Если в LicenseCheckerBridge.java мы указали свой package (например, com.acme.mygame.licensing), то здесь должно быть точно такое же:
    ```csharp
    var bridge = new AndroidJavaClass("com.acme.mygame.licensing.LicenseCheckerBridge");
    ```
</details>

Прикрепите этот скрипт к объекту на стартовой сцене.

## Теги

- `v1.0.0`