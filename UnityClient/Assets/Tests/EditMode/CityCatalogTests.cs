using System.Linq;
using NUnit.Framework;
using TransparentEarth.Data;

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
    }
}
