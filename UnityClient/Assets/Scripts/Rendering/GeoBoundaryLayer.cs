using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
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
        private string _coastlineJson;
        private string _countryJson;
        private GeoPoint _builtFor;

        public bool CoastlinesVisible => _coastlines == null || _coastlines.activeSelf;
        public bool CountriesVisible => _countries != null && _countries.activeSelf;

        public void Initialize(Transform globeRoot, LocationProvider location, float radius)
        {
            _globeRoot = globeRoot;
            _location = location;
            _radius = radius;
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
            layer.GetComponent<MeshFilter>().sharedMesh = CreateMesh(json, _location.Current, _radius,
                countries ? "Natural Earth Country Borders" : "Natural Earth Coastlines");
            var color = countries ? new Color(.62f, .96f, .82f, .33f) : new Color(.72f, 1f, .88f, .82f);
            var material = new Material(Shader.Find("Sprites/Default")) { color = color };
            material.renderQueue = countries ? 3110 : 3120;
            layer.GetComponent<MeshRenderer>().sharedMaterial = material;
            layer.SetActive(wasVisible);
            if (countries) _countries = layer;
            else _coastlines = layer;
            _builtFor = _location.Current;
        }

        private void Update()
        {
            if (_location == null || GeoMath.DistanceKm(_builtFor, _location.Current) < 25d) return;
            _builtFor = _location.Current;
            if (_coastlineJson != null) Build(_coastlineJson, false);
            if (_countryJson != null) Build(_countryJson, true);
        }

        private static Mesh CreateMesh(string json, GeoPoint observer, float radius, string name)
        {
            var vertices = new List<Vector3>(24000);
            var search = 0;
            while ((search = json.IndexOf("\"coordinates\":", search, StringComparison.Ordinal)) >= 0)
            {
                var start = json.IndexOf('[', search);
                var lastMulti = json.LastIndexOf("\"type\":\"MultiLineString\"", search, StringComparison.Ordinal);
                var lastLine = json.LastIndexOf("\"type\":\"LineString\"", search, StringComparison.Ordinal);
                var index = start;
                if (lastMulti > lastLine) ReadMultiLine(json, ref index, observer, radius, vertices);
                else ReadLine(json, ref index, observer, radius, vertices);
                search = Math.Max(index, search + 16);
            }

            var mesh = new Mesh { name = name, indexFormat = IndexFormat.UInt32 };
            mesh.SetVertices(vertices);
            var indices = new int[vertices.Count];
            for (var i = 0; i < indices.Length; i++) indices[i] = i;
            mesh.SetIndices(indices, MeshTopology.Lines, 0, false);
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void ReadMultiLine(string json, ref int index, GeoPoint observer, float radius, List<Vector3> vertices)
        {
            index++;
            while (index < json.Length)
            {
                Skip(json, ref index);
                if (json[index] == ']') { index++; return; }
                ReadLine(json, ref index, observer, radius, vertices);
            }
        }

        private static void ReadLine(string json, ref int index, GeoPoint observer, float radius, List<Vector3> vertices)
        {
            index++;
            var points = new List<Vector3>(64);
            while (index < json.Length)
            {
                Skip(json, ref index);
                if (json[index] == ']') { index++; break; }
                if (json[index] != '[') { index++; continue; }
                index++;
                var longitude = ReadNumber(json, ref index);
                Skip(json, ref index);
                if (json[index] == ',') index++;
                var latitude = ReadNumber(json, ref index);
                while (index < json.Length && json[index] != ']') index++;
                if (index < json.Length) index++;
                points.Add(GeoMath.SurfaceNormalEnu(observer, new GeoPoint(latitude, longitude)) * radius);
            }

            for (var i = 1; i < points.Count; i++) AddArc(points[i - 1], points[i], radius, vertices);
        }

        private static void AddArc(Vector3 from, Vector3 to, float radius, List<Vector3> vertices)
        {
            var angle = Vector3.Angle(from, to);
            var pieces = Mathf.Max(1, Mathf.CeilToInt(angle / 2.5f));
            var previous = from;
            for (var i = 1; i <= pieces; i++)
            {
                var current = Vector3.Slerp(from, to, i / (float)pieces).normalized * radius;
                vertices.Add(previous);
                vertices.Add(current);
                previous = current;
            }
        }

        private static double ReadNumber(string json, ref int index)
        {
            Skip(json, ref index);
            var start = index;
            while (index < json.Length && (char.IsDigit(json[index]) || json[index] is '-' or '+' or '.' or 'e' or 'E')) index++;
            return double.Parse(json.Substring(start, index - start), CultureInfo.InvariantCulture);
        }

        private static void Skip(string json, ref int index)
        {
            while (index < json.Length && (char.IsWhiteSpace(json[index]) || json[index] == ',')) index++;
        }
    }
}
