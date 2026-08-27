using System;
using System.Collections;
using System.Collections.Generic;
using TransparentEarth.Geo;
using TransparentEarth.Sensors;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Rendering;

namespace TransparentEarth.Rendering
{
    public sealed class GeoBoundaryLayer : MonoBehaviour
    {
        private float _radius;
        private LocationProvider _location;
        private Transform _globeRoot;
        private GameObject _coastlines;
        private GameObject _countries;
        private Material _surfaceMaterial;
        private Material _interiorMaterial;
        private Texture2D _landMask;
        private string _coastlineJson;
        private string _countryJson;
        private GeoPoint _builtFor;

        public bool CoastlinesVisible => _coastlines == null || _coastlines.activeSelf;
        public bool CountriesVisible => _countries != null && _countries.activeSelf;

        public void Initialize(Transform globeRoot, LocationProvider location, float radius, Material surfaceMaterial,
            Material interiorMaterial)
        {
            _globeRoot = globeRoot;
            _location = location;
            _radius = radius;
            _surfaceMaterial = surfaceMaterial;
            _interiorMaterial = interiorMaterial;
            _builtFor = location.Current;
            StartCoroutine(Load("ne_110m_coastline.geojson", false));
            StartCoroutine(Load("ne_110m_admin_0_boundary_lines_land.geojson", true));
        }

        public void SetCoastlinesVisible(bool visible)
        {
            if (_coastlines != null) _coastlines.SetActive(visible);
        }

        public void SetCountriesVisible(bool visible)
        {
            if (_countries != null) _countries.SetActive(visible);
        }

        private IEnumerator Load(string fileName, bool countries)
        {
            var path = Application.streamingAssetsPath + "/" + fileName;
            if (!path.Contains("://")) path = new Uri(path).AbsoluteUri;
            using var request = UnityWebRequest.Get(path);
            yield return request.SendWebRequest();
            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"Could not load geographic layer {fileName}: {request.error}");
                yield break;
            }

            if (countries) _countryJson = request.downloadHandler.text;
            else _coastlineJson = request.downloadHandler.text;
            Build(request.downloadHandler.text, countries);
        }

        private void Build(string json, bool countries)
        {
            var oldObject = countries ? _countries : _coastlines;
            var wasVisible = oldObject == null ? !countries : oldObject.activeSelf;
            if (oldObject != null) Destroy(oldObject);
            var layer = new GameObject(countries ? "Countries Layer" : "Continents Layer", typeof(MeshFilter), typeof(MeshRenderer));
            layer.transform.SetParent(_globeRoot, false);
            var lines = ParseLines(json, _location.Current, _radius);
            var enuToEcef = GeoMath.EnuToEcefMatrix(_location.Current);
            _surfaceMaterial.SetMatrix("_EnuToEcef", enuToEcef);
            _interiorMaterial.SetMatrix("_EnuToEcef", enuToEcef);
            layer.GetComponent<MeshFilter>().sharedMesh = CreateMesh(lines,
                countries ? "Natural Earth Country Borders" : "Natural Earth Coastlines");
            var color = countries ? new Color(.62f, .96f, .82f, .24f) : new Color(.72f, 1f, .88f, .68f);
            var material = new Material(Shader.Find("Sprites/Default")) { color = color };
            material.renderQueue = countries ? 3110 : 3120;
            layer.GetComponent<MeshRenderer>().sharedMaterial = material;
            layer.SetActive(wasVisible);
            if (countries) _countries = layer;
            else
            {
                _coastlines = layer;
                RebuildLandMask(lines, enuToEcef);
            }
            _builtFor = _location.Current;
        }

        private void Update()
        {
            if (_location == null || GeoMath.DistanceKm(_builtFor, _location.Current) < 25d) return;
            _builtFor = _location.Current;
            if (_coastlineJson != null) Build(_coastlineJson, false);
            if (_countryJson != null) Build(_countryJson, true);
        }

        private static List<List<Vector3>> ParseLines(string json, GeoPoint observer, float radius)
        {
            var paths = GeoJsonLines.Parse(json);
            var lines = new List<List<Vector3>>(paths.Count);
            foreach (var path in paths)
            {
                var points = new List<Vector3>(path.Count);
                foreach (var geoPoint in path)
                    points.Add(GeoMath.SurfaceNormalEnu(observer, geoPoint) * radius);
                lines.Add(points);
            }
            return lines;
        }

        private static Mesh CreateMesh(List<List<Vector3>> lines, string name)
        {
            var vertices = new List<Vector3>(32000);
            foreach (var points in lines) AddSmoothedLine(points, vertices);
            var mesh = new Mesh { name = name, indexFormat = IndexFormat.UInt32 };
            mesh.SetVertices(vertices);
            var indices = new int[vertices.Count];
            for (var i = 0; i < indices.Length; i++) indices[i] = i;
            mesh.SetIndices(indices, MeshTopology.Lines, 0, false);
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void AddSmoothedLine(List<Vector3> points, List<Vector3> vertices)
        {
            if (points.Count < 2) return;
            var radius = points[0].magnitude;
            var closed = Vector3.Angle(points[0], points[^1]) < .05f;
            var count = closed ? points.Count - 1 : points.Count;
            var previous = points[0];
            for (var segment = 0; segment < count - (closed ? 0 : 1); segment++)
            {
                var i0 = closed ? (segment - 1 + count) % count : Mathf.Max(0, segment - 1);
                var i1 = segment;
                var i2 = (segment + 1) % count;
                var i3 = closed ? (segment + 2) % count : Mathf.Min(count - 1, segment + 2);
                var pieces = Mathf.Max(1, Mathf.CeilToInt(Vector3.Angle(points[i1], points[i2]) / .7f));
                for (var piece = 1; piece <= pieces; piece++)
                {
                    var t = piece / (float)pieces;
                    var t2 = t * t;
                    var t3 = t2 * t;
                    var current = .5f * ((2f * points[i1]) + (-points[i0] + points[i2]) * t +
                                         (2f * points[i0] - 5f * points[i1] + 4f * points[i2] - points[i3]) * t2 +
                                         (-points[i0] + 3f * points[i1] - 3f * points[i2] + points[i3]) * t3);
                    current = current.normalized * radius;
                    vertices.Add(previous);
                    vertices.Add(current);
                    previous = current;
                }
            }
        }

        private void RebuildLandMask(List<List<Vector3>> lines, Matrix4x4 enuToEcef)
        {
            const int width = 1024;
            const int height = 512;
            var pixels = new byte[width * height];
            foreach (var line in lines)
            {
                if (line.Count < 4 || Vector3.Angle(line[0], line[^1]) > 1f) continue;
                RasterizeRing(line, enuToEcef, pixels, width, height);
            }
            var landPixels = 0;
            foreach (var pixel in pixels) if (pixel > 0) landPixels++;
            Debug.Log($"Natural Earth land mask: {landPixels * 100f / pixels.Length:0.0}% land coverage");

            if (_landMask != null) Destroy(_landMask);
            var rgbaPixels = new Color32[pixels.Length];
            for (var i = 0; i < pixels.Length; i++)
                rgbaPixels[i] = pixels[i] > 0 ? new Color32(255, 255, 255, 255) : new Color32(0, 0, 0, 255);
            _landMask = new Texture2D(width, height, TextureFormat.RGBA32, false, true)
            {
                name = "Observer-relative Natural Earth land mask",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Repeat
            };
            _landMask.SetPixels32(rgbaPixels);
            _landMask.Apply(false, false);
            _surfaceMaterial.SetTexture("_LandMask", _landMask);
        }

        private static void RasterizeRing(List<Vector3> ring, Matrix4x4 enuToEcef, byte[] pixels, int width, int height)
        {
            var polygon = new List<Vector2>(ring.Count);
            var previousU = 0f;
            for (var i = 0; i < ring.Count; i++)
            {
                var normal = enuToEcef.MultiplyVector(ring[i].normalized).normalized;
                var u = Mathf.Atan2(normal.y, normal.x) / (Mathf.PI * 2f) + .5f;
                if (i > 0)
                {
                    while (u - previousU > .5f) u -= 1f;
                    while (u - previousU < -.5f) u += 1f;
                }
                previousU = u;
                polygon.Add(new Vector2(u, Mathf.Asin(normal.z) / Mathf.PI + .5f));
            }

            var minU = polygon[0].x;
            var maxU = minU;
            var minV = polygon[0].y;
            var maxV = minV;
            foreach (var point in polygon)
            {
                minU = Mathf.Min(minU, point.x);
                maxU = Mathf.Max(maxU, point.x);
                minV = Mathf.Min(minV, point.y);
                maxV = Mathf.Max(maxV, point.y);
            }

            var firstShift = Mathf.FloorToInt(-maxU);
            var lastShift = Mathf.CeilToInt(1f - minU);
            var intersections = new List<float>(polygon.Count);
            for (var shift = firstShift; shift <= lastShift; shift++)
            {
                var yMin = Mathf.Clamp(Mathf.FloorToInt(minV * height), 0, height - 1);
                var yMax = Mathf.Clamp(Mathf.CeilToInt(maxV * height), 0, height - 1);
                for (var y = yMin; y <= yMax; y++)
                {
                    var scanY = (y + .5f) / height;
                    intersections.Clear();
                    for (var edge = 0; edge < polygon.Count - 1; edge++)
                    {
                        var a = polygon[edge];
                        var b = polygon[edge + 1];
                        if ((a.y > scanY) == (b.y > scanY)) continue;
                        intersections.Add(a.x + shift + (scanY - a.y) * (b.x - a.x) / (b.y - a.y));
                    }
                    intersections.Sort();
                    for (var pair = 0; pair + 1 < intersections.Count; pair += 2)
                    {
                        var xMin = Mathf.Clamp(Mathf.CeilToInt(intersections[pair] * width), 0, width - 1);
                        var xMax = Mathf.Clamp(Mathf.FloorToInt(intersections[pair + 1] * width), 0, width - 1);
                        for (var x = xMin; x <= xMax; x++) pixels[y * width + x] = 255;
                    }
                }
            }

            var globalRing = new List<Vector3>(ring.Count);
            foreach (var point in ring) globalRing.Add(enuToEcef.MultiplyVector(point.normalized).normalized);
            if (ContainsSphericalPoint(globalRing, Vector3.forward * -1f))
            {
                var capEnd = Mathf.Clamp(Mathf.FloorToInt(minV * height), 0, height);
                for (var y = 0; y < capEnd; y++)
                for (var x = 0; x < width; x++) pixels[y * width + x] = 255;
            }
            if (ContainsSphericalPoint(globalRing, Vector3.forward))
            {
                var capStart = Mathf.Clamp(Mathf.CeilToInt(maxV * height), 0, height);
                for (var y = capStart; y < height; y++)
                for (var x = 0; x < width; x++) pixels[y * width + x] = 255;
            }
        }

        private static bool ContainsSphericalPoint(List<Vector3> ring, Vector3 point)
        {
            var winding = 0f;
            for (var i = 0; i < ring.Count - 1; i++)
            {
                var a = Vector3.ProjectOnPlane(ring[i], point).normalized;
                var b = Vector3.ProjectOnPlane(ring[i + 1], point).normalized;
                if (a.sqrMagnitude < .001f || b.sqrMagnitude < .001f) return true;
                winding += Mathf.Atan2(Vector3.Dot(point, Vector3.Cross(a, b)), Vector3.Dot(a, b));
            }
            return Mathf.Abs(winding) > Mathf.PI;
        }
    }
}
