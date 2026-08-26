using System.Linq;
using NUnit.Framework;
using TransparentEarth.Data;
using TransparentEarth.Rendering;

namespace TransparentEarth.Tests
{
    public sealed class CityCatalogTests
    {
        private static readonly string[] EasternEuropeanCapitals =
        {
            "Belgrade", "Budapest", "Sarajevo", "Ljubljana", "Zagreb", "Podgorica", "Pristina",
            "Skopje", "Tirana", "Sofia", "Bucharest", "Chisinau", "Bratislava", "Prague", "Warsaw",
            "Kyiv", "Minsk", "Vilnius", "Riga", "Tallinn"
        };

        [Test]
        public void ContainsEasternEuropeanCapitals()
        {
            foreach (var capital in EasternEuropeanCapitals)
                Assert.That(CityCatalog.All.Any(city => city.Name == capital), Is.True, capital);
        }

        [Test]
        public void CityKeysAreUnique()
        {
            var keys = CityCatalog.All.Select(city => city.Country + "|" + city.Name).ToArray();
            Assert.That(keys.Distinct().Count(), Is.EqualTo(keys.Length));
        }

        [Test]
        public void SameNameAndCountryIdentifiesSavedPlace()
        {
            var left = new City("Springfield", "US", 39.78, -89.64, 60);
            var right = new City("Springfield", "US", 44.05, -123.02, 60);
            Assert.That(GeoObjectStreamer.AreSamePlace(left, right), Is.True);
        }

        [Test]
        public void SameNameInDifferentCountriesRemainsDistinct()
        {
            var left = new City("San Jose", "US", 37.34, -121.89, 60);
            var right = new City("San Jose", "CR", 9.93, -84.08, 60);
            Assert.That(GeoObjectStreamer.AreSamePlace(left, right), Is.False);
        }

        [Test]
        public void NearbyCoordinatesIdentifySamePlaceAcrossLocalizedNames()
        {
            var left = new City("Kazan", "RU", 55.7946, 49.1115, 80);
            var right = new City("Казань", "RU", 55.7950, 49.1120, 80);
            Assert.That(GeoObjectStreamer.AreSamePlace(left, right), Is.True);
        }
    }
}
