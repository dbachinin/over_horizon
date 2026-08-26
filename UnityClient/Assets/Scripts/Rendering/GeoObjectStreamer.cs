using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using TransparentEarth.Data;
using TransparentEarth.Geo;
using TransparentEarth.Sensors;
using UnityEngine;
using UnityEngine.Networking;

namespace TransparentEarth.Rendering
{
    public sealed class GeoObjectStreamer : MonoBehaviour
    {
        private const int ZoneSizeDegrees = 20;
        private const int MaximumVisibleObjects = 28;
        private const int MinimumGlobalObjects = 20;
        private const double NearbyRadiusKm = 30d;
        private const int MaximumNearbyObjects = 10;
        private readonly Dictionary<ZoneKey, List<City>> _zones = new();
        private readonly List<CityMarkerView> _visible = new();
        private readonly Dictionary<string, CityMarkerView> _active = new();
        private readonly List<City> _nearbyPlaces = new();
        private Camera _camera;
        private EarthRenderer _earth;
        private LocationProvider _location;
        private Transform _markerRoot;
        private GeoPoint _observer;
        private float _nextRefresh;
        private float _nextNearbyRequest;
        private bool _loadingNearby;
        private bool _nearbyReady;
        private GeoPoint _nearbyLoadedFor;

        public IReadOnlyList<CityMarkerView> VisibleMarkers => _visible;
        public int LoadedZoneCount { get; private set; }
        public int NearbyPlaceCount => _nearbyPlaces.Count;
        public bool IsNearbyLoading => _loadingNearby;

        public void Initialize(Transform root, Camera sceneCamera, EarthRenderer earth, LocationProvider location)
        {
            _camera = sceneCamera;
            _earth = earth;
            _location = location;
            _markerRoot = new GameObject("Streamed Geo Objects").transform;
            _markerRoot.SetParent(root, false);
            BuildZoneIndex();
            _observer = location.Current;
            Refresh(force: true);
            StartCoroutine(LoadNearbyPlaces());
        }

        private void Update()
        {
            if (_camera == null || Time.unscaledTime < _nextRefresh) return;
            _nextRefresh = Time.unscaledTime + .22f;
            Refresh(force: GeoMath.DistanceKm(_observer, _location.Current) > 5d);
            if (!_loadingNearby && Time.unscaledTime >= _nextNearbyRequest &&
                (!_nearbyReady || GeoMath.DistanceKm(_nearbyLoadedFor, _location.Current) > 8d))
                StartCoroutine(LoadNearbyPlaces());
        }

        private void BuildZoneIndex()
        {
            foreach (var city in CityCatalog.All)
            {
                var key = ZoneKey.For(city.Position);
                if (!_zones.TryGetValue(key, out var cities)) _zones[key] = cities = new List<City>();
                cities.Add(city);
            }
        }

        private void Refresh(bool force)
        {
            if (force) _observer = _location.Current;
            var forward = _camera.transform.forward;
            var candidates = new List<(City city, GeoProjection projection, float score, bool nearby)>();
            var focusedZones = 0;

            foreach (var zone in _zones)
            {
                var zoneProjection = GeoMath.Project(_observer, zone.Key.Center);
                var zoneDirection = _earth.GeographicRotation * zoneProjection.DirectionEnu;
                if (!force && Vector3.Dot(forward, zoneDirection) < .22f) continue;
                if (Vector3.Dot(forward, zoneDirection) < -.05f) continue;
                focusedZones++;
                foreach (var city in zone.Value)
                {
                    var projection = GeoMath.Project(_observer, city.Position);
                    var displayDirection = _earth.GeographicRotation * projection.DirectionEnu;
                    var focus = Vector3.Dot(forward, displayDirection);
                    if (focus < .34f) continue;
                    candidates.Add((city, projection, focus * 1000f + city.Importance, false));
                }
            }

            var nearbyCandidates = _nearbyPlaces
                .Select(city => (city, projection: GeoMath.Project(_observer, city.Position)))
                .Where(item => item.projection.DistanceKm <= NearbyRadiusKm + .5d)
                .Select(item => (item.city, item.projection,
                    direction: _earth.GeographicRotation * item.projection.DirectionEnu))
                .Where(item => Vector3.Dot(forward, item.direction) > .12f)
                .OrderBy(item => item.projection.DistanceKm)
                .Take(MaximumNearbyObjects);
            foreach (var item in nearbyCandidates)
            {
                var focus = Vector3.Dot(forward, item.direction);
                candidates.Add((item.city, item.projection,
                    2200f + focus * 300f - (float)item.projection.DistanceKm * 5f, true));
            }

            LoadedZoneCount = focusedZones;
            var ranked = candidates.OrderByDescending(item => item.score).ToList();
            // Nearby OSM settlements must not push national capitals out of the focused sector.
            var selected = ranked.Where(item => !item.nearby).Take(MinimumGlobalObjects).ToList();
            var selectedKeys = selected.Select(item => MarkerKey(item.city)).ToHashSet();
            foreach (var item in ranked)
            {
                if (selected.Count >= MaximumVisibleObjects) break;
                if (!selectedKeys.Add(MarkerKey(item.city))) continue;
                selected.Add(item);
            }
            var desired = selected.ToDictionary(item => MarkerKey(item.city), item => item);

            foreach (var key in _active.Keys.Where(key => !desired.ContainsKey(key)).ToArray())
            {
                Destroy(_active[key].Anchor.gameObject);
                if (_active[key].Flag != null) Destroy(_active[key].Flag.gameObject);
                _active.Remove(key);
            }

            foreach (var item in desired.Values)
            {
                var key = MarkerKey(item.city);
                if (_active.TryGetValue(key, out var existing))
                {
                    existing.Anchor.localPosition = _earth.GeographicRotation * item.projection.DirectionEnu * 16f;
                    existing.Projection = item.projection;
                    continue;
                }

                var anchor = new GameObject("Target · " + item.city.Name).transform;
                anchor.SetParent(_markerRoot, false);
                anchor.localPosition = _earth.GeographicRotation * item.projection.DirectionEnu * 16f;
                _active[key] = new CityMarkerView(item.city, item.projection, anchor, item.nearby);
            }

            _visible.Clear();
            _visible.AddRange(_active.Values.OrderByDescending(marker => marker.City.Importance));
        }

        private IEnumerator LoadNearbyPlaces()
        {
            _loadingNearby = true;
            _nearbyLoadedFor = _location.Current;
            _nextNearbyRequest = Time.unscaledTime + 120f;
            var latitudeDelta = .275d;
            var longitudeDelta = .275d / Math.Max(.2d, Math.Cos(_nearbyLoadedFor.Latitude * Math.PI / 180d));
            var south = (_nearbyLoadedFor.Latitude - latitudeDelta).ToString("0.######", CultureInfo.InvariantCulture);
            var north = (_nearbyLoadedFor.Latitude + latitudeDelta).ToString("0.######", CultureInfo.InvariantCulture);
            var west = (_nearbyLoadedFor.Longitude - longitudeDelta).ToString("0.######", CultureInfo.InvariantCulture);
            var east = (_nearbyLoadedFor.Longitude + longitudeDelta).ToString("0.######", CultureInfo.InvariantCulture);
            var query = $"[out:json][timeout:15];nwr[\"place\"~\"^(city|town|village|hamlet)$\"][\"name\"]" +
                        $"({south},{west},{north},{east});out center 80;";
            var body = Encoding.UTF8.GetBytes("data=" + UnityWebRequest.EscapeURL(query));
            var endpoints = new[]
            {
                "https://overpass-api.de/api/interpreter",
                "https://overpass.kumi.systems/api/interpreter"
            };
            OverpassResponse response = null;
            var lastError = "no endpoint answered";
            foreach (var endpoint in endpoints)
            {
                using var request = new UnityWebRequest(endpoint, "POST")
                {
                    uploadHandler = new UploadHandlerRaw(body),
                    downloadHandler = new DownloadHandlerBuffer(),
                    timeout = 20
                };
                request.SetRequestHeader("Content-Type", "application/x-www-form-urlencoded");
                request.SetRequestHeader("User-Agent", "TransparentEarth-Unity/1.0");
                yield return request.SendWebRequest();
                if (request.result == UnityWebRequest.Result.Success)
                {
                    response = JsonUtility.FromJson<OverpassResponse>(request.downloadHandler.text);
                    if (response?.elements != null) break;
                    lastError = "invalid JSON response";
                }
                else lastError = request.error;
            }

            if (response?.elements != null)
            {
                var places = new List<City>();
                foreach (var element in response.elements)
                {
                    if (element.tags == null || string.IsNullOrWhiteSpace(element.tags.name)) continue;
                    var lat = element.center?.lat ?? element.lat;
                    var lon = element.center?.lon ?? element.lon;
                    var position = new GeoPoint(lat, lon);
                    var distance = GeoMath.DistanceKm(_nearbyLoadedFor, position);
                    if (distance > NearbyRadiusKm || places.Any(city => city.Name == element.tags.name)) continue;
                    if (CityCatalog.All.Any(city => city.Name == element.tags.name &&
                                                  GeoMath.DistanceKm(city.Position, position) < 2d)) continue;
                    var importance = element.tags.place switch
                    {
                        "city" => 58,
                        "town" => 50,
                        "village" => 42,
                        _ => 34
                    };
                    places.Add(new City(element.tags.name, "LOCAL", lat, lon, importance));
                }
                _nearbyPlaces.Clear();
                _nearbyPlaces.AddRange(places.OrderBy(city => GeoMath.DistanceKm(_nearbyLoadedFor, city.Position)));
                _nearbyReady = true;
                Debug.Log($"Loaded {_nearbyPlaces.Count} nearby OpenStreetMap places within 30 km");
                Refresh(force: true);
            }
            else Debug.LogWarning($"Nearby OpenStreetMap places unavailable: {lastError}");
            _loadingNearby = false;
        }

        private static string MarkerKey(City city) => city.Country + "|" + city.Name;

        [Serializable]
        private sealed class OverpassResponse
        {
            public OverpassElement[] elements;
        }

        [Serializable]
        private sealed class OverpassElement
        {
            public double lat;
            public double lon;
            public OverpassCenter center;
            public OverpassTags tags;
        }

        [Serializable]
        private sealed class OverpassCenter
        {
            public double lat;
            public double lon;
        }

        [Serializable]
        private sealed class OverpassTags
        {
            public string name;
            public string place;
        }

        private readonly struct ZoneKey : IEquatable<ZoneKey>
        {
            private readonly int _latitude;
            private readonly int _longitude;
            public GeoPoint Center => new(_latitude * ZoneSizeDegrees + ZoneSizeDegrees / 2d - 90d,
                _longitude * ZoneSizeDegrees + ZoneSizeDegrees / 2d - 180d);

            private ZoneKey(int latitude, int longitude)
            {
                _latitude = latitude;
                _longitude = longitude;
            }

            public static ZoneKey For(GeoPoint point) => new(
                Mathf.Clamp(Mathf.FloorToInt((float)(point.Latitude + 90d) / ZoneSizeDegrees), 0, 8),
                Mathf.Clamp(Mathf.FloorToInt((float)(point.Longitude + 180d) / ZoneSizeDegrees), 0, 17));

            public bool Equals(ZoneKey other) => _latitude == other._latitude && _longitude == other._longitude;
            public override bool Equals(object obj) => obj is ZoneKey other && Equals(other);
            public override int GetHashCode() => (_latitude * 397) ^ _longitude;
        }
    }
}
