using System.Collections.Generic;
using UnityEngine;

namespace TransparentEarth.Geo
{
    /// <summary>
    /// Natural Earth coastlines and country borders pushed through the flat-earth distortion
    /// into unit-disc space (centre = North Pole, radius 1 = the ice wall, y up), ready to be
    /// rasterised onto the map plate. The projection is observer-independent, so this is built
    /// once and reused.
    /// </summary>
    public sealed class FlatEarthCartography
    {
        public IReadOnlyList<Vector2[]> Coastlines { get; }
        public IReadOnlyList<Vector2[]> Borders { get; }

        private FlatEarthCartography(List<Vector2[]> coastlines, List<Vector2[]> borders)
        {
            Coastlines = coastlines;
            Borders = borders;
        }

        public static FlatEarthCartography FromGeoJson(string coastlineJson, string borderJson) =>
            new(Project(coastlineJson), Project(borderJson));

        private static List<Vector2[]> Project(string json)
        {
            var paths = GeoJsonLines.Parse(json);
            var result = new List<Vector2[]>(paths.Count);
            foreach (var path in paths)
            {
                var projected = new Vector2[path.Count];
                for (var i = 0; i < path.Count; i++)
                    projected[i] = FlatEarthProjection.DiscPoint(path[i]);
                result.Add(projected);
            }
            return result;
        }
    }
}
