using NUnit.Framework;
using TransparentEarth.Geo;
using UnityEngine;

namespace TransparentEarth.Tests
{
    public sealed class GeoMathTests
    {
        [Test]
        public void AntipodeAcrossDateLineIsNormalized()
        {
            var result = GeoMath.Antipode(new GeoPoint(44.7866, 20.4489));
            Assert.That(result.Latitude, Is.EqualTo(-44.7866).Within(1e-9));
            Assert.That(result.Longitude, Is.EqualTo(-159.5511).Within(1e-9));
        }

        [Test]
        public void AntipodeIsHalfCircumferenceAway()
        {
            var origin = new GeoPoint(0, 0);
            Assert.That(GeoMath.DistanceKm(origin, GeoMath.Antipode(origin)), Is.EqualTo(GeoMath.HalfCircumferenceKm).Within(1e-5));
        }

        [Test]
        public void NearbyNorthTargetUsesPositiveUnityZ()
        {
            var projection = GeoMath.Project(new GeoPoint(0, 0), new GeoPoint(1, 0));
            Assert.That(projection.DirectionEnu.z, Is.GreaterThan(0));
            Assert.That(Mathf.Abs(projection.DirectionEnu.x), Is.LessThan(1e-4));
            Assert.That(projection.ElevationDegrees, Is.LessThan(0));
        }

        [Test]
        public void NearbyEastTargetUsesPositiveUnityX()
        {
            var projection = GeoMath.Project(new GeoPoint(0, 0), new GeoPoint(0, 1));
            Assert.That(projection.DirectionEnu.x, Is.GreaterThan(0));
            Assert.That(Mathf.Abs(projection.DirectionEnu.z), Is.LessThan(1e-4));
        }

        [Test]
        public void SurfaceNormalAtObserverPointsUp()
        {
            var observer = new GeoPoint(44.7866, 20.4489);
            var normal = GeoMath.SurfaceNormalEnu(observer, observer);
            Assert.That(normal.x, Is.EqualTo(0f).Within(1e-5f));
            Assert.That(normal.y, Is.EqualTo(1f).Within(1e-5f));
            Assert.That(normal.z, Is.EqualTo(0f).Within(1e-5f));
        }

        [Test]
        public void AntipodeSurfaceNormalPointsDown()
        {
            var observer = new GeoPoint(44.7866, 20.4489);
            var normal = GeoMath.SurfaceNormalEnu(observer, GeoMath.Antipode(observer));
            Assert.That(normal.y, Is.EqualTo(-1f).Within(1e-5f));
        }

        [Test]
        public void EnuUpMapsBackToObserverEcefNormal()
        {
            var observer = new GeoPoint(44.7866, 20.4489);
            var ecef = GeoMath.EnuToEcefMatrix(observer).MultiplyVector(Vector3.up).normalized;
            var expectedLatitude = Mathf.Asin(ecef.z) * Mathf.Rad2Deg;
            var expectedLongitude = Mathf.Atan2(ecef.y, ecef.x) * Mathf.Rad2Deg;
            Assert.That(expectedLatitude, Is.EqualTo(observer.Latitude).Within(1e-4));
            Assert.That(expectedLongitude, Is.EqualTo(observer.Longitude).Within(1e-4));
        }

        [Test]
        public void EnuNorthAtEquatorMapsTowardNorthPole()
        {
            var ecef = GeoMath.EnuToEcefMatrix(new GeoPoint(0, 0)).MultiplyVector(Vector3.forward);
            Assert.That(ecef.z, Is.EqualTo(1f).Within(1e-5f));
        }

        [TestCase(90, 0)]
        [TestCase(-90, 0)]
        [TestCase(0, 180)]
        [TestCase(0, -180)]
        public void EdgeCoordinatesProduceFiniteProjection(double latitude, double longitude)
        {
            var projection = GeoMath.Project(new GeoPoint(44.7866, 20.4489), new GeoPoint(latitude, longitude));
            Assert.That(float.IsNaN(projection.DirectionEnu.x), Is.False);
            Assert.That(double.IsNaN(projection.DistanceKm), Is.False);
        }
    }
}
