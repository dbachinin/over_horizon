using System;
using System.Collections.Generic;
using System.Linq;
using TransparentEarth.Data;
using TransparentEarth.Geo;
using TransparentEarth.Sensors;
using UnityEngine;

namespace TransparentEarth.Rendering
{
    public sealed class GeoObjectStreamer : MonoBehaviour
    {
        private const int ZoneSizeDegrees = 20;
        private const int MaximumVisibleObjects = 18;
        private readonly Dictionary<ZoneKey, List<City>> _zones = new();
        private readonly List<CityMarkerView> _visible = new();
        private readonly Dictionary<string, CityMarkerView> _active = new();
        private Camera _camera;
        private EarthRenderer _earth;
        private LocationProvider _location;
        private Transform _markerRoot;
        private GeoPoint _observer;
        private float _nextRefresh;

        public IReadOnlyList<CityMarkerView> VisibleMarkers => _visible;
        public int LoadedZoneCount { get; private set; }

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
        }

        private void Update()
        {
            if (_camera == null || Time.unscaledTime < _nextRefresh) return;
            _nextRefresh = Time.unscaledTime + .22f;
            Refresh(force: GeoMath.DistanceKm(_observer, _location.Current) > 5d);
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
            var candidates = new List<(City city, GeoProjection projection, float score)>();
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
                    candidates.Add((city, projection, focus * 1000f + city.Importance));
                }
            }

            LoadedZoneCount = focusedZones;
            var desired = candidates.OrderByDescending(item => item.score)
                .Take(MaximumVisibleObjects).ToDictionary(item => item.city.Name, item => item);

            foreach (var key in _active.Keys.Where(key => !desired.ContainsKey(key)).ToArray())
            {
                Destroy(_active[key].Anchor.gameObject);
                if (_active[key].Flag != null) Destroy(_active[key].Flag.gameObject);
                _active.Remove(key);
            }

            foreach (var item in desired.Values)
            {
                if (_active.TryGetValue(item.city.Name, out var existing))
                {
                    existing.Anchor.localPosition = _earth.GeographicRotation * item.projection.DirectionEnu * 16f;
                    continue;
                }

                var anchor = new GameObject("Target · " + item.city.Name).transform;
                anchor.SetParent(_markerRoot, false);
                anchor.localPosition = _earth.GeographicRotation * item.projection.DirectionEnu * 16f;
                var flag = _earth.CreateFlag(item.projection, item.city.Name, false);
                _active[item.city.Name] = new CityMarkerView(item.city, item.projection, anchor, flag);
            }

            _visible.Clear();
            _visible.AddRange(_active.Values.OrderByDescending(marker => marker.City.Importance));
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
