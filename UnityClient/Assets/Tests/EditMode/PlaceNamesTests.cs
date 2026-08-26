using NUnit.Framework;
using TransparentEarth.Data;
using TransparentEarth.I18n;
using UnityEngine;

namespace TransparentEarth.Tests
{
    public sealed class PlaceNamesTests
    {
        [TestCase(SystemLanguage.Russian, "Belgrade", "Белград")]
        [TestCase(SystemLanguage.Russian, "Warsaw", "Варшава")]
        [TestCase(SystemLanguage.SerboCroatian, "Belgrade", "Beograd")]
        [TestCase(SystemLanguage.SerboCroatian, "Vienna", "Beč")]
        [TestCase(SystemLanguage.English, "Moscow", "Moscow")]
        public void ResolvesCityName(SystemLanguage language, string canonical, string expected)
        {
            Assert.That(PlaceNames.GetForLanguage(canonical, language), Is.EqualTo(expected));
        }

        [TestCase(SystemLanguage.Russian)]
        [TestCase(SystemLanguage.SerboCroatian)]
        public void EveryCatalogCityHasTranslation(SystemLanguage language)
        {
            foreach (var city in CityCatalog.All)
                Assert.That(PlaceNames.HasTranslationForLanguage(city.Name, language), Is.True, city.Name);
        }

        [Test]
        public void UnknownOsmPlaceKeepsOriginalName()
        {
            Assert.That(PlaceNames.GetForLanguage("Unknown local settlement", SystemLanguage.Russian),
                Is.EqualTo("Unknown local settlement"));
        }
    }
}
