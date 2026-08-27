using NUnit.Framework;
using TransparentEarth.Geo;

namespace TransparentEarth.Tests
{
    public sealed class GeoJsonLinesTests
    {
        [Test]
        public void ParsesLineStringCoordinatesAsLonLat()
        {
            const string json =
                "{\"features\":[{\"geometry\":{\"type\":\"LineString\"," +
                "\"coordinates\":[[10.0,50.0],[11.5,-5.25]]}}]}";
            var lines = GeoJsonLines.Parse(json);
            Assert.That(lines, Has.Count.EqualTo(1));
            Assert.That(lines[0], Has.Count.EqualTo(2));
            Assert.That(lines[0][0].Longitude, Is.EqualTo(10.0).Within(1e-9));
            Assert.That(lines[0][0].Latitude, Is.EqualTo(50.0).Within(1e-9));
            Assert.That(lines[0][1].Longitude, Is.EqualTo(11.5).Within(1e-9));
            Assert.That(lines[0][1].Latitude, Is.EqualTo(-5.25).Within(1e-9));
        }

        [Test]
        public void SplitsMultiLineStringIntoSeparatePaths()
        {
            const string json =
                "{\"features\":[{\"geometry\":{\"type\":\"MultiLineString\"," +
                "\"coordinates\":[[[0,0],[1,1]],[[2,2],[3,3],[4,4]]]}}]}";
            var lines = GeoJsonLines.Parse(json);
            Assert.That(lines, Has.Count.EqualTo(2));
            Assert.That(lines[0], Has.Count.EqualTo(2));
            Assert.That(lines[1], Has.Count.EqualTo(3));
        }

        [Test]
        public void DropsDegeneratePathsAndHandlesEmptyInput()
        {
            Assert.That(GeoJsonLines.Parse(null), Is.Empty);
            Assert.That(GeoJsonLines.Parse("{\"features\":[]}"), Is.Empty);
        }

        [Test]
        public void ProjectsCartographyPathsIntoTheUnitDisc()
        {
            const string json =
                "{\"features\":[{\"geometry\":{\"type\":\"LineString\"," +
                "\"coordinates\":[[0.0,90.0],[0.0,0.0]]}}]}";
            var carto = FlatEarthCartography.FromGeoJson(json, null);
            Assert.That(carto.Coastlines, Has.Count.EqualTo(1));
            Assert.That(carto.Coastlines[0][0].magnitude, Is.EqualTo(0f).Within(1e-5f)); // North Pole
            Assert.That(carto.Coastlines[0][1].magnitude, Is.EqualTo(.5f).Within(1e-5f)); // Equator
        }
    }
}
