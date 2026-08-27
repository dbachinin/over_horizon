using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TransparentEarth.Data;
using TransparentEarth.Geo;
using TransparentEarth.I18n;
using TransparentEarth.Rendering;
using TransparentEarth.Sensors;
using TransparentEarth.Store;
using UnityEngine;
using UnityEngine.Networking;

namespace TransparentEarth.UI
{
    /// <summary>
    /// The paid "Flat Earther" mode. It replaces the transparent globe with a medieval-miniature
    /// tableau: a north-polar azimuthal map (the coordinate distortion), the South Pole stretched
    /// around the rim, every city dropped onto one horizon line with only a sub-degree relief
    /// wobble, and a bearing readout to each city.
    /// </summary>
    public sealed class FlatEarthScreen : MonoBehaviour
    {
        private LocationProvider _location;
        private DevicePoseProvider _pose;
        private GeoObjectStreamer _streamer;
        private EarthRenderer _earth;

        private Texture2D _parchment;
        private Texture2D _plate;
        private Texture2D _sun;
        private Texture2D _moon;
        private Texture2D _rose;
        private Texture2D _arse;
        private Texture2D _white;
        private Texture2D _dot;
        private Texture2D _ring;
        private Texture2D _card;
        private Texture2D _wedge;
        private readonly Texture2D[] _winds = new Texture2D[4];

        private GUIStyle _eyebrow;
        private GUIStyle _title;
        private GUIStyle _body;
        private GUIStyle _small;
        private GUIStyle _rowTitle;
        private GUIStyle _button;
        private GUIStyle _backLink;
        private GUIStyle _creed;
        private GUIStyle _search;

        private bool _open;
        private bool _cartographyRequested;
        private string _selectedKey;
        private string _query = string.Empty;
        private Vector2 _scroll;
        private bool _dragging;
        private float _lastDragY;
        private float _dragTravel;
        private float _mapZoom = 1f;
        private Vector2 _mapPan;
        private float _discSize = 300f;
        private float _gazeAngle;
        private bool _mapDragging;
        private Vector2 _mapLastDrag;
        private float _mapDragTravel;
        private readonly List<Entry> _entries = new();
        private readonly List<Entry> _filtered = new();
        private GeoPoint _entriesFor;
        private int _entriesCustomCount = -1;
        private bool _entriesReady;

        public bool IsOpen => _open;

        public void Initialize(LocationProvider location, DevicePoseProvider pose,
            GeoObjectStreamer streamer, EarthRenderer earth)
        {
            _location = location;
            _pose = pose;
            _streamer = streamer;
            _earth = earth;
        }

        public void Toggle()
        {
            if (_open) Close();
            else Open();
        }

        public void Open()
        {
            _open = true;
            _mapZoom = 1f;
            _mapPan = Vector2.zero;
            _earth?.SetInteractionEnabled(false);
            _earth?.SetWorldRenderingEnabled(false);
            if (!_cartographyRequested)
            {
                _cartographyRequested = true;
                StartCoroutine(LoadCartography());
            }
        }

        public void Close()
        {
            _open = false;
            _earth?.SetInteractionEnabled(true);
            _earth?.SetWorldRenderingEnabled(true);
        }

        private void Update()
        {
            if (!_open) return;

            if (_location != null)
            {
                var heading = _pose != null ? _pose.HeadingDegrees : 0f;
                var obs = FlatEarthProjection.DiscPoint(_location.Current);
                var ahead = FlatEarthProjection.DiscPoint(
                    GeoMath.Destination(_location.Current, heading, 900d));
                var v = new Vector2(ahead.x - obs.x, -(ahead.y - obs.y));
                if (v.sqrMagnitude > 1e-8f)
                {
                    var target = Mathf.Atan2(v.x, -v.y) * Mathf.Rad2Deg;
                    _gazeAngle = Mathf.LerpAngle(_gazeAngle, target,
                        1f - Mathf.Exp(-7f * Time.deltaTime));
                }
            }

            if (Input.touchCount != 2) return;
            var scale = Mathf.Max(.85f, Screen.width / 430f);
            var a = Input.GetTouch(0);
            var b = Input.GetTouch(1);
            var previous = ((a.position - a.deltaPosition) - (b.position - b.deltaPosition)).magnitude;
            var current = (a.position - b.position).magnitude;
            if (previous > 1f && current > 1f) SetZoom(_mapZoom * (current / previous));
            var pan = (a.deltaPosition + b.deltaPosition) * (.5f / scale);
            _mapPan += new Vector2(pan.x, -pan.y);
            ClampPan();
        }

        private void SetZoom(float value)
        {
            _mapZoom = Mathf.Clamp(value, 1f, 6f);
            if (_mapZoom <= 1.001f)
            {
                _mapZoom = 1f;
                _mapPan = Vector2.zero;
            }
            ClampPan();
        }

        private void ClampPan()
        {
            var limit = _discSize * (_mapZoom * .5f + .35f);
            _mapPan.x = Mathf.Clamp(_mapPan.x, -limit, limit);
            _mapPan.y = Mathf.Clamp(_mapPan.y, -limit, limit);
        }

        private void HandleMapPan(Rect rect)
        {
            if (Input.touchCount >= 2)
            {
                _mapDragging = false;
                return;
            }
            var e = Event.current;
            switch (e.type)
            {
                case EventType.MouseDown when rect.Contains(e.mousePosition):
                    _mapDragging = true;
                    _mapLastDrag = e.mousePosition;
                    _mapDragTravel = 0f;
                    break;
                case EventType.MouseDrag when _mapDragging:
                {
                    var delta = e.mousePosition - _mapLastDrag;
                    _mapLastDrag = e.mousePosition;
                    _mapDragTravel += delta.magnitude;
                    _mapPan += delta;
                    ClampPan();
                    e.Use();
                    break;
                }
                case EventType.MouseUp:
                    if (_mapDragging && _mapDragTravel > 8f) e.Use();
                    _mapDragging = false;
                    break;
            }
        }

        private void Awake()
        {
            _parchment = MedievalIconography.ParchmentSheet(160);
            _plate = MedievalIconography.FlatEarthPlate(768);
            _sun = MedievalIconography.SunFace(112);
            _moon = MedievalIconography.MoonFace(112);
            _rose = MedievalIconography.CompassRose(120);
            _arse = Resources.Load<Texture2D>("FlatEarthEntry") is { } entryIcon
                ? entryIcon
                : MedievalIconography.ArseHorn(120);
            for (var i = 0; i < 4; i++)
                _winds[i] = MedievalIconography.WindHead(96, 45f + i * 90f);
            _white = Solid(Color.white);
            _dot = Disc(20, Color.white);
            _ring = Ring(28, Color.white);
            _card = Solid(new Color(.83f, .73f, .52f, .96f));
            _wedge = WedgeTexture(96);
        }

        private void OnGUI()
        {
            if (!_open || _location == null) return;
            EnsureStyles();
            if (_title == null) return;

            var scale = Mathf.Max(.85f, Screen.width / 430f);
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1f));
            var safe = Screen.safeArea;
            var width = safe.width / scale;
            var height = safe.height / scale;
            var left = safe.x / scale;
            var top = (Screen.height - safe.yMax) / scale;

            GUI.color = Color.white;
            TileParchment(new Rect(0f, 0f, Screen.width / scale, Screen.height / scale));

            if (!FlatEarthEntitlement.IsUnlocked)
            {
                DrawHeader(left, top, width);
                DrawPaywall(left, top, width, height);
                DrawCloseButton(left, top, width);
                return;
            }

            RefreshEntries();
            ApplyFilter();

            var discSize = Mathf.Min(width - 18f, height * .35f);
            var discRect = new Rect(left + (width - discSize) * .5f, top + 74f, discSize, discSize);
            DrawDisc(discRect);

            var stripRect = new Rect(left + 14f, discRect.yMax + 8f, width - 28f, 82f);
            DrawHorizonStrip(stripRect);

            var readoutRect = new Rect(left + 14f, stripRect.yMax + 6f, width - 28f, 44f);
            DrawSelectedReadout(readoutRect);

            var searchRect = new Rect(left + 14f, readoutRect.yMax + 6f, width - 28f, 32f);
            DrawSearchField(searchRect);

            var listRect = new Rect(left + 14f, searchRect.yMax + 6f, width - 28f,
                top + height - searchRect.yMax - 14f);
            DrawCityList(listRect);

            // Header last so the disc's masked overflow never sits on top of it.
            DrawHeader(left, top, width);
            GUI.color = MedievalIconography.Blood;
            GUI.Label(new Rect(left + 96f, top + 40f, width - 108f, 30f),
                AppText.Get(TextKey.SecretInitiate), _creed);
            GUI.color = Color.white;
            DrawCloseButton(left, top, width);
        }

        private void DrawHeader(float left, float top, float width)
        {
            GUI.color = Color.white;
            GUI.DrawTexture(new Rect(left + 6, top + 2, 44, 44), _sun, ScaleMode.ScaleToFit);
            GUI.DrawTexture(new Rect(left + width - 50, top + 2, 44, 44), _moon, ScaleMode.ScaleToFit);
            GUI.Label(new Rect(left + 54, top + 3, width - 108, 13),
                AppText.Get(TextKey.FlatEarthMode), _eyebrow);
            GUI.Label(new Rect(left + 54, top + 15, width - 108, 22),
                AppText.Get(TextKey.FlatEarthTitle), _title);
        }

        private void DrawDisc(Rect rect)
        {
            _discSize = rect.width;
            var ev = Event.current;
            if (ev.type == EventType.ScrollWheel && rect.Contains(ev.mousePosition))
            {
                SetZoom(_mapZoom * (1f - ev.delta.y * .06f));
                ev.Use();
            }

            HandleMapPan(rect);

            var radius = rect.width * .46f * _mapZoom;
            var theta = _gazeAngle; // rotate the whole map by -theta so the gaze points up

            // The disc stays centred in its frame (never clipped at the top); the map turns
            // around the North Pole and the observer orbits with it.
            var mapCenter = rect.center + _mapPan;
            var obsDisc = FlatEarthProjection.DiscPoint(_location.Current);

            Vector2 ToScreen(Vector2 disc) =>
                mapCenter + Rotate(new Vector2(disc.x * radius, -disc.y * radius), -theta);

            // The four winds sit behind the disc, tucked under its rim.
            var frameCenter = rect.center;
            var frameRadius = rect.width * .46f;
            GUI.color = Color.white;
            foreach (var wind in Enumerable.Range(0, 4))
            {
                var wa = (45f + wind * 90f) * Mathf.Deg2Rad;
                var wp = frameCenter + new Vector2(Mathf.Cos(wa), -Mathf.Sin(wa)) * (frameRadius - 10f);
                GUI.DrawTexture(new Rect(wp.x - 26f, wp.y - 26f, 52f, 52f), _winds[wind], ScaleMode.ScaleToFit);
            }

            var savedMatrix = GUI.matrix;
            var scale = Mathf.Max(.85f, Screen.width / 430f);
            var pivotScreen = mapCenter * scale;
            GUI.matrix = Matrix4x4.TRS(pivotScreen, Quaternion.Euler(0f, 0f, -theta), Vector3.one)
                         * Matrix4x4.TRS(-pivotScreen, Quaternion.identity, Vector3.one)
                         * savedMatrix;
            GUI.color = Color.white;
            var d = rect.width * _mapZoom;
            GUI.DrawTexture(new Rect(mapCenter.x - d * .5f, mapCenter.y - d * .5f, d, d),
                _plate, ScaleMode.ScaleToFit);
            GUI.matrix = savedMatrix;

            var observer = ToScreen(obsDisc);
            Entry? selected = null;
            foreach (var entry in _entries)
            {
                var p = ToScreen(entry.Placement.Disc);
                var isSelected = entry.Key == _selectedKey;
                if (isSelected) selected = entry;
                if (!rect.Contains(p)) continue;
                var size = isSelected ? 9f : entry.City.Importance >= 90 ? 6f : 4.5f;
                GUI.color = isSelected ? MedievalIconography.Blood : MedievalIconography.Ink;
                GUI.DrawTexture(new Rect(p.x - size * .5f, p.y - size * .5f, size, size), _dot);
                if (_mapDragTravel < 8f && Clicked(new Rect(p.x - 12f, p.y - 12f, 24f, 24f))) Select(entry.Key);
            }

            if (selected.HasValue)
            {
                var target = ToScreen(selected.Value.Placement.Disc);
                DrawLine(observer, target, 2.4f, MedievalIconography.Blood);
                GUI.color = MedievalIconography.Gilt;
                GUI.DrawTexture(new Rect(target.x - 14f, target.y - 14f, 28f, 28f), _ring);
            }

            var pole = ToScreen(Vector2.zero);
            GUI.color = Color.white;
            if (rect.Contains(pole))
                GUI.Label(new Rect(pole.x - 70f, pole.y + 7f, 140f, 12f),
                    AppText.Get(TextKey.NorthPoleHub), _small);

            MaskAround(rect);
            DrawGazeWedge(mapCenter, rect);

            if (rect.Contains(observer))
            {
                GUI.color = MedievalIconography.Gilt;
                GUI.DrawTexture(new Rect(observer.x - 5f, observer.y - 5f, 10f, 10f), _dot);
                GUI.color = MedievalIconography.Ink;
                GUI.DrawTexture(new Rect(observer.x - 9f, observer.y - 9f, 18f, 18f), _ring);
            }

            GUI.color = Color.white;
            GUI.Label(new Rect(rect.x, rect.yMax - 13f, rect.width, 14f),
                AppText.Get(TextKey.IceWall), _small);

            if (_mapZoom > 1.02f)
            {
                var home = new Rect(rect.xMax - 40f, rect.y + 4f, 36f, 24f);
                GUI.color = MedievalIconography.Gilt;
                GUI.DrawTexture(home, _white);
                GUI.color = new Color(.16f, .10f, .05f);
                GUI.Label(home, "1:1", _small);
                GUI.color = Color.white;
                if (Clicked(home))
                {
                    _mapZoom = 1f;
                    _mapPan = Vector2.zero;
                }
            }
        }

        private void DrawGazeWedge(Vector2 apex, Rect rect)
        {
            var length = (apex.y - rect.y) - 6f;
            if (length < 20f) return;
            var halfWidth = length * .34f;
            var tint = MedievalIconography.Gilt;
            tint.a = .9f;
            GUI.color = new Color(tint.r, tint.g, tint.b, .22f);
            GUI.DrawTexture(new Rect(apex.x - halfWidth, apex.y - length, halfWidth * 2f, length), _wedge);
            GUI.color = tint;
            DrawLine(apex, new Vector2(apex.x - halfWidth, apex.y - length), 1.6f, tint);
            DrawLine(apex, new Vector2(apex.x + halfWidth, apex.y - length), 1.6f, tint);
            GUI.color = Color.white;
            GUI.Label(new Rect(apex.x - 60f, apex.y - length - 2f, 120f, 12f),
                AppText.Get(TextKey.Gaze), _small);
        }

        private void MaskAround(Rect hole)
        {
            var scale = Mathf.Max(.85f, Screen.width / 430f);
            var w = Screen.width / scale;
            var h = Screen.height / scale;
            TileParchment(new Rect(0f, 0f, w, hole.y));
            TileParchment(new Rect(0f, hole.yMax, w, h - hole.yMax));
            TileParchment(new Rect(0f, hole.y, hole.x, hole.height));
            TileParchment(new Rect(hole.xMax, hole.y, w - hole.xMax, hole.height));
        }

        private static Vector2 Rotate(Vector2 v, float degrees)
        {
            var r = degrees * Mathf.Deg2Rad;
            var c = Mathf.Cos(r);
            var s = Mathf.Sin(r);
            return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c);
        }

        private void DrawHorizonStrip(Rect rect)
        {
            GUI.color = new Color(1f, 1f, 1f, .35f);
            GUI.DrawTexture(rect, _card);
            GUI.color = Color.white;
            GUI.Label(new Rect(rect.x + 8f, rect.y + 4f, rect.width - 16f, 14f),
                AppText.Get(TextKey.OneHorizonLine), _small);

            var baseline = rect.y + rect.height * .62f;
            const float pixelsPerDegree = 20f;
            GUI.color = MedievalIconography.Ink;
            GUI.DrawTexture(new Rect(rect.x + 6f, baseline - 1f, rect.width - 12f, 2f), _white);

            var heading = _pose != null ? _pose.HeadingDegrees : 0f;
            var half = (rect.width - 24f) * .5f;
            foreach (var entry in _entries)
            {
                var relative = Mathf.DeltaAngle(heading, (float)entry.Placement.AzimuthDegrees);
                if (Mathf.Abs(relative) > 96f) continue;
                var x = rect.center.x + relative / 96f * half;
                var y = baseline - (float)entry.Placement.ElevationDegrees * pixelsPerDegree;
                var isSelected = entry.Key == _selectedKey;
                GUI.color = isSelected ? MedievalIconography.Blood : MedievalIconography.Ink;
                GUI.DrawTexture(new Rect(x - .75f, Mathf.Min(y, baseline), 1.5f, Mathf.Abs(y - baseline) + 1f), _white);
                var dot = isSelected ? 7f : 4f;
                GUI.DrawTexture(new Rect(x - dot * .5f, y - dot * .5f, dot, dot), _dot);
                if (isSelected)
                    GUI.Label(new Rect(x - 60f, y - 30f, 120f, 26f),
                        $"{PlaceNames.Get(entry.City.Name).ToUpperInvariant()}\n{AppText.Get(TextKey.Azimuth)} {entry.Placement.AzimuthDegrees:000}°",
                        _small);
                var hit = new Rect(x - 10f, rect.y, 20f, rect.height);
                if (Clicked(hit)) Select(entry.Key);
            }

            GUI.color = MedievalIconography.Blood;
            GUI.DrawTexture(new Rect(rect.center.x - 1f, rect.y + 2f, 2f, rect.height - 4f), _white);
            GUI.color = Color.white;
        }

        private void DrawSelectedReadout(Rect rect)
        {
            GUI.color = new Color(1f, 1f, 1f, .5f);
            GUI.DrawTexture(rect, _card);
            GUI.color = Color.white;
            var entry = _entries.FirstOrDefault(e => e.Key == _selectedKey);
            if (entry.City.Name == null)
            {
                GUI.Label(rect, AppText.Get(TextKey.WhereToLook), _body);
                return;
            }

            var azimuth = entry.Placement.AzimuthDegrees;
            GUI.DrawTexture(new Rect(rect.x + 6f, rect.y + 5f, 36f, 36f), _rose, ScaleMode.ScaleToFit);
            GUI.Label(new Rect(rect.x + 50f, rect.y + 5f, rect.width - 60f, 18f),
                PlaceNames.Get(entry.City.Name).ToUpperInvariant(), _rowTitle);
            GUI.Label(new Rect(rect.x + 50f, rect.y + 24f, rect.width - 60f, 16f),
                $"{AppText.Get(TextKey.Azimuth)} {azimuth:000}° · {AppText.CardinalDirection(azimuth)} · " +
                $"{entry.Placement.DistanceKm:0} {AppText.Get(TextKey.Kilometers)}", _small);
        }

        private void DrawSearchField(Rect rect)
        {
            var hasQuery = !string.IsNullOrEmpty(_query);
            GUI.color = new Color(1f, 1f, 1f, .55f);
            GUI.DrawTexture(rect, _card);
            GUI.color = Color.white;

            var fieldWidth = hasQuery ? rect.width - 52f : rect.width - 12f;
            GUI.SetNextControlName("FlatEarthCitySearch");
            var typed = GUI.TextField(new Rect(rect.x + 8f, rect.y + 3f, fieldWidth, rect.height - 6f),
                _query, 40, _search);
            if (typed != _query)
            {
                _query = typed;
                _scroll.y = 0f;
            }
            if (!hasQuery && GUI.GetNameOfFocusedControl() != "FlatEarthCitySearch")
            {
                GUI.color = new Color(MedievalIconography.Ink.r, MedievalIconography.Ink.g,
                    MedievalIconography.Ink.b, .45f);
                GUI.Label(new Rect(rect.x + 12f, rect.y, rect.width - 24f, rect.height),
                    AppText.Get(TextKey.PlaceSearchHint), _search);
                GUI.color = Color.white;
            }
            if (hasQuery)
            {
                var clear = new Rect(rect.xMax - 44f, rect.y + 3f, 38f, rect.height - 6f);
                GUI.color = MedievalIconography.Gilt;
                GUI.DrawTexture(clear, _white);
                GUI.color = Color.white;
                GUI.Label(clear, "×", _button);
                if (Clicked(clear))
                {
                    _query = string.Empty;
                    GUIUtility.keyboardControl = 0;
                    _scroll.y = 0f;
                }
            }
        }

        private void DrawCityList(Rect rect)
        {
            if (_filtered.Count == 0)
            {
                GUI.Label(rect, AppText.Get(TextKey.NoPlacesFound), _body);
                return;
            }

            const float rowHeight = 40f;
            var inner = new Rect(0f, 0f, rect.width - 16f, _filtered.Count * rowHeight);
            HandleSwipe(rect, inner.height);

            _scroll = GUI.BeginScrollView(rect, _scroll, inner);
            var y = 0f;
            foreach (var entry in _filtered)
            {
                var row = new Rect(0f, y, inner.width, rowHeight - 4f);
                y += rowHeight;
                if (row.yMax < _scroll.y || row.y > _scroll.y + rect.height) continue;
                var isSelected = entry.Key == _selectedKey;
                GUI.color = new Color(1f, 1f, 1f, isSelected ? .6f : .28f);
                GUI.DrawTexture(row, _card);
                GUI.color = Color.white;
                GUI.Label(new Rect(row.x + 10f, row.y + 3f, row.width - 120f, 18f),
                    PlaceNames.Get(entry.City.Name).ToUpperInvariant(), _rowTitle);
                GUI.Label(new Rect(row.x + 10f, row.y + 20f, row.width - 120f, 14f),
                    $"{entry.City.Country} · {entry.Placement.DistanceKm:0} {AppText.Get(TextKey.Kilometers)}", _small);
                GUI.Label(new Rect(row.xMax - 110f, row.y + 3f, 104f, 18f),
                    $"{AppText.Get(TextKey.Azimuth)} {entry.Placement.AzimuthDegrees:000}°", _rowTitle);
                GUI.Label(new Rect(row.xMax - 110f, row.y + 20f, 104f, 14f),
                    AppText.CardinalDirection(entry.Placement.AzimuthDegrees), _small);
                if (_dragTravel < 8f && Clicked(row)) Select(entry.Key);
            }
            GUI.EndScrollView();
        }

        private void HandleSwipe(Rect viewport, float contentHeight)
        {
            var maxScroll = Mathf.Max(0f, contentHeight - viewport.height);
            var e = Event.current;
            switch (e.type)
            {
                case EventType.MouseDown when viewport.Contains(e.mousePosition):
                    _dragging = true;
                    _lastDragY = e.mousePosition.y;
                    _dragTravel = 0f;
                    break;
                case EventType.MouseDrag when _dragging:
                {
                    var dy = e.mousePosition.y - _lastDragY;
                    _lastDragY = e.mousePosition.y;
                    _dragTravel += Mathf.Abs(dy);
                    _scroll.y = Mathf.Clamp(_scroll.y - dy, 0f, maxScroll);
                    e.Use();
                    break;
                }
                case EventType.MouseUp:
                    // Swallow the release after a real drag so it does not land as a row tap.
                    if (_dragging && _dragTravel > 8f) e.Use();
                    _dragging = false;
                    break;
                case EventType.ScrollWheel when viewport.Contains(e.mousePosition):
                    _scroll.y = Mathf.Clamp(_scroll.y + e.delta.y * 12f, 0f, maxScroll);
                    e.Use();
                    break;
            }
            _scroll.y = Mathf.Clamp(_scroll.y, 0f, maxScroll);
        }

        private void ApplyFilter()
        {
            _filtered.Clear();
            var q = _query?.Trim();
            if (string.IsNullOrEmpty(q))
            {
                _filtered.AddRange(_entries);
                return;
            }
            foreach (var entry in _entries)
            {
                if (entry.City.Name.IndexOf(q, System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    PlaceNames.Get(entry.City.Name).IndexOf(q, System.StringComparison.OrdinalIgnoreCase) >= 0)
                    _filtered.Add(entry);
            }
        }

        private void DrawPaywall(float left, float top, float width, float height)
        {
            var card = new Rect(left + width * .5f - 165f, top + 96f, 330f, 372f);
            GUI.color = new Color(1f, 1f, 1f, .96f);
            GUI.DrawTexture(card, _card);
            GUI.color = MedievalIconography.Ink;
            GUI.DrawTexture(new Rect(card.x + 3f, card.y + 3f, card.width - 6f, 2f), _white);
            GUI.DrawTexture(new Rect(card.x + 3f, card.yMax - 5f, card.width - 6f, 2f), _white);
            GUI.color = Color.white;

            GUI.DrawTexture(new Rect(card.center.x - 58f, card.y + 12f, 116f, 116f), _arse, ScaleMode.ScaleToFit);
            GUI.color = MedievalIconography.Blood;
            GUI.Label(new Rect(card.x + 20f, card.y + 132f, card.width - 40f, 120f),
                AppText.Get(TextKey.SecretInitiate), _creed);
            GUI.color = Color.white;

            var state = FlatEarthEntitlement.State;
            var buy = new Rect(card.x + 24f, card.yMax - 118f, card.width - 48f, 46f);
            GUI.color = state.Phase == PurchasePhase.Pending
                ? new Color(.55f, .48f, .34f, 1f)
                : MedievalIconography.Gilt;
            GUI.DrawTexture(buy, _white);
            GUI.color = Color.white;
            var label = state.Phase switch
            {
                PurchasePhase.Pending => AppText.Get(TextKey.PurchasePending),
                _ => $"{AppText.Get(TextKey.Unlock)} · {FlatEarthEntitlement.LocalizedPrice}"
            };
            GUI.Label(buy, label, _button);
            if (state.Phase != PurchasePhase.Pending && Clicked(buy)) FlatEarthEntitlement.Purchase();

            var restore = new Rect(card.x + 24f, card.yMax - 64f, card.width - 48f, 24f);
            GUI.Label(restore, AppText.Get(TextKey.RestorePurchase), _small);
            if (state.Phase != PurchasePhase.Pending && Clicked(restore)) FlatEarthEntitlement.Restore();

            if (state.Phase == PurchasePhase.Failed)
            {
                GUI.color = MedievalIconography.Blood;
                GUI.Label(new Rect(card.x + 18f, card.yMax - 36f, card.width - 36f, 20f),
                    AppText.Get(TextKey.PurchaseFailed), _small);
                GUI.color = Color.white;
            }
        }

        private void DrawCloseButton(float left, float top, float width)
        {
            // A back tab pinned to the top-left, clear of the centred creed.
            var rect = new Rect(left + 4f, top + 44f, 86f, 30f);
            GUI.color = new Color(1f, 1f, 1f, .5f);
            GUI.DrawTexture(rect, _card);
            GUI.color = MedievalIconography.Blood;
            GUI.Label(new Rect(rect.x + 4f, rect.y, rect.width - 6f, rect.height),
                "‹ " + AppText.Get(TextKey.BackToGlobe), _backLink);
            GUI.color = Color.white;
            if (Clicked(rect)) Close();
        }

        private IEnumerator LoadCartography()
        {
            string coastJson = null;
            string borderJson = null;
            yield return ReadStreamingAsset("ne_110m_coastline.geojson", text => coastJson = text);
            yield return ReadStreamingAsset("ne_110m_admin_0_boundary_lines_land.geojson", text => borderJson = text);
            if (string.IsNullOrEmpty(coastJson)) yield break;

            var cartography = FlatEarthCartography.FromGeoJson(coastJson, borderJson);
            yield return null; // let the parse frame breathe before the raster bake
            if (_plate != null) Destroy(_plate);
            _plate = MedievalIconography.FlatEarthPlate(768, cartography.Coastlines, cartography.Borders);
        }

        private static IEnumerator ReadStreamingAsset(string fileName, System.Action<string> onLoaded)
        {
            var path = Application.streamingAssetsPath + "/" + fileName;
            if (!path.Contains("://")) path = new System.Uri(path).AbsoluteUri;
            using var request = UnityWebRequest.Get(path);
            yield return request.SendWebRequest();
            if (request.result == UnityWebRequest.Result.Success)
                onLoaded(request.downloadHandler.text);
            else
                Debug.LogWarning($"Flat Earth cartography: could not load {fileName}: {request.error}");
        }

        private void RefreshEntries()
        {
            var custom = _streamer != null ? _streamer.CustomPlaces.Count : 0;
            if (_entriesReady && custom == _entriesCustomCount &&
                GeoMath.DistanceKm(_entriesFor, _location.Current) < 2d)
                return;

            _entriesFor = _location.Current;
            _entriesCustomCount = custom;
            _entriesReady = true;
            _entries.Clear();

            var cities = CityCatalog.All.AsEnumerable();
            if (_streamer != null) cities = cities.Concat(_streamer.CustomPlaces);
            foreach (var city in cities)
            {
                var key = city.Name + "|" + city.Country;
                var placement = FlatEarthProjection.Place(_entriesFor, key, city.Position);
                _entries.Add(new Entry(city, key, placement));
            }
            _entries.Sort((a, b) => a.Placement.AzimuthDegrees.CompareTo(b.Placement.AzimuthDegrees));
        }

        private void Select(string key)
        {
            _selectedKey = _selectedKey == key ? null : key;
        }

        // Stamped rather than rotated so it is immune to the GUI.matrix rotation used by the map.
        private void DrawLine(Vector2 a, Vector2 b, float width, Color color)
        {
            var length = (b - a).magnitude;
            if (length < 1f) return;
            var steps = Mathf.CeilToInt(length / 2.5f);
            GUI.color = color;
            for (var i = 0; i <= steps; i++)
            {
                var p = Vector2.Lerp(a, b, i / (float)steps);
                GUI.DrawTexture(new Rect(p.x - width * .5f, p.y - width * .5f, width, width), _dot);
            }
            GUI.color = Color.white;
        }

        private void TileParchment(Rect area)
        {
            if (area.width <= 0f || area.height <= 0f) return;
            GUI.DrawTextureWithTexCoords(area, _parchment,
                new Rect(area.x / 160f, area.y / 160f, area.width / 160f, area.height / 160f));
        }

        private void EnsureStyles()
        {
            if (_title != null) return;
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) return;
            _eyebrow = Style(font, 10, FontStyle.Bold, MedievalIconography.Blood, TextAnchor.MiddleCenter);
            _title = Style(font, 17, FontStyle.Bold, MedievalIconography.Ink, TextAnchor.MiddleCenter);
            _body = Style(font, 12, FontStyle.Normal, MedievalIconography.Ink, TextAnchor.UpperLeft);
            _body.wordWrap = true;
            _small = Style(font, 9, FontStyle.Bold, MedievalIconography.Ink, TextAnchor.MiddleCenter);
            _rowTitle = Style(font, 12, FontStyle.Bold, MedievalIconography.Ink, TextAnchor.MiddleLeft);
            _button = Style(font, 13, FontStyle.Bold, new Color(.16f, .10f, .05f), TextAnchor.MiddleCenter);
            _backLink = Style(font, 9, FontStyle.Bold, MedievalIconography.Blood, TextAnchor.MiddleLeft);
            _backLink.wordWrap = true;
            _creed = Style(font, 11, FontStyle.BoldAndItalic, MedievalIconography.Blood, TextAnchor.MiddleCenter);
            _creed.wordWrap = true;
            _search = new GUIStyle(GUI.skin.textField)
            {
                font = font,
                fontSize = 13,
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(8, 8, 4, 4)
            };
            _search.normal.textColor = MedievalIconography.Ink;
            _search.focused.textColor = MedievalIconography.Ink;
        }

        private static GUIStyle Style(Font font, int size, FontStyle fontStyle, Color color, TextAnchor anchor) =>
            new()
            {
                font = font,
                fontSize = size,
                fontStyle = fontStyle,
                alignment = anchor,
                normal = new GUIStyleState { textColor = color }
            };

        private static bool Clicked(Rect rect)
        {
            var current = Event.current;
            if (current == null || current.type != EventType.MouseUp || current.button != 0 ||
                !rect.Contains(current.mousePosition)) return false;
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

        private static Texture2D Disc(int size, Color color)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Bilinear
            };
            var pixels = new Color32[size * size];
            var c = (size - 1) * .5f;
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                var d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c));
                var a = Mathf.Clamp01(size * .5f - d);
                pixels[y * size + x] = new Color(color.r, color.g, color.b, color.a * a);
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
        }

        private static Texture2D WedgeTexture(int size)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            var pixels = new Color32[size * size];
            var mid = (size - 1) * .5f;
            for (var y = 0; y < size; y++)
            {
                var frac = y / (size - 1f); // 0 = apex row, 1 = wide base row
                var halfSpan = frac * mid + .5f;
                for (var x = 0; x < size; x++)
                {
                    var inside = Mathf.Abs(x - mid) <= halfSpan ? 1f : 0f;
                    var alpha = inside * Mathf.Lerp(.04f, 1f, frac);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
        }

        private static Texture2D Ring(int size, Color color)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Bilinear
            };
            var pixels = new Color32[size * size];
            var c = (size - 1) * .5f;
            var outer = size * .5f;
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                var d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c));
                var a = Mathf.Clamp01(outer - d) * Mathf.Clamp01(d - (outer - 3f));
                pixels[y * size + x] = new Color(color.r, color.g, color.b, color.a * a);
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
        }

        private readonly struct Entry
        {
            public readonly City City;
            public readonly string Key;
            public readonly FlatEarthPlacement Placement;

            public Entry(City city, string key, FlatEarthPlacement placement)
            {
                City = city;
                Key = key;
                Placement = placement;
            }
        }
    }
}
