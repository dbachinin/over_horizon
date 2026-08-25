using System.Collections;
using TransparentEarth.Geo;
using UnityEngine;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif

namespace TransparentEarth.Sensors
{
    public sealed class LocationProvider : MonoBehaviour
    {
        public GeoPoint Current { get; private set; } = new GeoPoint(44.7866, 20.4489, 117);
        public float AccuracyMeters { get; private set; } = 4f;
        public bool IsLive { get; private set; }

        private IEnumerator Start()
        {
#if UNITY_ANDROID
            if (!Permission.HasUserAuthorizedPermission(Permission.FineLocation))
                Permission.RequestUserPermission(Permission.FineLocation);
#endif
            if (!Input.location.isEnabledByUser) yield break;
            Input.location.Start(3f, 3f);
            var attempts = 15;
            while (Input.location.status == LocationServiceStatus.Initializing && attempts-- > 0)
                yield return new WaitForSeconds(1f);
            IsLive = Input.location.status == LocationServiceStatus.Running;
        }

        private void Update()
        {
            if (Input.location.status != LocationServiceStatus.Running) return;
            var data = Input.location.lastData;
            Current = new GeoPoint(data.latitude, data.longitude, data.altitude);
            AccuracyMeters = data.horizontalAccuracy;
            IsLive = true;
        }

        private void OnDestroy()
        {
            if (Input.location.status == LocationServiceStatus.Running) Input.location.Stop();
        }
    }
}
