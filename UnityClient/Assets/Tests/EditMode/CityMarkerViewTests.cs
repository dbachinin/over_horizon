using NUnit.Framework;
using TransparentEarth.Data;
using TransparentEarth.Geo;
using TransparentEarth.Rendering;
using UnityEngine;

namespace TransparentEarth.Tests
{
    public sealed class CityMarkerViewTests
    {
        [TestCase(29.9, true)]
        [TestCase(30.0, true)]
        [TestCase(30.1, false)]
        public void NearbyClassificationDependsOnActualDistance(double distanceKm, bool expected)
        {
            var marker = Marker(distanceKm, isNearbySettlement: false);
            Assert.That(marker.IsNearby, Is.EqualTo(expected));
        }

        [Test]
        public void LoadedNearbySettlementAlwaysHasLeader()
        {
            Assert.That(Marker(31d, isNearbySettlement: true).IsNearby, Is.True);
        }

        [TestCase(-.1, true)]
        [TestCase(0d, true)]
        [TestCase(.1, false)]
        public void LeaderLineCoversOneThirdOfEarthCircumference(double thresholdOffsetKm, bool expected)
        {
            var distance = GeoMath.OneThirdCircumferenceKm + thresholdOffsetKm;
            Assert.That(Marker(distance, isNearbySettlement: false).HasLeaderLine, Is.EqualTo(expected));
        }

        [Test]
        public void RevealDoesNotDisappearWhenNextWaveRestarts()
        {
            var marker = Marker(12d, isNearbySettlement: false);
            Assert.That(marker.AccumulateReveal(.82f), Is.EqualTo(.82f).Within(1e-6f));
            Assert.That(marker.AccumulateReveal(0f), Is.EqualTo(.82f).Within(1e-6f));
        }

        private static CityMarkerView Marker(double distanceKm, bool isNearbySettlement)
        {
            var city = new City("Test", "TS", 0d, 0d, 1);
            var projection = new GeoProjection(distanceKm, 0d, 0d, 0d, Vector3.forward);
            return new CityMarkerView(city, projection, null, isNearbySettlement);
        }
    }
}
