using NUnit.Framework;
using TransparentEarth.Data;
using TransparentEarth.Geo;

namespace TransparentEarth.Tests
{
    public sealed class AntipodeResolverTests
    {
        [Test]
        public void FindsClosestNamedObjectFromCatalog()
        {
            var antipode = GeoMath.Antipode(new GeoPoint(44.8125, 20.4612));
            var result = AntipodeResolver.FindNearestNamedObject(antipode);

            Assert.That(result.Object.Name, Is.Not.Empty);
            foreach (var candidate in CityCatalog.All)
                Assert.That(result.Projection.DistanceKm,
                    Is.LessThanOrEqualTo(GeoMath.DistanceKm(antipode, candidate.Position) + 1e-6));
            foreach (var candidate in GeographicObjectCatalog.All)
                Assert.That(result.Projection.DistanceKm,
                    Is.LessThanOrEqualTo(GeoMath.DistanceKm(antipode, candidate.Position) + 1e-6));
            Assert.That(result.Object.Name, Is.EqualTo("Chatham Islands"));
        }

        [Test]
        public void DirectionIsFiniteAndNormalized()
        {
            var result = AntipodeResolver.FindNearestNamedObject(new GeoPoint(-44.8125, -159.5388));

            Assert.That(double.IsNaN(result.Projection.BearingDegrees), Is.False);
            Assert.That(result.Projection.BearingDegrees, Is.InRange(0d, 360d));
            Assert.That(result.Projection.DistanceKm, Is.GreaterThanOrEqualTo(0d));
        }
    }
}
