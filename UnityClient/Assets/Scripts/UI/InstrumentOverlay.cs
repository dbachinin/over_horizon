using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using TransparentEarth.Data;
using TransparentEarth.Geo;
using TransparentEarth.I18n;
using TransparentEarth.Map;
using TransparentEarth.Rendering;
using TransparentEarth.Sensors;
using UnityEngine;
using UnityEngine.Networking;

namespace TransparentEarth.UI
{
    public sealed class InstrumentOverlay : MonoBehaviour
    {
        private Camera _camera;
        private DevicePoseProvider _pose;
        private LocationProvider _location;
        private IReadOnlyList<CityMarkerView> _markers;
        private OpenStreetMapTileLoader _map;
        private GeoObjectStreamer _streamer;
        private EarthRenderer _earth;
        private Texture2D _panel;
        private Texture2D _mint;
        private Texture2D _white;
        private Texture2D _layerOn;
        private Texture2D _layerOff;
        private Texture2D _compassButton;
        private Texture2D _leaderRight;
        private Texture2D _leaderLeft;
        private Texture2D _directionArrow;
        private GUIStyle _eyebrow;
        private GUIStyle _title;
        private GUIStyle _small;
        private GUIStyle _markerTitle;
        private GUIStyle _markerMeta;
        private GUIStyle _referenceLabel;
        private GUIStyle _buttonText;
        private GUIStyle _searchField;
        private bool _transparentEarth = true;
        private bool _hasAntipodeObject;
        private GeoPoint _resolvedAntipode;
        private GeographicObjectDirection _nearestAntipodeObject;
        private readonly List<PlaceSearchResult> _placeResults = new();
        private readonly Dictionary<string, PlaceSearchResult[]> _placeSearchCache =
            new(StringComparer.OrdinalIgnoreCase);
        private string _placeQuery = string.Empty;
        private string _placeSearchError = string.Empty;
        private bool _placeSearchStarted;
        private bool _placeSearching;
        private float _lastPlaceSearchAt = -10f;
        private int _tab;

        public void Initialize(Camera sceneCamera, DevicePoseProvider pose, LocationProvider location,
            IReadOnlyList<CityMarkerView> markers, OpenStreetMapTileLoader map, GeoObjectStreamer streamer,
            EarthRenderer earth)
        {
            _camera = sceneCamera;
            _pose = pose;
            _location = location;
            _markers = markers;
            _map = map;
            _streamer = streamer;
            _earth = earth;
        }

        private void Awake()
        {
            _panel = Solid(new Color(.035f, .075f, .063f, .93f));
            _mint = Solid(TransparentEarthStyle.Mint);
            _white = Solid(Color.white);
            _layerOn = Solid(new Color(.18f, .42f, .34f, .88f));
            _layerOff = Solid(new Color(.06f, .12f, .105f, .72f));
            _compassButton = Circle(new Color(.035f, .075f, .063f, .58f));
            _leaderRight = Leader(pointsRight: true);
            _leaderLeft = Leader(pointsRight: false);
            _directionArrow = DirectionArrow();
        }

        private void OnGUI()
        {
            if (_camera == null) return;
            EnsureStyles();
            if (_title == null) return;
            var safe = Screen.safeArea;
            var scale = Mathf.Max(.85f, Screen.width / 430f);
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1));
            var width = safe.width / scale;
            var height = safe.height / scale;
            var left = safe.x / scale;
            var top = (Screen.height - safe.yMax) / scale;
            _earth.SetInteractionEnabled(_tab == 0);

            GUI.Label(new Rect(left + 20, top + 16, 260, 18), "OVERHORIZON", _eyebrow);
            var title = _tab switch
            {
                0 => AppText.Get(TextKey.LookThroughHorizon),
                1 => AppText.Get(TextKey.OtherSideOfEarth),
                _ => AppText.Get(TextKey.PlaceSearchTitle)
            };
            GUI.Label(new Rect(left + 20, top + 34, 350, 34), title, _title);
            StatusPill(new Rect(left + width - 82, top + 20, 62, 28));

            if (_tab == 0)
            {
                Metrics(left, top, width, height, scale);
                DrawMarkers(scale, left, width);
                DrawAntipodeTarget(scale, left, width);
                DrawReferenceLegends(scale);
                OrientationButton(left, top, width);
            }
            else if (_tab == 1) DrawAntipode(left, top, width, height);
            else DrawPlaces(left, top, width, height);

            BottomDeck(left, top, width, height);
        }

        private void Metrics(float left, float top, float width, float height, float scale)
        {
            var viewPitch = _pose.PitchDegrees + _earth.ManualLookPitchDegrees;
            var localStatus = _streamer.IsNearbyLoading ? "…" : _streamer.NearbyPlaceCount.ToString();
            var text = $"AZ  {_pose.HeadingDegrees:000}°   TILT {viewPitch:+0;-0;0}°   GPS ±{_location.AccuracyMeters:0}m   LOCAL {localStatus}";
            GUI.Label(new Rect(left + width / 2 - 150, top + 76, 300, 24), text, _small);
            var horizon = _camera.WorldToScreenPoint(_earth.HorizonWorldPoint);
            if (horizon.z > 0)
            {
                var horizonY = (Screen.height - horizon.y) / scale;
                GUI.Label(new Rect(left + 20, horizonY - 17, 180, 18),
                    $"—  {AppText.Get(TextKey.PhysicalHorizon)}", _small);
            }
        }

        private void DrawMarkers(float scale, float left, float width)
        {
            var horizonScreen = _camera.WorldToScreenPoint(_earth.HorizonWorldPoint);
            var occupiedLabels = new List<Rect>();
            foreach (var marker in _markers)
            {
                if (!_transparentEarth && marker.Projection.ElevationDegrees < -1) continue;
                var viewport = _camera.WorldToScreenPoint(marker.Anchor.position);
                if (viewport.z <= 0 || viewport.x < -100 || viewport.x > Screen.width + 100 ||
                    viewport.y < Screen.height * .2f || viewport.y > Screen.height * .82f) continue;
                if (horizonScreen.z > 0f)
                {
                    var earthSideGap = (marker.IsNearby ? 72f : 8f) * scale;
                    viewport.y = CityMarkerView.ConstrainToEarthSide(viewport.y, horizonScreen.y, earthSideGap);
                }
                var x = viewport.x / scale;
                var y = (Screen.height - viewport.y) / scale;
                var pointsRight = x < left + width * .63f;
                var horizonProximity = horizonScreen.z > 0f
                    ? 1f - Mathf.Clamp01(Mathf.Abs(viewport.y - horizonScreen.y) / (Screen.height * .14f))
                    : 0f;
                var labelLift = Mathf.SmoothStep(0f, 26f, horizonProximity);
                var pulse = _earth.ScanPulseAt((float)marker.Projection.CentralAngleDegrees);
                var reveal = marker.AccumulateReveal(
                    _earth.MarkerRevealAt((float)marker.Projection.CentralAngleDegrees));
                if (reveal < .015f) continue;
                var accent = marker.Accent;
                accent.a = reveal * (.76f + pulse * .24f);
                GUI.color = accent;
                var underlineX = marker.HasLeaderLine
                    ? pointsRight ? x + 21f : x - 139f
                    : pointsRight ? x + 9f : x - 127f;
                var textX = underlineX + 2f;
                var labelRect = new Rect(textX - 5f, y - 40f - labelLift, 158f, 39f);
                for (var attempt = 0; attempt < 5 && OverlapsAny(labelRect, occupiedLabels); attempt++)
                {
                    labelLift += 38f;
                    labelRect.y = y - 40f - labelLift;
                }
                occupiedLabels.Add(labelRect);
                var underlineY = y - 20f - labelLift;
                if (marker.HasLeaderLine)
                {
                    var leaderX = pointsRight ? x : x - 22f;
                    GUI.DrawTexture(new Rect(leaderX, underlineY, 22f, 20f + labelLift),
                        pointsRight ? _leaderRight : _leaderLeft);
                }
                if (horizonProximity > 0f)
                {
                    GUI.color = new Color(1f, 1f, 1f, Mathf.Lerp(.42f, .88f, horizonProximity));
                    GUI.DrawTexture(labelRect, _panel);
                    GUI.color = accent;
                }
                GUI.DrawTexture(new Rect(underlineX, underlineY, 118f, 1.25f + pulse * 1.1f), _white);
                GUI.DrawTexture(new Rect(x - 3f - pulse, y - 3f - pulse, 6f + pulse * 2f, 6f + pulse * 2f), _white);
                var titleStyle = marker.IsNearby ? _markerMeta : _markerTitle;
                GUI.Label(new Rect(textX, y - 38f - labelLift, 154f, 18f),
                    PlaceNames.Get(marker.City.Name).ToUpperInvariant(), titleStyle);
                var depth = marker.Projection.ElevationDegrees < -.1
                    ? $"{Math.Abs(marker.Projection.ElevationDegrees):0.0}° {AppText.Get(TextKey.Below)}"
                    : AppText.Get(TextKey.OnHorizon);
                var meta = marker.IsNearby
                    ? $"{marker.Projection.DistanceKm:0.0} {AppText.Get(TextKey.Kilometers)} · {AppText.Get(TextKey.Nearby)}"
                    : $"{marker.Projection.DistanceKm:0} {AppText.Get(TextKey.Kilometers)} · {depth}";
                GUI.Label(new Rect(textX, y - 18f - labelLift, 154f, 16f), meta, _markerMeta);
                GUI.color = Color.white;
            }
        }

        private static bool OverlapsAny(Rect candidate, List<Rect> occupied)
        {
            var padded = new Rect(candidate.x - 5f, candidate.y - 4f,
                candidate.width + 10f, candidate.height + 8f);
            foreach (var rect in occupied)
                if (padded.Overlaps(rect)) return true;
            return false;
        }

        private void DrawReferenceLegends(float scale)
        {
            if (!_earth.ReferencesVisible || _referenceLabel == null) return;
            var labels = new[]
            {
                AppText.Get(TextKey.Equator),
                AppText.Get(TextKey.TropicCancer),
                AppText.Get(TextKey.TropicCapricorn),
                AppText.Get(TextKey.Greenwich),
                AppText.Get(TextKey.DateLine)
            };
            for (var track = 0; track < labels.Length; track++)
            {
                var found = false;
                var bestScore = float.MaxValue;
                var bestViewport = Vector3.zero;
                for (var sample = 0; sample < 48; sample++)
                {
                    // Anchors are fixed on the Earth. Selecting the closest visible anchor makes
                    // the label follow the gaze without an independent back-and-forth animation.
                    var phase = track * .137f + sample / 48f;
                    GeoPoint point;
                    if (track < 3)
                    {
                        var latitude = track == 0 ? 0d : track == 1 ? 23.436d : -23.436d;
                        var longitude = Mathf.Repeat(phase, 1f) * 360d - 180d;
                        point = new GeoPoint(latitude, longitude);
                    }
                    else
                    {
                        var latitude = -82d + Mathf.PingPong(phase * 2f, 1f) * 164d;
                        point = new GeoPoint(latitude, track == 3 ? 0d : 180d);
                    }

                    var candidate = _camera.WorldToScreenPoint(_earth.GeographicSurfacePoint(point));
                    if (candidate.z <= 0f || candidate.x < Screen.width * .07f || candidate.x > Screen.width * .93f ||
                        candidate.y < Screen.height * .20f || candidate.y > Screen.height * .79f) continue;
                    var score = Mathf.Abs(candidate.x - Screen.width * .5f) +
                                Mathf.Abs(candidate.y - Screen.height * .52f) * .38f;
                    if (score >= bestScore) continue;
                    found = true;
                    bestScore = score;
                    bestViewport = candidate;
                }
                if (!found) continue;
                var x = bestViewport.x / scale;
                var y = (Screen.height - bestViewport.y) / scale;
                var size = _referenceLabel.CalcSize(new GUIContent(labels[track]));
                var rect = new Rect(x - size.x * .5f - 7f, y - 13f, size.x + 14f, 23f);
                GUI.color = new Color(1f, 1f, 1f, .76f);
                GUI.DrawTexture(rect, _panel);
                GUI.color = TransparentEarthStyle.BlueprintGold;
                GUI.DrawTexture(new Rect(rect.x + 5f, rect.yMax - 3f, rect.width - 10f, 1f), _white);
                GUI.Label(new Rect(rect.x + 7f, rect.y + 1f, rect.width - 14f, 17f), labels[track], _referenceLabel);
                GUI.color = Color.white;
            }
        }

        private void DrawAntipodeTarget(float scale, float left, float width)
        {
            var viewport = _camera.WorldToScreenPoint(_earth.AntipodeWorldPoint);
            if (viewport.z <= 0f || viewport.x < -80f || viewport.x > Screen.width + 80f ||
                viewport.y < Screen.height * .20f || viewport.y > Screen.height * .82f) return;

            var x = viewport.x / scale;
            var y = (Screen.height - viewport.y) / scale;
            var pulse = .5f + .5f * Mathf.Sin(Time.unscaledTime * 4.2f);
            var pointsRight = x < left + width * .63f;
            var underlineX = pointsRight ? x + 27f : x - 151f;
            var leaderX = pointsRight ? x : x - 28f;
            GUI.color = TransparentEarthStyle.Signal;
            GUI.DrawTexture(new Rect(x - 7f - pulse * 2f, y - 1f, 14f + pulse * 4f, 2f), _white);
            GUI.DrawTexture(new Rect(x - 1f, y - 7f - pulse * 2f, 2f, 14f + pulse * 4f), _white);
            GUI.DrawTexture(new Rect(leaderX, y - 28f, 28f, 28f), pointsRight ? _leaderRight : _leaderLeft);
            GUI.DrawTexture(new Rect(underlineX, y - 28f, 124f, 1.5f), _white);
            GUI.DrawTexture(new Rect(underlineX + 2f, y - 47f, 152f, 19f), _panel);
            GUI.Label(new Rect(underlineX + 5f, y - 47f, 146f, 18f),
                AppText.Get(TextKey.AntipodePoint), _referenceLabel);
            GUI.color = Color.white;
        }

        private void DrawAntipode(float left, float top, float width, float height)
        {
            var antipode = GeoMath.Antipode(_location.Current);
            var nearest = NearestAntipodeObject(antipode);
            _map.EnsureLoaded(antipode);
            var mapSize = width - 36;
            var mapRect = new Rect(left + 18, top + 92, mapSize, mapSize);
            GUI.color = Color.white;
            if (_map.Texture != null)
            {
                GUI.DrawTextureWithTexCoords(mapRect, _map.Texture, new Rect(0, 0, 1, 1), false);
                var markerX = mapRect.x + _map.MarkerUv.x * mapRect.width;
                var markerY = mapRect.y + (1f - _map.MarkerUv.y) * mapRect.height;
                GUI.color = TransparentEarthStyle.Signal;
                GUI.DrawTexture(new Rect(markerX - 10, markerY - 1, 20, 2), _white);
                GUI.DrawTexture(new Rect(markerX - 1, markerY - 10, 2, 20), _white);
                GUI.DrawTexture(new Rect(markerX - 4, markerY - 4, 8, 8), _white);
                GUI.color = Color.white;
            }
            else
            {
                GUI.DrawTexture(mapRect, _panel);
                GUI.Label(new Rect(mapRect.x, mapRect.center.y - 12, mapRect.width, 24),
                    string.IsNullOrEmpty(_map.Error)
                        ? AppText.Get(TextKey.LoadingMap)
                        : AppText.Get(TextKey.MapUnavailable), _small);
            }

            GUI.DrawTexture(new Rect(mapRect.x + 10, mapRect.y + 10, mapRect.width - 20, 58), _panel);
            GUI.color = TransparentEarthStyle.Signal;
            GUI.Label(new Rect(mapRect.x + 20, mapRect.y + 15, mapRect.width - 40, 18),
                AppText.Get(TextKey.ExactAntipode), _eyebrow);
            GUI.color = Color.white;
            GUI.Label(new Rect(mapRect.x + 20, mapRect.y + 31, mapRect.width - 40, 25),
                $"{antipode.Latitude:0.0000}°, {antipode.Longitude:0.0000}°", _title);
            GUI.Label(new Rect(mapRect.x + 8, mapRect.yMax - 24, mapRect.width - 16, 18),
                "© OpenStreetMap contributors", _small);
            var factsRect = new Rect(mapRect.x, mapRect.yMax + 12, mapRect.width, 58);
            GUI.DrawTexture(factsRect, _panel);
            GUI.Label(new Rect(factsRect.x + 14, factsRect.y + 9, factsRect.width - 28, 18),
                AppText.Get(TextKey.ThroughEarthDistance), _eyebrow);
            GUI.Label(new Rect(factsRect.x + 14, factsRect.y + 27, factsRect.width - 28, 22),
                $"{GeoMath.EarthDiameterKm:0} {AppText.Get(TextKey.Kilometers)}  ·  {AppText.Get(TextKey.FlagSaved)}", _small);

            var objectRect = new Rect(factsRect.x, factsRect.yMax + 10, factsRect.width, 112);
            GUI.DrawTexture(objectRect, _panel);
            GUI.color = TransparentEarthStyle.Mint;
            GUI.Label(new Rect(objectRect.x + 14, objectRect.y + 8, objectRect.width - 28, 18),
                AppText.Get(TextKey.NearestGeographicObject), _eyebrow);
            GUI.color = Color.white;
            GUI.Label(new Rect(objectRect.x + 14, objectRect.y + 28, objectRect.width - 88, 22),
                $"{PlaceNames.Get(nearest.Object.Name).ToUpperInvariant()} · {nearest.Object.Country}", _markerTitle);
            var bearing = nearest.Projection.BearingDegrees;
            GUI.Label(new Rect(objectRect.x + 14, objectRect.y + 53, objectRect.width - 92, 18),
                $"{AppText.Get(TextKey.Direction)}  {AppText.CardinalDirection(bearing)} · {bearing:000}°", _markerMeta);
            GUI.Label(new Rect(objectRect.x + 14, objectRect.y + 75, objectRect.width - 92, 18),
                $"{AppText.Get(TextKey.SurfaceDistance)}  {nearest.Projection.DistanceKm:0} {AppText.Get(TextKey.Kilometers)}",
                _markerMeta);
            DrawDirectionArrow(new Rect(objectRect.xMax - 68, objectRect.y + 32, 48, 48), (float)bearing);
            GUI.color = Color.white;
        }

        private GeographicObjectDirection NearestAntipodeObject(GeoPoint antipode)
        {
            if (!_hasAntipodeObject || GeoMath.DistanceKm(_resolvedAntipode, antipode) > 1d)
            {
                _resolvedAntipode = antipode;
                _nearestAntipodeObject = AntipodeResolver.FindNearestNamedObject(antipode);
                _hasAntipodeObject = true;
            }
            return _nearestAntipodeObject;
        }

        private void DrawDirectionArrow(Rect rect, float bearing)
        {
            var matrix = GUI.matrix;
            var guiScale = Mathf.Max(.01f, matrix.m00);
            var screenRect = new Rect(rect.x * guiScale, rect.y * guiScale,
                rect.width * guiScale, rect.height * guiScale);
            GUI.matrix = Matrix4x4.identity;
            GUIUtility.RotateAroundPivot(bearing, screenRect.center);
            GUI.color = TransparentEarthStyle.Signal;
            GUI.DrawTexture(screenRect, _directionArrow);
            GUI.matrix = matrix;
        }

        private void DrawPlaces(float left, float top, float width, float height)
        {
            var searchPanel = new Rect(left + 18f, top + 92f, width - 36f, 88f);
            GUI.color = Color.white;
            GUI.DrawTexture(searchPanel, _panel);
            GUI.color = TransparentEarthStyle.Mint;
            GUI.Label(new Rect(searchPanel.x + 14f, searchPanel.y + 7f, searchPanel.width - 28f, 18f),
                AppText.Get(TextKey.PlaceSearchHint), _eyebrow);

            var inputRect = new Rect(searchPanel.x + 14f, searchPanel.y + 31f, searchPanel.width - 112f, 40f);
            GUI.color = Color.white;
            _placeQuery = GUI.TextField(inputRect, _placeQuery, 80, _searchField);
            var searchRect = new Rect(searchPanel.xMax - 90f, searchPanel.y + 31f, 76f, 40f);
            GUI.DrawTexture(searchRect, _placeSearching ? _layerOff : _layerOn);
            GUI.Label(searchRect, _placeSearching ? "…" : AppText.Get(TextKey.Search), _buttonText);
            if (!_placeSearching && Clicked(searchRect) && !string.IsNullOrWhiteSpace(_placeQuery))
            {
                GUIUtility.keyboardControl = 0;
                StartCoroutine(SearchPlaces(_placeQuery.Trim()));
            }

            var contentTop = searchPanel.yMax + 12f;
            GUI.color = TransparentEarthStyle.Mint;
            GUI.Label(new Rect(left + 24f, contentTop, width - 48f, 20f),
                _placeResults.Count > 0
                    ? AppText.Get(TextKey.SearchResults)
                    : $"{AppText.Get(TextKey.SavedPlaces)} · {_streamer.CustomPlaces.Count}", _eyebrow);
            GUI.color = Color.white;

            if (_placeResults.Count > 0)
                DrawPlaceResults(left, contentTop + 25f, width, top + height - 66f);
            else if (_placeSearchStarted && !_placeSearching)
                GUI.Label(new Rect(left + 24f, contentTop + 36f, width - 48f, 24f),
                    string.IsNullOrEmpty(_placeSearchError)
                        ? AppText.Get(TextKey.NoPlacesFound)
                        : AppText.Get(TextKey.SearchUnavailable), _small);
            else
                DrawSavedPlaces(left, contentTop + 25f, width, top + height - 66f);

            GUI.color = Color.white;
            GUI.Label(new Rect(left + 20f, top + height - 88f, width - 40f, 18f),
                "© OpenStreetMap contributors · Nominatim", _small);
        }

        private void DrawPlaceResults(float left, float y, float width, float bottom)
        {
            const float rowHeight = 76f;
            foreach (var result in _placeResults)
            {
                if (y + rowHeight > bottom) break;
                var row = new Rect(left + 18f, y, width - 36f, rowHeight - 6f);
                GUI.color = Color.white;
                GUI.DrawTexture(row, _panel);
                GUI.Label(new Rect(row.x + 14f, row.y + 8f, row.width - 106f, 20f),
                    result.Name.ToUpperInvariant(), _markerTitle);
                GUI.Label(new Rect(row.x + 14f, row.y + 29f, row.width - 106f, 17f),
                    Shorten(result.DisplayName, 48), _markerMeta);
                GUI.Label(new Rect(row.x + 14f, row.y + 47f, row.width - 106f, 15f),
                    $"{result.Latitude:0.0000}°, {result.Longitude:0.0000}°", _small);

                var saved = _streamer.ContainsCustomPlace(result.Position);
                var addRect = new Rect(row.xMax - 88f, row.y + 17f, 74f, 36f);
                GUI.DrawTexture(addRect, saved ? _layerOff : _layerOn);
                GUI.Label(addRect, saved ? AppText.Get(TextKey.Added) : AppText.Get(TextKey.AddPlace), _buttonText);
                if (!saved && Clicked(addRect))
                    _streamer.AddCustomPlace(new City(result.Name, result.Country, result.Latitude, result.Longitude,
                        result.Importance));
                y += rowHeight;
            }
        }

        private void DrawSavedPlaces(float left, float y, float width, float bottom)
        {
            const float rowHeight = 54f;
            foreach (var city in _streamer.CustomPlaces)
            {
                if (y + rowHeight > bottom) break;
                var row = new Rect(left + 18f, y, width - 36f, rowHeight - 6f);
                GUI.color = Color.white;
                GUI.DrawTexture(row, _panel);
                GUI.Label(new Rect(row.x + 14f, row.y + 6f, row.width - 28f, 20f),
                    PlaceNames.Get(city.Name).ToUpperInvariant(), _markerTitle);
                GUI.Label(new Rect(row.x + 14f, row.y + 26f, row.width - 28f, 16f),
                    $"{city.Country} · {city.Position.Latitude:0.0000}°, {city.Position.Longitude:0.0000}°", _markerMeta);
                y += rowHeight;
            }
        }

        private IEnumerator SearchPlaces(string query)
        {
            _placeSearching = true;
            _placeSearchStarted = true;
            _placeSearchError = string.Empty;
            _placeResults.Clear();
            var language = SearchLanguage();
            var cacheKey = language + "|" + query;
            if (_placeSearchCache.TryGetValue(cacheKey, out var cached))
            {
                _placeResults.AddRange(cached);
                _placeSearching = false;
                yield break;
            }

            var delay = 1.05f - (Time.realtimeSinceStartup - _lastPlaceSearchAt);
            if (delay > 0f) yield return new WaitForSecondsRealtime(delay);
            _lastPlaceSearchAt = Time.realtimeSinceStartup;
            var endpoint = PlayerPrefs.GetString("OverHorizon.NominatimEndpoint",
                "https://nominatim.openstreetmap.org/search");
            var url = endpoint + "?format=jsonv2&addressdetails=1&limit=8&accept-language=" + language +
                      "&q=" + UnityWebRequest.EscapeURL(query);
            using var request = UnityWebRequest.Get(url);
            request.timeout = 18;
            request.SetRequestHeader("User-Agent", "OverHorizon/1.0 (Android; com.transparentearth.unity; place-search)");
            yield return request.SendWebRequest();
            if (request.result != UnityWebRequest.Result.Success)
            {
                _placeSearchError = request.error;
                _placeSearching = false;
                yield break;
            }

            try
            {
                var envelope = JsonUtility.FromJson<NominatimEnvelope>("{\"items\":" +
                    request.downloadHandler.text + "}");
                if (envelope?.items != null)
                {
                    foreach (var item in envelope.items)
                    {
                        if (!IsSettlement(item) || !double.TryParse(item.lat, NumberStyles.Float,
                                CultureInfo.InvariantCulture, out var latitude) ||
                            !double.TryParse(item.lon, NumberStyles.Float, CultureInfo.InvariantCulture,
                                out var longitude)) continue;
                        var name = string.IsNullOrWhiteSpace(item.name)
                            ? FirstAddressPart(item.display_name)
                            : item.name.Trim();
                        if (string.IsNullOrWhiteSpace(name)) continue;
                        var country = item.address?.country_code?.ToUpperInvariant() ?? string.Empty;
                        var result = new PlaceSearchResult(name, country, item.display_name ?? name,
                            latitude, longitude, ImportanceFor(item.type));
                        if (_placeResults.Exists(existing => GeoMath.DistanceKm(existing.Position, result.Position) < 1d))
                            continue;
                        _placeResults.Add(result);
                    }
                }
                _placeSearchCache[cacheKey] = _placeResults.ToArray();
            }
            catch (Exception exception)
            {
                _placeSearchError = exception.Message;
            }
            _placeSearching = false;
        }

        private static bool IsSettlement(NominatimItem item)
        {
            var type = string.IsNullOrWhiteSpace(item.addresstype) ? item.type : item.addresstype;
            return type is "city" or "town" or "village" or "hamlet" or "municipality" or "locality";
        }

        private static int ImportanceFor(string type) => type switch
        {
            "city" => 84,
            "town" => 74,
            "village" => 64,
            _ => 54
        };

        private static string SearchLanguage() => Application.systemLanguage switch
        {
            SystemLanguage.Russian => "ru",
            SystemLanguage.SerboCroatian => "sr-Latn",
            _ => "en"
        };

        private static string FirstAddressPart(string displayName)
        {
            if (string.IsNullOrWhiteSpace(displayName)) return string.Empty;
            var comma = displayName.IndexOf(',');
            return (comma < 0 ? displayName : displayName[..comma]).Trim();
        }

        private static string Shorten(string value, int maximum)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= maximum) return value;
            return value[..Mathf.Max(1, maximum - 1)].TrimEnd() + "…";
        }

        private void BottomDeck(float left, float top, float width, float height)
        {
            var navHeight = 58f;
            var deckHeight = _tab == 0 ? 142f : 0f;
            var deckY = top + height - navHeight - deckHeight;

            if (_tab == 0)
            {
                GUI.DrawTexture(new Rect(left + 12, deckY, width - 24, deckHeight - 8), _panel);
                GUI.Label(new Rect(left + 30, deckY + 15, 220, 22),
                    AppText.Get(TextKey.TransparentEarth), _markerTitle);
                GUI.Label(new Rect(left + 30, deckY + 35, 240, 18),
                    AppText.Get(TextKey.BeyondHorizon), _small);
                var toggleLabel = _transparentEarth ? AppText.Get(TextKey.On) : AppText.Get(TextKey.Off);
                var toggleRect = new Rect(left + width - 92, deckY + 15, 62, 32);
                GUI.DrawTexture(toggleRect, _transparentEarth ? _layerOn : _layerOff);
                GUI.Label(toggleRect, toggleLabel, _buttonText);
                if (Clicked(toggleRect)) _transparentEarth = !_transparentEarth;
                GUI.Label(new Rect(left + 30, deckY + 65, width - 60, 18),
                    AppText.Get(TextKey.GlobeLayers), _eyebrow);
                var gap = 5f;
                var layerWidth = (width - 60 - gap * 3) / 4f;
                var layerY = deckY + 88;
                if (LayerButton(new Rect(left + 30, layerY, layerWidth, 32), AppText.Get(TextKey.Grid), _earth.GridVisible))
                    _earth.SetGridVisible(!_earth.GridVisible);
                if (LayerButton(new Rect(left + 30 + layerWidth + gap, layerY, layerWidth, 32), AppText.Get(TextKey.Continents), _earth.ContinentsVisible))
                    _earth.SetContinentsVisible(!_earth.ContinentsVisible);
                if (LayerButton(new Rect(left + 30 + (layerWidth + gap) * 2, layerY, layerWidth, 32), AppText.Get(TextKey.Countries), _earth.CountriesVisible))
                    _earth.SetCountriesVisible(!_earth.CountriesVisible);
                if (LayerButton(new Rect(left + 30 + (layerWidth + gap) * 3, layerY, layerWidth, 32), AppText.Get(TextKey.References), _earth.ReferencesVisible))
                    _earth.SetReferencesVisible(!_earth.ReferencesVisible);
            }

            var navY = top + height - navHeight;
            var itemWidth = width / 3f;
            var names = new[]
            {
                AppText.Get(TextKey.Overview), AppText.Get(TextKey.Antipode), AppText.Get(TextKey.Places)
            };
            for (var i = 0; i < names.Length; i++)
            {
                GUI.color = i == _tab ? TransparentEarthStyle.Mint : TransparentEarthStyle.Muted;
                var navRect = new Rect(left + i * itemWidth, navY, itemWidth, navHeight);
                GUI.Label(navRect, names[i], _small);
                if (Clicked(navRect)) _tab = i;
            }
            GUI.color = Color.white;
        }

        private bool LayerButton(Rect rect, string label, bool active)
        {
            GUI.color = Color.white;
            GUI.DrawTexture(rect, active ? _layerOn : _layerOff);
            GUI.color = active ? Color.white : TransparentEarthStyle.Muted;
            GUI.Label(rect, label, _small);
            var pressed = Clicked(rect);
            GUI.color = Color.white;
            return pressed;
        }

        private void OrientationButton(float left, float top, float width)
        {
            var rect = new Rect(left + width - 76, top + 98, 54, 54);
            var manual = _earth.IsManuallyOriented;
            GUI.color = manual ? TransparentEarthStyle.Signal : Color.white;
            GUI.DrawTexture(rect, _compassButton);
            GUI.color = Color.white;
            var compassStyle = new GUIStyle(_markerTitle) { alignment = TextAnchor.MiddleCenter, fontSize = 15 };
            compassStyle.normal.textColor = manual ? TransparentEarthStyle.Signal : Color.white;
            var realStyle = new GUIStyle(_small);
            realStyle.normal.textColor = manual ? TransparentEarthStyle.Signal : TransparentEarthStyle.Muted;
            GUI.Label(new Rect(rect.x, rect.y + 5, rect.width, 22), "N", compassStyle);
            GUI.Label(new Rect(rect.x, rect.y + 27, rect.width, 16), AppText.Get(TextKey.Real), realStyle);
            if (Clicked(rect)) _earth.RestoreRealOrientation();
        }

        private void StatusPill(Rect rect)
        {
            GUI.DrawTexture(rect, _panel);
            GUI.DrawTexture(new Rect(rect.x + 9, rect.y + 11, 6, 6), _mint);
            GUI.Label(new Rect(rect.x + 21, rect.y + 5, 38, 20),
                _location.IsLive ? AppText.Get(TextKey.Live) : AppText.Get(TextKey.Demo), _eyebrow);
        }

        private void EnsureStyles()
        {
            if (_title != null) return;
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) return;
            _eyebrow = TextStyle(font, 10, FontStyle.Bold, TransparentEarthStyle.Mint, TextAnchor.MiddleLeft);
            _title = TextStyle(font, 21, FontStyle.Normal, Color.white, TextAnchor.MiddleLeft);
            _small = TextStyle(font, 9, FontStyle.Normal, TransparentEarthStyle.Muted, TextAnchor.MiddleCenter);
            _markerTitle = TextStyle(font, 11, FontStyle.Bold, Color.white, TextAnchor.MiddleLeft);
            _markerMeta = TextStyle(font, 8, FontStyle.Normal, Color.white, TextAnchor.MiddleLeft);
            _referenceLabel = TextStyle(font, 8, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
            _buttonText = TextStyle(font, 11, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
            _searchField = new GUIStyle(GUI.skin.textField)
            {
                font = font,
                fontSize = 13,
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(12, 12, 6, 6)
            };
            _searchField.normal.textColor = Color.white;
            _searchField.focused.textColor = Color.white;
        }

        private sealed class PlaceSearchResult
        {
            public readonly string Name;
            public readonly string Country;
            public readonly string DisplayName;
            public readonly double Latitude;
            public readonly double Longitude;
            public readonly int Importance;
            public GeoPoint Position => new(Latitude, Longitude);

            public PlaceSearchResult(string name, string country, string displayName, double latitude,
                double longitude, int importance)
            {
                Name = name;
                Country = country;
                DisplayName = displayName;
                Latitude = latitude;
                Longitude = longitude;
                Importance = importance;
            }
        }

        [Serializable]
        private sealed class NominatimEnvelope
        {
            public NominatimItem[] items;
        }

        [Serializable]
        private sealed class NominatimItem
        {
            public string lat;
            public string lon;
            public string name;
            public string display_name;
            public string type;
            public string addresstype;
            public NominatimAddress address;
        }

        [Serializable]
        private sealed class NominatimAddress
        {
            public string country_code;
        }

        private static GUIStyle TextStyle(Font font, int size, FontStyle fontStyle, Color color, TextAnchor alignment) =>
            new()
            {
                font = font,
                fontSize = size,
                fontStyle = fontStyle,
                alignment = alignment,
                normal = new GUIStyleState { textColor = color }
            };

        private static bool Clicked(Rect rect)
        {
            var current = Event.current;
            if (current == null || current.type != EventType.MouseUp || current.button != 0 || !rect.Contains(current.mousePosition))
                return false;
            current.Use();
            return true;
        }

        private static Texture2D Solid(Color color)
        {
            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }

        private static Texture2D Circle(Color color)
        {
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };
            var pixels = new Color[size * size];
            var center = new Vector2((size - 1) * .5f, (size - 1) * .5f);
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                var distance = Vector2.Distance(new Vector2(x, y), center);
                var edge = Mathf.InverseLerp(size * .5f, size * .42f, distance);
                var ring = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(size * .49f, size * .44f, distance))
                           - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(size * .44f, size * .39f, distance));
                pixels[y * size + x] = Color.Lerp(new Color(color.r, color.g, color.b, color.a * edge),
                    new Color(TransparentEarthStyle.Mint.r, TransparentEarthStyle.Mint.g, TransparentEarthStyle.Mint.b, .28f), ring);
            }
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        private static Texture2D Leader(bool pointsRight)
        {
            const int width = 44;
            const int height = 40;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Bilinear
            };
            var pixels = new Color32[width * height];
            for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
            {
                var expectedX = pointsRight
                    ? y * (width - 1f) / (height - 1f)
                    : (height - 1f - y) * (width - 1f) / (height - 1f);
                var distance = Mathf.Abs(x - expectedX);
                var alpha = (byte)Mathf.RoundToInt(Mathf.Clamp01(1.7f - distance) * 255f);
                pixels[y * width + x] = new Color32(255, 255, 255, alpha);
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
        }

        private static Texture2D DirectionArrow()
        {
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Bilinear
            };
            var pixels = new Color32[size * size];
            for (var y = 8; y < 54; y++) PaintPixel(pixels, size, size / 2, y, 2);
            for (var step = 0; step < 17; step++)
            {
                PaintPixel(pixels, size, size / 2 - step, 53 - step, 2);
                PaintPixel(pixels, size, size / 2 + step, 53 - step, 2);
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
        }

        private static void PaintPixel(Color32[] pixels, int size, int centerX, int centerY, int radius)
        {
            for (var y = Mathf.Max(0, centerY - radius); y <= Mathf.Min(size - 1, centerY + radius); y++)
            for (var x = Mathf.Max(0, centerX - radius); x <= Mathf.Min(size - 1, centerX + radius); x++)
                pixels[y * size + x] = new Color32(255, 255, 255, 255);
        }
    }
}
