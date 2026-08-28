using System;
using System.Collections.Generic;
using GoogleMobileAds.Api;
using GoogleMobileAds.Common;
using GoogleMobileAds.Ump.Api;
using UnityEngine;

namespace TransparentEarth.Ads
{
    /// <summary>
    /// Consent-first AdMob bootstrap. Production identifiers live in Resources/AdMobConfig.json;
    /// the checked-in configuration deliberately uses Google's fixed test ad units.
    /// </summary>
    public sealed class AdMobService : MonoBehaviour
    {
        private const string ConfigResource = "AdMobConfig";
        private const string AndroidTestBanner = "ca-app-pub-3940256099942544/6300978111";
        private const string IosTestBanner = "ca-app-pub-3940256099942544/2934735716";
        private BannerView _banner;
        private AdMobConfig _config;
        private bool _initializationStarted;
        private bool _bannerLoaded;
        private bool _suppressed;

        public bool PrivacyOptionsRequired =>
            ConsentInformation.PrivacyOptionsRequirementStatus == PrivacyOptionsRequirementStatus.Required;

        public int BottomInsetPixels
        {
            get
            {
                if (!_bannerLoaded || _suppressed) return 0;
                var density = Screen.dpi > 0f ? Screen.dpi / 160f : 1f;
                return Mathf.RoundToInt(50f * density);
            }
        }

        public void Initialize()
        {
            _config = LoadConfig();
            if (_config.useTestAds)
            {
                MobileAds.SetRequestConfiguration(new RequestConfiguration
                {
                    TestDeviceIds = new List<string> { AdRequest.TestDeviceSimulator }
                });
            }

            RequestConsentUpdate();
        }

        public void SetSuppressed(bool suppressed)
        {
            _suppressed = suppressed;
            ApplyBannerVisibility();
        }

        public void ShowPrivacyOptions()
        {
            ConsentForm.ShowPrivacyOptionsForm(error =>
            {
                if (error != null) Debug.LogWarning("AdMob privacy options: " + error.Message);
                if (ConsentInformation.CanRequestAds()) QueueInitializeAds();
            });
        }

        private void RequestConsentUpdate()
        {
            var parameters = new ConsentRequestParameters
            {
                TagForUnderAgeOfConsent = false
            };
            ConsentInformation.Update(parameters, updateError =>
            {
                if (updateError != null)
                    Debug.LogWarning("AdMob consent update: " + updateError.Message);

                if (ConsentInformation.CanRequestAds())
                {
                    QueueInitializeAds();
                    return;
                }

                if (updateError != null) return;
                ConsentForm.LoadAndShowConsentFormIfRequired(showError =>
                {
                    if (showError != null)
                        Debug.LogWarning("AdMob consent form: " + showError.Message);
                    if (ConsentInformation.CanRequestAds()) QueueInitializeAds();
                });
            });
        }

        private void QueueInitializeAds() =>
            MobileAdsEventExecutor.ExecuteInUpdate(InitializeAds);

        private void InitializeAds()
        {
            if (_initializationStarted) return;
            _initializationStarted = true;
            MobileAds.Initialize(status =>
            {
                if (status == null)
                {
                    Debug.LogWarning("Google Mobile Ads initialization failed.");
                    return;
                }
                MobileAdsEventExecutor.ExecuteInUpdate(CreateBanner);
            });
        }

        private void CreateBanner()
        {
            DestroyBanner();
            var unitId = BannerUnitId();
            if (string.IsNullOrWhiteSpace(unitId))
            {
                Debug.LogWarning("AdMob banner unit ID is empty; banner disabled.");
                return;
            }

            _banner = new BannerView(unitId, AdSize.Banner, AdPosition.Bottom);
            _banner.Hide();
            _banner.OnBannerAdLoaded += () => MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                _bannerLoaded = true;
                ApplyBannerVisibility();
                Debug.Log("AdMob banner loaded" + (_config.useTestAds ? " (test)" : string.Empty) + ".");
            });
            _banner.OnBannerAdLoadFailed += error => MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                _bannerLoaded = false;
                Debug.LogWarning("AdMob banner load failed: " + error);
            });
            _banner.LoadAd(new AdRequest());
        }

        private void ApplyBannerVisibility()
        {
            if (_banner == null || !_bannerLoaded) return;
            if (_suppressed) _banner.Hide();
            else _banner.Show();
        }

        private string BannerUnitId()
        {
#if UNITY_IOS
            return _config.useTestAds ? IosTestBanner : _config.iosBannerAdUnitId;
#else
            return _config.useTestAds ? AndroidTestBanner : _config.androidBannerAdUnitId;
#endif
        }

        private static AdMobConfig LoadConfig()
        {
            var asset = Resources.Load<TextAsset>(ConfigResource);
            if (asset != null)
            {
                try
                {
                    var parsed = JsonUtility.FromJson<AdMobConfig>(asset.text);
                    if (parsed != null) return parsed;
                }
                catch (Exception exception)
                {
                    Debug.LogWarning("Could not read AdMobConfig.json: " + exception.Message);
                }
            }
            return new AdMobConfig { useTestAds = true };
        }

        private void DestroyBanner()
        {
            _bannerLoaded = false;
            if (_banner == null) return;
            _banner.Destroy();
            _banner = null;
        }

        private void OnDestroy() => DestroyBanner();

        [Serializable]
        private sealed class AdMobConfig
        {
            public bool useTestAds = true;
            public string androidAppId;
            public string iosAppId;
            public string androidBannerAdUnitId;
            public string iosBannerAdUnitId;
        }
    }
}
