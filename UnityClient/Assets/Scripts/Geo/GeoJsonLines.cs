using System;
using System.Collections.Generic;
using System.Globalization;

namespace TransparentEarth.Geo
{
    /// <summary>
    /// Minimal streaming reader for the Natural Earth GeoJSON line layers used by the app
    /// (<c>LineString</c> / <c>MultiLineString</c> features). Returns each path as an ordered
    /// list of lon/lat <see cref="GeoPoint"/>s; callers decide how to project them.
    /// </summary>
    public static class GeoJsonLines
    {
        public static List<List<GeoPoint>> Parse(string json)
        {
            var lines = new List<List<GeoPoint>>(160);
            if (string.IsNullOrEmpty(json)) return lines;

            var search = 0;
            while ((search = json.IndexOf("\"coordinates\":", search, StringComparison.Ordinal)) >= 0)
            {
                var index = json.IndexOf('[', search);
                if (index < 0) break;
                var lastMulti = json.LastIndexOf("\"type\":\"MultiLineString\"", search, StringComparison.Ordinal);
                var lastLine = json.LastIndexOf("\"type\":\"LineString\"", search, StringComparison.Ordinal);
                if (lastMulti > lastLine) ReadMultiLine(json, ref index, lines);
                else ReadLine(json, ref index, lines);
                search = Math.Max(index, search + 16);
            }
            return lines;
        }

        private static void ReadMultiLine(string json, ref int index, List<List<GeoPoint>> lines)
        {
            index++;
            while (index < json.Length)
            {
                Skip(json, ref index);
                if (index >= json.Length || json[index] == ']') { index++; return; }
                ReadLine(json, ref index, lines);
            }
        }

        private static void ReadLine(string json, ref int index, List<List<GeoPoint>> lines)
        {
            index++;
            var points = new List<GeoPoint>(64);
            while (index < json.Length)
            {
                Skip(json, ref index);
                if (json[index] == ']') { index++; break; }
                if (json[index] != '[') { index++; continue; }
                index++;
                var longitude = ReadNumber(json, ref index);
                Skip(json, ref index);
                if (index < json.Length && json[index] == ',') index++;
                var latitude = ReadNumber(json, ref index);
                while (index < json.Length && json[index] != ']') index++;
                if (index < json.Length) index++;
                points.Add(new GeoPoint(latitude, longitude));
            }
            if (points.Count > 1) lines.Add(points);
        }

        private static double ReadNumber(string json, ref int index)
        {
            Skip(json, ref index);
            var start = index;
            while (index < json.Length &&
                   (char.IsDigit(json[index]) || json[index] is '-' or '+' or '.' or 'e' or 'E')) index++;
            return double.Parse(json.Substring(start, index - start), CultureInfo.InvariantCulture);
        }

        private static void Skip(string json, ref int index)
        {
            while (index < json.Length && (char.IsWhiteSpace(json[index]) || json[index] == ',')) index++;
        }
    }
}
