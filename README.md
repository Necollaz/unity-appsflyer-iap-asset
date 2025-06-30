# AppsFlyer IAP Connector (Unity Package)

Пакет содержит только:
- Unity SDK AppsFlyer
- AppsFlyer Purchase Connector

## Установка

1. В Unity откройте **Window → Package Manager**.  
2. Нажмите на кнопку **+** → **Add package from git URL…**  
3. Вставьте: https://github.com/Necollaz/unity-appsflyer-asset.git#v1.0.0
4. Нажмите **Add**.

После этого в вашем проекте появится:
- Папка `Packages/com.yourcompany.appsflyer-iap/Runtime/AppsFlyerSDK`
- Папка `Packages/com.yourcompany.appsflyer-iap/Runtime/AppsFlyerPurchaseConnector`
- Ваш скрипт `AppsFlyerAdAndIAPCallbacks`

## Как пользоваться

1. В **Player Settings → Other Settings → Scripting Define Symbols (Android)** добавьте `AMAZON_STORE`, если собираете под Amazon.  
2. В сцене создайте пустой GameObject и прикрепите к нему компонент `AppsFlyerAdAndIAPCallbacks`.  
3. В инспекторе заполните поля:
- **AppsFlyer Dev Key**  
- **iOS App ID**  
- **Enable Debug Logs**  
4. Соберите и запустите на нужной платформе — SDK сам инициализируется и подключит Purchase Connector.

## Теги

- `v1.0.0`