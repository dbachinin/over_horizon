using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TransparentEarth.Geo;
using UnityEngine;
using UnityEngine.Networking;

namespace TransparentEarth.Map
{
    public sealed class OpenStreetMapTileLoader : MonoBehaviour
    {
        public const int Zoom = 4;
        private const double MaximumLatitude = 85.05112878;
        private readonly Dictionary<int, Texture2D> _tiles = new();
        private readonly HashSet<int> _loading = new();

        public bool IsLoading => _loading.Count > 0;
        public bool HasTiles => _tiles.Count > 0;
        public string Error { get; private set; }

        public static Vector2 WorldTile(GeoPoint point)
        {
            var n = 1 << Zoom;
            var latitude = Math.Max(-MaximumLatitude, Math.Min(MaximumLatitude, point.Latitude));
            var x = (GeoMath.NormalizeLongitude(point.Longitude) + 180d) / 360d * n;
            var latitudeRadians = latitude * Math.PI / 180d;
            var y = (1d - Math.Log(Math.Tan(latitudeRadians) + 1d / Math.Cos(latitudeRadians)) / Math.PI) / 2d * n;
            return new Vector2((float)x, (float)y);
        }

        public void EnsureLoaded(Vector2 centerTile)
        {
            var centerX = Mathf.FloorToInt(centerTile.x);
            var centerY = Mathf.FloorToInt(centerTile.y);
            var n = 1 << Zoom;
            for (var offsetY = -1; offsetY <= 1; offsetY++)
            for (var offsetX = -1; offsetX <= 1; offsetX++)
            {
                var rawY = centerY + offsetY;
                if (rawY < 0 || rawY >= n) continue;
                var x = NormalizeX(centerX + offsetX);
                var key = TileKey(x, rawY);
                if (_tiles.ContainsKey(key) || !_loading.Add(key)) continue;
                Error = null;
                StartCoroutine(LoadTile(x, rawY, key));
            }
        }

        public bool TryGetTile(int tileX, int tileY, out Texture2D texture)
        {
            var n = 1 << Zoom;
            if (tileY < 0 || tileY >= n)
            {
                texture = null;
                return false;
            }
            return _tiles.TryGetValue(TileKey(NormalizeX(tileX), tileY), out texture);
        }

        private IEnumerator LoadTile(int x, int y, int key)
        {
            var directory = Path.Combine(Application.persistentDataPath, "osm", Zoom.ToString());
            var path = Path.Combine(directory, $"{x}_{y}.png");
            if (File.Exists(path) && DateTime.UtcNow - File.GetLastWriteTimeUtc(path) < TimeSpan.FromDays(7))
            {
                var cached = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (cached.LoadImage(File.ReadAllBytes(path)))
                {
                    _tiles[key] = cached;
                    _loading.Remove(key);
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
                _loading.Remove(key);
                yield break;
            }

            _tiles[key] = DownloadHandlerTexture.GetContent(request);
            Directory.CreateDirectory(directory);
            File.WriteAllBytes(path, request.downloadHandler.data);
            _loading.Remove(key);
        }

        private static int NormalizeX(int x)
        {
            var n = 1 << Zoom;
            return (x % n + n) % n;
        }

        private static int TileKey(int x, int y) => y * (1 << Zoom) + x;

        private void OnDestroy()
        {
            foreach (var texture in _tiles.Values)
                if (texture != null) Destroy(texture);
            _tiles.Clear();
        }
    }
}
