# OverHorizon: Google Play and AdMob setup

The project is wired for one Google Play auto-renewing subscription and one bottom AdMob banner.
Keep the Android package name identical in every console: `com.transparentearth.unity`.

## IDs to configure

| Purpose | Console | Project location | Required value |
| --- | --- | --- | --- |
| Android package name | Google Play Console / AdMob | `Assets/Scripts/App/AppIdentity.cs` | `com.transparentearth.unity` |
| Subscription product ID | Play Console > Monetize > Products > Subscriptions | `AppIdentity.FlatEarthSubscriptionId` | `com.transparentearth.unity.flatearth` |
| Subscription base plan ID | Play Console, inside the subscription | Console only | For example `monthly`; do not put it in Unity |
| AdMob Android App ID | AdMob > Apps > OverHorizon > App settings | `Assets/Resources/AdMobConfig.json` → `androidAppId` | Looks like `ca-app-pub-123...~456...` |
| AdMob Android banner unit ID | AdMob > Ad units > Banner | `Assets/Resources/AdMobConfig.json` → `androidBannerAdUnitId` | Looks like `ca-app-pub-123.../789...` |
| Google Play public license key | Play Console licensing/monetization setup | Generate `Assets/Scripts/UnityPurchasing/generated/GooglePlayTangle.cs` using Unity Receipt Obfuscator | Full Base64 public key |

The AdMob App ID uses `~`; the banner ad-unit ID uses `/`. They are different IDs and must not be swapped.
The iOS values can remain Google's test IDs while publishing Android; they are ignored by the Android build.

## Google Play subscription

1. Create the app in Play Console with package name `com.transparentearth.unity`. This identifier cannot be changed after publishing.
2. Create a subscription whose product ID is exactly `com.transparentearth.unity.flatearth`.
3. Add and activate at least one auto-renewing base plan, for example `monthly`, with its price and supported regions. The base-plan ID is configured only in Play Console.
4. Publish the subscription changes. Upload an AAB to an internal test track and add license testers; store products normally cannot be tested from an Editor/APK sideload alone.
5. Keep the generated `GooglePlayTangle.cs` with `IsPopulated = true`. Do not edit anything in `Library/PackageCache`.

If a different immutable product ID was already created in Play Console, change
`FlatEarthSubscriptionId` in `Assets/Scripts/App/AppIdentity.cs` to that exact ID before building.

## AdMob banner

1. Add the Android app in AdMob using package name `com.transparentearth.unity`.
2. Create a **Banner** ad unit.
3. Copy the App ID and banner ad-unit ID into `Assets/Resources/AdMobConfig.json`.
4. Keep `useTestAds: true` during development. Set it to `false` only after inserting both production Android IDs for the release build.
5. In AdMob **Privacy & messaging**, publish the GDPR/European regulations message. The app uses Google UMP and requests ads only when `ConsentInformation.CanRequestAds()` allows it.
6. Complete Play Console **Data safety**, **Ads**, privacy-policy URL, content rating, target audience, and subscription disclosures according to the actual app behavior.

`OverHorizon/Prepare Android Project` copies the configured AdMob App ID into the Google Mobile Ads settings asset. Do not edit that generated settings asset separately.

## Release validation and build

Before release, configure upload signing through environment variables:

```text
OVERHORIZON_KEYSTORE_PATH
OVERHORIZON_KEYSTORE_PASS
OVERHORIZON_KEY_ALIAS
OVERHORIZON_KEY_ALIAS_PASS
OVERHORIZON_VERSION_NAME       (optional, for example 1.0.0)
OVERHORIZON_VERSION_CODE       (optional, must increase for every upload)
```

Then run:

1. `OverHorizon > Validate Google Play Release`
2. `OverHorizon > Build Google Play AAB`

Validation intentionally fails while Google test AdMob IDs are present, `useTestAds` is true,
the receipt key is not generated, signing is missing, or the Android identity is inconsistent.
