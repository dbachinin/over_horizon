using System;
using UnityEngine;

namespace TransparentEarth.Geo
{
    public readonly struct GeoPoint
    {
        public readonly double Latitude;
        public readonly double Longitude;
        public readonly double AltitudeMeters;

        public GeoPoint(double latitude, double longitude, double altitudeMeters = 0)
        {
            Latitude = latitude;
            Longitude = longitude;
            AltitudeMeters = altitudeMeters;
        }
    }

    public readonly struct GeoProjection
    {
        public readonly double DistanceKm;
        public readonly double BearingDegrees;
        public readonly double CentralAngleDegrees;
        public readonly double ElevationDegrees;
        public readonly Vector3 DirectionEnu;

        public GeoProjection(double distanceKm, double bearingDegrees, double centralAngleDegrees,
            double elevationDegrees, Vector3 directionEnu)
        {
            DistanceKm = distanceKm;
            BearingDegrees = bearingDegrees;
            CentralAngleDegrees = centralAngleDegrees;
            ElevationDegrees = elevationDegrees;
            DirectionEnu = directionEnu;
        }
    }

    public static class GeoMath
    {
        public const double EarthRadiusKm = 6371.0088;
        public const double HalfCircumferenceKm = Math.PI * EarthRadiusKm;

        public static GeoPoint Antipode(GeoPoint point) =>
            new GeoPoint(-point.Latitude, NormalizeLongitude(point.Longitude + 180.0), point.AltitudeMeters);

        public static double NormalizeLongitude(double longitude) =>
            ((longitude + 540.0) % 360.0) - 180.0;

        public static double DistanceKm(GeoPoint from, GeoPoint to)
        {
            var lat1 = DegreesToRadians(from.Latitude);
            var lat2 = DegreesToRadians(to.Latitude);
            var dLat = lat2 - lat1;
            var dLon = DegreesToRadians(to.Longitude - from.Longitude);
            var a = Math.Pow(Math.Sin(dLat / 2.0), 2.0) +
                    Math.Cos(lat1) * Math.Cos(lat2) * Math.Pow(Math.Sin(dLon / 2.0), 2.0);
            return 2.0 * EarthRadiusKm * Math.Asin(Math.Min(1.0, Math.Sqrt(a)));
        }

        public static GeoProjection Project(GeoPoint observer, GeoPoint target)
        {
            var observerEcef = ToEcef(observer);
            var targetEcef = ToEcef(target);
            var delta = targetEcef - observerEcef;
            var enu = EcefDeltaToEnu(delta, observer);
            var direction = enu.sqrMagnitude < 1e-12f ? Vector3.forward : enu.normalized;
            var horizontal = Math.Sqrt(enu.x * enu.x + enu.z * enu.z);
            var elevation = Math.Atan2(enu.y, horizontal) * Mathf.Rad2Deg;
            var bearing = (Math.Atan2(enu.x, enu.z) * Mathf.Rad2Deg + 360.0) % 360.0;
            var distance = DistanceKm(observer, target);
            return new GeoProjection(distance, bearing, distance / EarthRadiusKm * Mathf.Rad2Deg, elevation, direction);
        }

        public static Vector3 SurfaceNormalEnu(GeoPoint observer, GeoPoint target)
        {
            var targetLat = DegreesToRadians(target.Latitude);
            var targetLon = DegreesToRadians(target.Longitude);
            var targetUnit = new Vector3(
                (float)(Math.Cos(targetLat) * Math.Cos(targetLon)),
                (float)(Math.Cos(targetLat) * Math.Sin(targetLon)),
                (float)Math.Sin(targetLat));
            return EcefDeltaToEnu(targetUnit, observer).normalized;
        }

        private static Vector3 ToEcef(GeoPoint point)
        {
            var lat = DegreesToRadians(point.Latitude);
            var lon = DegreesToRadians(point.Longitude);
            var radius = EarthRadiusKm + point.AltitudeMeters / 1000.0;
            return new Vector3(
                (float)(radius * Math.Cos(lat) * Math.Cos(lon)),
                (float)(radius * Math.Cos(lat) * Math.Sin(lon)),
                (float)(radius * Math.Sin(lat)));
        }

        // Unity local coordinates are x=east, y=up, z=north.
        private static Vector3 EcefDeltaToEnu(Vector3 delta, GeoPoint observer)
        {
            var lat = DegreesToRadians(observer.Latitude);
            var lon = DegreesToRadians(observer.Longitude);
            var east = -Math.Sin(lon) * delta.x + Math.Cos(lon) * delta.y;
            var north = -Math.Sin(lat) * Math.Cos(lon) * delta.x
                        - Math.Sin(lat) * Math.Sin(lon) * delta.y
                        + Math.Cos(lat) * delta.z;
            var up = Math.Cos(lat) * Math.Cos(lon) * delta.x
                     + Math.Cos(lat) * Math.Sin(lon) * delta.y
                     + Math.Sin(lat) * delta.z;
            return new Vector3((float)east, (float)up, (float)north);
        }

        private static double DegreesToRadians(double value) => value * Math.PI / 180.0;
    }
}
