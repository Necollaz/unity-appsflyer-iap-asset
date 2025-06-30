# AppsFlyer IAP Connector (Unity Package)

Пакет содержит только:
- Unity SDK AppsFlyer
- AppsFlyer Purchase Connector

## Установка

1. Скачайте данный архир и установите в корень проекта.
2. Если в проекте не имеется плагина AppLovin MAX, скачать по ссылке: https://github.com/AppLovin/AppLovin-MAX-Unity-Plugin/releases

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