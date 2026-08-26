using System;
using System.Collections.Generic;
using TransparentEarth.Geo;
using TransparentEarth.I18n;
using TransparentEarth.Map;
using TransparentEarth.Rendering;
using TransparentEarth.Sensors;
using UnityEngine;

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
        private GUIStyle _eyebrow;
        private GUIStyle _title;
        private GUIStyle _small;
        private GUIStyle _markerTitle;
        private GUIStyle _markerMeta;
        private GUIStyle _referenceLabel;
        private GUIStyle _buttonText;
        private bool _transparentEarth = true;
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

            GUI.Label(new Rect(left + 20, top + 16, 260, 18), "TRANSPARENT EARTH", _eyebrow);
            GUI.Label(new Rect(left + 20, top + 34, 330, 34),
                _tab == 0 ? AppText.Get(TextKey.LookThroughHorizon) : AppText.Get(TextKey.OtherSideOfEarth), _title);
            StatusPill(new Rect(left + width - 82, top + 20, 62, 28));

            if (_tab == 0)
            {
                Metrics(left, top, width, height, scale);
                DrawMarkers(scale, left, width);
                DrawReferenceLegends(scale);
                OrientationButton(left, top, width);
            }
            else DrawAntipode(left, top, width, height);

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
            foreach (var marker in _markers)
            {
                if (!_transparentEarth && marker.Projection.ElevationDegrees < -1) continue;
                var viewport = _camera.WorldToScreenPoint(marker.Anchor.position);
                if (viewport.z <= 0 || viewport.x < -100 || viewport.x > Screen.width + 100 ||
                    viewport.y < Screen.height * .2f || viewport.y > Screen.height * .82f) continue;
                var x = viewport.x / scale;
                var y = (Screen.height - viewport.y) / scale;
                var pointsRight = x < left + width * .63f;
                var horizonProximity = horizonScreen.z > 0f
                    ? 1f - Mathf.Clamp01(Mathf.Abs(viewport.y - horizonScreen.y) / (Screen.height * .14f))
                    : 0f;
                var labelLift = Mathf.SmoothStep(0f, 26f, horizonProximity);
                var underlineY = y - 20f - labelLift;
                var pulse = _earth.ScanPulseAt((float)marker.Projection.CentralAngleDegrees);
                var reveal = marker.AccumulateReveal(
                    _earth.MarkerRevealAt((float)marker.Projection.CentralAngleDegrees));
                if (reveal < .015f) continue;
                var accent = marker.Accent;
                accent.a = reveal * (.76f + pulse * .24f);
                GUI.color = accent;
                if (marker.HasLeaderLine)
                {
                    var leaderX = pointsRight ? x : x - 22f;
                    GUI.DrawTexture(new Rect(leaderX, underlineY, 22f, 20f + labelLift),
                        pointsRight ? _leaderRight : _leaderLeft);
                }
                var underlineX = marker.HasLeaderLine
                    ? pointsRight ? x + 21f : x - 139f
                    : pointsRight ? x + 9f : x - 127f;
                var textX = underlineX + 2f;
                if (horizonProximity > 0f)
                {
                    GUI.color = new Color(1f, 1f, 1f, Mathf.Lerp(.42f, .88f, horizonProximity));
                    GUI.DrawTexture(new Rect(textX - 5f, y - 40f - labelLift, 158f, 39f), _panel);
                    GUI.color = accent;
                }
                GUI.DrawTexture(new Rect(underlineX, underlineY, 118f, 1.25f + pulse * 1.1f), _white);
                GUI.DrawTexture(new Rect(x - 3f - pulse, y - 3f - pulse, 6f + pulse * 2f, 6f + pulse * 2f), _white);
                var titleStyle = marker.IsNearby ? _markerMeta : _markerTitle;
                GUI.Label(new Rect(textX, y - 38f - labelLift, 154f, 18f),
                    marker.City.Name.ToUpperInvariant(), titleStyle);
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

        private void DrawAntipode(float left, float top, float width, float height)
        {
            var antipode = GeoMath.Antipode(_location.Current);
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
                GUI.DrawTexture(new Rect(markerX - 7, markerY - 7, 14, 14), _white);
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
                $"{GeoMath.HalfCircumferenceKm:0} {AppText.Get(TextKey.Kilometers)}  ·  {AppText.Get(TextKey.FlagSaved)}", _small);
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
            var itemWidth = width / 5f;
            var names = new[]
            {
                AppText.Get(TextKey.Overview), AppText.Get(TextKey.Antipode), AppText.Get(TextKey.Map),
                AppText.Get(TextKey.Places), AppText.Get(TextKey.Profile)
            };
            for (var i = 0; i < names.Length; i++)
            {
                GUI.color = i == _tab ? TransparentEarthStyle.Mint : TransparentEarthStyle.Muted;
                var navRect = new Rect(left + i * itemWidth, navY, itemWidth, navHeight);
                GUI.Label(navRect, names[i], _small);
                if (Clicked(navRect) && i < 2) _tab = i;
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
    }
}
