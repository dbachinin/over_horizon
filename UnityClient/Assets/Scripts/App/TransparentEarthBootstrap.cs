using TransparentEarth.Ads;
using TransparentEarth.CameraFeed;
using TransparentEarth.Map;
using TransparentEarth.Rendering;
using TransparentEarth.Sensors;
using TransparentEarth.Store;
using TransparentEarth.UI;
using UnityEngine;

namespace TransparentEarth.App
{
    public static class TransparentEarthBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Start()
        {
            if (Object.FindFirstObjectByType<InstrumentOverlay>() != null) return;

            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            var root = new GameObject("OverHorizon App");
            Object.DontDestroyOnLoad(root);
            var location = root.AddComponent<LocationProvider>();
            var posePivot = new GameObject("Device Pose").AddComponent<DevicePoseProvider>();
            posePivot.transform.SetParent(root.transform, false);

            var cameraObject = new GameObject("Geospatial Camera");
            cameraObject.transform.SetParent(posePivot.transform, false);
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = TransparentEarthStyle.Ink;
            camera.fieldOfView = 62f;
            camera.nearClipPlane = .03f;
            camera.farClipPlane = 320f;
            root.AddComponent<ArCameraBackground>().Initialize(camera);
            var earthRoot = new GameObject("Earth Scene");
            earthRoot.transform.SetParent(root.transform, false);
            var earth = earthRoot.AddComponent<EarthRenderer>();
            earth.Build(camera, location);
            earth.CreateAntipodeFlag();
            var streamer = root.AddComponent<GeoObjectStreamer>();
            streamer.Initialize(root.transform, camera, earth, location);
            var map = root.AddComponent<OpenStreetMapTileLoader>();
            var ads = root.AddComponent<AdMobService>();
            ads.Initialize();

#if UNITY_ANDROID && !UNITY_EDITOR
            var subscriptions = root.AddComponent<GooglePlaySubscriptionService>();
            subscriptions.Initialize();
#else
            FlatEarthEntitlement.Broker = new SimulatedPurchaseBroker();
#endif

            var flatEarth = root.AddComponent<FlatEarthScreen>();
            flatEarth.Initialize(location, posePivot, streamer, earth, ads);

            var overlay = root.AddComponent<InstrumentOverlay>();
            overlay.Initialize(camera, posePivot, location, streamer.VisibleMarkers, map, streamer, earth, flatEarth, ads);
        }
    }
}
