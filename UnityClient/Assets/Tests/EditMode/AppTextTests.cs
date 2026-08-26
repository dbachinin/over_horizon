using System;
using NUnit.Framework;
using TransparentEarth.I18n;
using UnityEngine;

namespace TransparentEarth.Tests
{
    public sealed class AppTextTests
    {
        [TestCase(SystemLanguage.English, TextKey.LookThroughHorizon, "View through Earth")]
        [TestCase(SystemLanguage.Russian, TextKey.LookThroughHorizon, "Вид сквозь Землю")]
        [TestCase(SystemLanguage.SerboCroatian, TextKey.LookThroughHorizon, "Pogled kroz Zemlju")]
        [TestCase(SystemLanguage.English, TextKey.TransparentEarth, "Display controls")]
        [TestCase(SystemLanguage.Russian, TextKey.TransparentEarth, "Управление отображением")]
        [TestCase(SystemLanguage.SerboCroatian, TextKey.TransparentEarth, "Upravljanje prikazom")]
        public void ResolvesSupportedLanguage(SystemLanguage language, TextKey key, string expected)
        {
            Assert.That(AppText.GetForLanguage(key, language), Is.EqualTo(expected));
        }

        [TestCase(SystemLanguage.English)]
        [TestCase(SystemLanguage.Russian)]
        [TestCase(SystemLanguage.SerboCroatian)]
        public void EveryKeyHasTranslation(SystemLanguage language)
        {
            foreach (TextKey key in Enum.GetValues(typeof(TextKey)))
                Assert.That(AppText.GetForLanguage(key, language), Is.Not.Empty, key.ToString());
        }

        [Test]
        public void UnsupportedLanguageFallsBackToEnglish()
        {
            Assert.That(AppText.GetForLanguage(TextKey.Map, SystemLanguage.Japanese), Is.EqualTo("MAP"));
        }

        [TestCase(SystemLanguage.English, 359d, "N")]
        [TestCase(SystemLanguage.English, 45d, "NE")]
        [TestCase(SystemLanguage.Russian, 180d, "Ю")]
        [TestCase(SystemLanguage.SerboCroatian, 90d, "I")]
        public void LocalizesCardinalDirection(SystemLanguage language, double bearing, string expected)
        {
            Assert.That(AppText.CardinalDirectionForLanguage(bearing, language), Is.EqualTo(expected));
        }
    }
}
