using System;
using NUnit.Framework;
using TransparentEarth.I18n;
using UnityEngine;

namespace TransparentEarth.Tests
{
    public sealed class AppTextTests
    {
        [TestCase(SystemLanguage.English, TextKey.LookThroughHorizon, "Look through the horizon")]
        [TestCase(SystemLanguage.Russian, TextKey.LookThroughHorizon, "Смотрите сквозь горизонт")]
        [TestCase(SystemLanguage.SerboCroatian, TextKey.LookThroughHorizon, "Pogled kroz horizont")]
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
    }
}
