using System;
using System.Collections.Generic;
using TransparentEarth.Geo;
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
        private GUIStyle _eyebrow;
        private GUIStyle _title;
        private GUIStyle _small;
        private GUIStyle _markerTitle;
        private GUIStyle _markerMeta;
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
            GUI.Label(new Rect(left + 20, top + 34, 330, 34), _tab == 0 ? "Смотрите сквозь горизонт" : "Другая сторона Земли", _title);
            StatusPill(new Rect(left + width - 82, top + 20, 62, 28));

            if (_tab == 0)
            {
                Metrics(left, top, width, height, scale);
                DrawMarkers(scale);
                OrientationButton(left, top, width);
            }
            else DrawAntipode(left, top, width, height);

            BottomDeck(left, top, width, height);
        }

        private void Metrics(float left, float top, float width, float height, float scale)
        {
            var viewPitch = _pose.PitchDegrees + _earth.ManualLookPitchDegrees;
            var text = $"AZ  {_pose.HeadingDegrees:000}°     TILT  {viewPitch:+0;-0;0}°     GPS  ±{_location.AccuracyMeters:0} m     ZONES  {_streamer.LoadedZoneCount}";
            GUI.Label(new Rect(left + width / 2 - 150, top + 76, 300, 24), text, _small);
            var horizon = _camera.WorldToScreenPoint(_earth.HorizonWorldPoint);
            if (horizon.z > 0)
            {
                var horizonY = (Screen.height - horizon.y) / scale;
                GUI.Label(new Rect(left + 20, horizonY - 17, 180, 18), "—  ФИЗИЧЕСКИЙ ГОРИЗОНТ", _small);
            }
        }

        private void DrawMarkers(float scale)
        {
            foreach (var marker in _markers)
            {
                if (!_transparentEarth && marker.Projection.ElevationDegrees < -1) continue;
                var viewport = _camera.WorldToScreenPoint(marker.Anchor.position);
                if (viewport.z <= 0 || viewport.x < -100 || viewport.x > Screen.width + 100 ||
                    viewport.y < Screen.height * .2f || viewport.y > Screen.height * .82f) continue;
                var x = viewport.x / scale;
                var y = (Screen.height - viewport.y) / scale;
                var rect = new Rect(x + 8, y - 22, 160, 46);
                GUI.DrawTexture(rect, _panel);
                GUI.color = marker.Accent;
                GUI.DrawTexture(new Rect(x - 3, y - 3, 7, 7), _white);
                GUI.color = Color.white;
                GUI.Label(new Rect(rect.x + 9, rect.y + 5, rect.width - 12, 18), marker.City.Name.ToUpperInvariant(), _markerTitle);
                var depth = marker.Projection.ElevationDegrees < -.1
                    ? $"{Math.Abs(marker.Projection.ElevationDegrees):0.0}° НИЖЕ"
                    : "НА ГОРИЗОНТЕ";
                GUI.Label(new Rect(rect.x + 9, rect.y + 23, rect.width - 12, 18), $"{marker.Projection.DistanceKm:0} KM  ·  {depth}", _markerMeta);
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
                    string.IsNullOrEmpty(_map.Error) ? "ЗАГРУЗКА КАРТЫ…" : "КАРТА НЕДОСТУПНА", _small);
            }

            GUI.DrawTexture(new Rect(mapRect.x + 10, mapRect.y + 10, mapRect.width - 20, 58), _panel);
            GUI.color = TransparentEarthStyle.Signal;
            GUI.Label(new Rect(mapRect.x + 20, mapRect.y + 15, mapRect.width - 40, 18), "ТОЧНЫЙ АНТИПОД", _eyebrow);
            GUI.color = Color.white;
            GUI.Label(new Rect(mapRect.x + 20, mapRect.y + 31, mapRect.width - 40, 25),
                $"{antipode.Latitude:0.0000}°, {antipode.Longitude:0.0000}°", _title);
            GUI.Label(new Rect(mapRect.x + 8, mapRect.yMax - 24, mapRect.width - 16, 18),
                "© OpenStreetMap contributors", _small);
            var factsRect = new Rect(mapRect.x, mapRect.yMax + 12, mapRect.width, 58);
            GUI.DrawTexture(factsRect, _panel);
            GUI.Label(new Rect(factsRect.x + 14, factsRect.y + 9, factsRect.width - 28, 18),
                "РАССТОЯНИЕ СКВОЗЬ ЗЕМЛЮ", _eyebrow);
            GUI.Label(new Rect(factsRect.x + 14, factsRect.y + 27, factsRect.width - 28, 22),
                $"{GeoMath.HalfCircumferenceKm:0} км  ·  флажок сохранён на сфере", _small);
        }

        private void BottomDeck(float left, float top, float width, float height)
        {
            var navHeight = 58f;
            var deckHeight = _tab == 0 ? 142f : 0f;
            var deckY = top + height - navHeight - deckHeight;

            if (_tab == 0)
            {
                GUI.DrawTexture(new Rect(left + 12, deckY, width - 24, deckHeight - 8), _panel);
                GUI.Label(new Rect(left + 30, deckY + 15, 220, 22), "Прозрачная Земля", _markerTitle);
                GUI.Label(new Rect(left + 30, deckY + 35, 240, 18), "Объекты за линией горизонта", _small);
                var toggleLabel = _transparentEarth ? "ВКЛ" : "ВЫКЛ";
                var toggleRect = new Rect(left + width - 92, deckY + 15, 62, 32);
                GUI.DrawTexture(toggleRect, _transparentEarth ? _layerOn : _layerOff);
                GUI.Label(toggleRect, toggleLabel, _buttonText);
                if (Clicked(toggleRect)) _transparentEarth = !_transparentEarth;
                GUI.Label(new Rect(left + 30, deckY + 65, width - 60, 18), "СЛОИ ГЛОБУСА", _eyebrow);
                var gap = 6f;
                var layerWidth = (width - 60 - gap * 2) / 3f;
                var layerY = deckY + 88;
                if (LayerButton(new Rect(left + 30, layerY, layerWidth, 32), "СЕТКА", _earth.GridVisible))
                    _earth.SetGridVisible(!_earth.GridVisible);
                if (LayerButton(new Rect(left + 30 + layerWidth + gap, layerY, layerWidth, 32), "МАТЕРИКИ", _earth.ContinentsVisible))
                    _earth.SetContinentsVisible(!_earth.ContinentsVisible);
                if (LayerButton(new Rect(left + 30 + (layerWidth + gap) * 2, layerY, layerWidth, 32), "СТРАНЫ", _earth.CountriesVisible))
                    _earth.SetCountriesVisible(!_earth.CountriesVisible);
            }

            var navY = top + height - navHeight;
            var itemWidth = width / 5f;
            var names = new[] { "ОБЗОР", "АНТИПОД", "КАРТА", "МЕСТА", "ПРОФИЛЬ" };
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
            GUI.Label(new Rect(rect.x, rect.y + 27, rect.width, 16), "REAL", realStyle);
            if (Clicked(rect)) _earth.RestoreRealOrientation();
        }

        private void StatusPill(Rect rect)
        {
            GUI.DrawTexture(rect, _panel);
            GUI.DrawTexture(new Rect(rect.x + 9, rect.y + 11, 6, 6), _mint);
            GUI.Label(new Rect(rect.x + 21, rect.y + 5, 38, 20), _location.IsLive ? "LIVE" : "DEMO", _eyebrow);
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
            _markerMeta = TextStyle(font, 8, FontStyle.Normal, TransparentEarthStyle.Mint, TextAnchor.MiddleLeft);
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
    }
}
