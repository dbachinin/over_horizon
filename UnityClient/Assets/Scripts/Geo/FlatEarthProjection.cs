using System;
using UnityEngine;

namespace TransparentEarth.Geo
{
    public readonly struct FlatEarthPlacement
    {
        /// Point on the unit map disc. The centre (0,0) is the North Pole, the circle
        /// of radius 1 is the "ice wall" the single South Pole is smeared across.
        public readonly Vector2 Disc;

        /// 0 at the North Pole, 1 on the outer rim.
        public readonly double RadiusNormalized;

        /// Clockwise angle from the top of the disc, degrees.
        public readonly double MapAngleDegrees;

        /// Initial great-circle bearing from the observer to the target, degrees.
        public readonly double AzimuthDegrees;

        /// Horizon relief for the single-line city strip. Always within
        /// [-ReliefLimitDegrees, ReliefLimitDegrees] and only non-zero for close cities.
        public readonly double ElevationDegrees;

        public readonly double DistanceKm;

        public FlatEarthPlacement(Vector2 disc, double radiusNormalized, double mapAngleDegrees,
            double azimuthDegrees, double elevationDegrees, double distanceKm)
        {
            Disc = disc;
            RadiusNormalized = radiusNormalized;
            MapAngleDegrees = mapAngleDegrees;
            AzimuthDegrees = azimuthDegrees;
            ElevationDegrees = elevationDegrees;
            DistanceKm = distanceKm;
        }
    }

    /// <summary>
    /// The "flat earth" coordinate distortion used by the novelty mode: a north-polar
    /// azimuthal-equidistant layout where meridians stay straight, parallels become
    /// concentric circles and the South Pole is stretched around the whole circumference.
    /// Cities are pinned to one horizon line; only neighbours get a sub-degree relief wobble
    /// so their labels do not collapse onto each other.
    /// </summary>
    public static class FlatEarthProjection
    {
        public const double ReliefLimitDegrees = 1.0;
        public const double ReliefRangeKm = 600.0;

        public static double RadiusNormalized(double latitude) =>
            Math.Clamp((90.0 - latitude) / 180.0, 0.0, 1.0);

        public static double MapAngleDegrees(double longitude) =>
            (GeoMath.NormalizeLongitude(longitude) + 360.0) % 360.0;

        public static Vector2 DiscPoint(GeoPoint point)
        {
            var radius = RadiusNormalized(point.Latitude);
            var angle = MapAngleDegrees(point.Longitude) * Math.PI / 180.0;
            // Clockwise from the top so 0° longitude points up and 90°E points right.
            return new Vector2((float)(radius * Math.Sin(angle)), (float)(radius * Math.Cos(angle)));
        }

        /// Deterministic, name-seeded relief in [-ReliefLimitDegrees, ReliefLimitDegrees].
        public static double ReliefDegrees(string key)
        {
            if (string.IsNullOrEmpty(key)) return 0.0;
            unchecked
            {
                var hash = 2166136261u;
                foreach (var character in key)
                {
                    hash ^= character;
                    hash *= 16777619u;
                }
                var unit = hash / (double)uint.MaxValue * 2.0 - 1.0;
                return unit * ReliefLimitDegrees;
            }
        }

        /// Relief that fades to exactly zero past ReliefRangeKm, keeping distant cities on one line.
        public static double HorizonElevationDegrees(string key, double distanceKm)
        {
            if (distanceKm >= ReliefRangeKm || distanceKm < 0.0) return 0.0;
            return ReliefDegrees(key) * (1.0 - distanceKm / ReliefRangeKm);
        }

        public static FlatEarthPlacement Place(GeoPoint observer, string key, GeoPoint target)
        {
            var projection = GeoMath.Project(observer, target);
            return new FlatEarthPlacement(
                DiscPoint(target),
                RadiusNormalized(target.Latitude),
                MapAngleDegrees(target.Longitude),
                projection.BearingDegrees,
                HorizonElevationDegrees(key, projection.DistanceKm),
                projection.DistanceKm);
        }
    }
}
