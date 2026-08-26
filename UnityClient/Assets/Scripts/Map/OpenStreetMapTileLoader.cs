using System;
using System.Collections;
using System.IO;
using TransparentEarth.Geo;
using UnityEngine;
using UnityEngine.Networking;

namespace TransparentEarth.Map
{
    public sealed class OpenStreetMapTileLoader : MonoBehaviour
    {
        private const int Zoom = 4;
        private const double MaximumLatitude = 85.05112878;
        private bool _loading;
        private int _tileX = int.MinValue;
        private int _tileY = int.MinValue;

        public Texture2D Texture { get; private set; }
        public Vector2 MarkerUv { get; private set; } = new(.5f, .5f);
        public string Error { get; private set; }

        public void EnsureLoaded(GeoPoint antipode)
        {
            var n = 1 << Zoom;
            var latitude = Math.Max(-MaximumLatitude, Math.Min(MaximumLatitude, antipode.Latitude));
            var x = (GeoMath.NormalizeLongitude(antipode.Longitude) + 180d) / 360d * n;
            var latitudeRadians = latitude * Math.PI / 180d;
            var y = (1d - Math.Log(Math.Tan(latitudeRadians) + 1d / Math.Cos(latitudeRadians)) / Math.PI) / 2d * n;
            var tileX = ((int)Math.Floor(x) % n + n) % n;
            var tileY = Math.Max(0, Math.Min(n - 1, (int)Math.Floor(y)));
            MarkerUv = new Vector2((float)(x - Math.Floor(x)), 1f - (float)(y - Math.Floor(y)));
            if (_loading || (_tileX == tileX && _tileY == tileY && Texture != null)) return;
            _tileX = tileX;
            _tileY = tileY;
            StartCoroutine(LoadTile(tileX, tileY));
        }

        private IEnumerator LoadTile(int x, int y)
        {
            _loading = true;
            Error = null;
            var directory = Path.Combine(Application.persistentDataPath, "osm", Zoom.ToString());
            var path = Path.Combine(directory, $"{x}_{y}.png");
            if (File.Exists(path) && DateTime.UtcNow - File.GetLastWriteTimeUtc(path) < TimeSpan.FromDays(7))
            {
                var cached = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (cached.LoadImage(File.ReadAllBytes(path)))
                {
                    Texture = cached;
                    _loading = false;
                    yield break;
                }
                Destroy(cached);
            }

            var url = $"https://tile.openstreetmap.org/{Zoom}/{x}/{y}.png";
            using var request = UnityWebRequestTexture.GetTexture(url);
            request.SetRequestHeader("User-Agent", "OverHorizon/1.0 (Android; com.transparentearth.unity)");
            yield return request.SendWebRequest();
            if (request.result != UnityWebRequest.Result.Success)
            {
                Error = request.error;
                _loading = false;
                yield break;
            }

            Texture = DownloadHandlerTexture.GetContent(request);
            Directory.CreateDirectory(directory);
            File.WriteAllBytes(path, request.downloadHandler.data);
            _loading = false;
        }
    }
}
