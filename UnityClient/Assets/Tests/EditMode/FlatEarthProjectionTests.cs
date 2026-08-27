using NUnit.Framework;
using TransparentEarth.Geo;
using UnityEngine;

namespace TransparentEarth.Tests
{
    public sealed class FlatEarthProjectionTests
    {
        [Test]
        public void NorthPoleCollapsesToTheDiscCentre()
        {
            var disc = FlatEarthProjection.DiscPoint(new GeoPoint(90d, 137d));
            Assert.That(disc.magnitude, Is.EqualTo(0f).Within(1e-5f));
        }

        [Test]
        public void SouthPoleIsSmearedOntoTheOuterRimForEveryLongitude()
        {
            for (var longitude = -180; longitude <= 180; longitude += 45)
            {
                var disc = FlatEarthProjection.DiscPoint(new GeoPoint(-90d, longitude));
                Assert.That(disc.magnitude, Is.EqualTo(1f).Within(1e-5f), $"lon {longitude}");
            }
        }

        [Test]
        public void EquatorSitsHalfwayToTheRim()
        {
            Assert.That(FlatEarthProjection.RadiusNormalized(0d), Is.EqualTo(.5d).Within(1e-9));
        }

        [Test]
        public void ZeroLongitudePointsUp()
        {
            var disc = FlatEarthProjection.DiscPoint(new GeoPoint(0d, 0d));
            Assert.That(disc.x, Is.EqualTo(0f).Within(1e-5f));
            Assert.That(disc.y, Is.GreaterThan(0f));
        }

        [Test]
        public void EastLongitudePutsThePointToTheRight()
        {
            var disc = FlatEarthProjection.DiscPoint(new GeoPoint(0d, 90d));
            Assert.That(disc.x, Is.GreaterThan(0f));
            Assert.That(Mathf.Abs(disc.y), Is.LessThan(1e-4f));
        }

        [Test]
        public void ReliefStaysWithinOneDegreeAndIsDeterministic()
        {
            var first = FlatEarthProjection.ReliefDegrees("Belgrade|RS");
            var second = FlatEarthProjection.ReliefDegrees("Belgrade|RS");
            Assert.That(first, Is.EqualTo(second));
            Assert.That(Mathf.Abs((float)first), Is.LessThanOrEqualTo((float)FlatEarthProjection.ReliefLimitDegrees));
        }

        [Test]
        public void DistantCitiesShareTheOneHorizonLine()
        {
            Assert.That(FlatEarthProjection.HorizonElevationDegrees("Tokyo|JP", 8000d), Is.EqualTo(0d));
        }

        [Test]
        public void NearbyCitiesGetASubDegreeReliefWobble()
        {
            var relief = FlatEarthProjection.HorizonElevationDegrees("Zemun|RS", 30d);
            Assert.That(Mathf.Abs((float)relief), Is.GreaterThan(0f));
            Assert.That(Mathf.Abs((float)relief), Is.LessThan(1f));
        }

        [Test]
        public void PlaceReportsTheGreatCircleBearingAsAzimuth()
        {
            var observer = new GeoPoint(0d, 0d);
            var placement = FlatEarthProjection.Place(observer, "east", new GeoPoint(0d, 10d));
            Assert.That(placement.AzimuthDegrees, Is.EqualTo(90d).Within(.5));
        }
    }
}
