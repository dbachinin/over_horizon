using System.Collections.Generic;
using UnityEngine;

namespace TransparentEarth.I18n
{
    public enum TextKey
    {
        LookThroughHorizon, OtherSideOfEarth, PhysicalHorizon, Below, OnHorizon, Nearby,
        Equator, TropicCancer, TropicCapricorn, Greenwich, DateLine,
        LoadingMap, MapUnavailable, ExactAntipode, ThroughEarthDistance, FlagSaved,
        TransparentEarth, BeyondHorizon, On, Off, GlobeLayers, Grid, Continents, Countries, References,
        Overview, Antipode, Map, Places, Profile, Real, Live, Demo, Kilometers
    }

    public static class AppText
    {
        private static readonly IReadOnlyDictionary<TextKey, string> English = new Dictionary<TextKey, string>
        {
            [TextKey.LookThroughHorizon] = "Look through the horizon",
            [TextKey.OtherSideOfEarth] = "The other side of Earth",
            [TextKey.PhysicalHorizon] = "PHYSICAL HORIZON",
            [TextKey.Below] = "BELOW",
            [TextKey.OnHorizon] = "ON THE HORIZON",
            [TextKey.Nearby] = "NEARBY",
            [TextKey.Equator] = "EQUATOR · 0°",
            [TextKey.TropicCancer] = "TROPIC OF CANCER · 23.4° N",
            [TextKey.TropicCapricorn] = "TROPIC OF CAPRICORN · 23.4° S",
            [TextKey.Greenwich] = "GREENWICH · 0°",
            [TextKey.DateLine] = "DATE LINE · 180°",
            [TextKey.LoadingMap] = "LOADING MAP…",
            [TextKey.MapUnavailable] = "MAP UNAVAILABLE",
            [TextKey.ExactAntipode] = "EXACT ANTIPODE",
            [TextKey.ThroughEarthDistance] = "DISTANCE THROUGH EARTH",
            [TextKey.FlagSaved] = "flag saved on the sphere",
            [TextKey.TransparentEarth] = "Transparent Earth",
            [TextKey.BeyondHorizon] = "Objects beyond the horizon",
            [TextKey.On] = "ON",
            [TextKey.Off] = "OFF",
            [TextKey.GlobeLayers] = "GLOBE LAYERS",
            [TextKey.Grid] = "GRID",
            [TextKey.Continents] = "CONTINENTS",
            [TextKey.Countries] = "COUNTRIES",
            [TextKey.References] = "REFERENCES",
            [TextKey.Overview] = "OVERVIEW",
            [TextKey.Antipode] = "ANTIPODE",
            [TextKey.Map] = "MAP",
            [TextKey.Places] = "PLACES",
            [TextKey.Profile] = "PROFILE",
            [TextKey.Real] = "REAL",
            [TextKey.Live] = "LIVE",
            [TextKey.Demo] = "DEMO",
            [TextKey.Kilometers] = "KM"
        };

        private static readonly IReadOnlyDictionary<TextKey, string> Russian = new Dictionary<TextKey, string>
        {
            [TextKey.LookThroughHorizon] = "Смотрите сквозь горизонт",
            [TextKey.OtherSideOfEarth] = "Другая сторона Земли",
            [TextKey.PhysicalHorizon] = "ФИЗИЧЕСКИЙ ГОРИЗОНТ",
            [TextKey.Below] = "НИЖЕ",
            [TextKey.OnHorizon] = "НА ГОРИЗОНТЕ",
            [TextKey.Nearby] = "РЯДОМ",
            [TextKey.Equator] = "ЭКВАТОР · 0°",
            [TextKey.TropicCancer] = "ТРОПИК РАКА · 23.4° N",
            [TextKey.TropicCapricorn] = "ТРОПИК КОЗЕРОГА · 23.4° S",
            [TextKey.Greenwich] = "ГРИНВИЧ · 0°",
            [TextKey.DateLine] = "ЛИНИЯ ДАТ · 180°",
            [TextKey.LoadingMap] = "ЗАГРУЗКА КАРТЫ…",
            [TextKey.MapUnavailable] = "КАРТА НЕДОСТУПНА",
            [TextKey.ExactAntipode] = "ТОЧНЫЙ АНТИПОД",
            [TextKey.ThroughEarthDistance] = "РАССТОЯНИЕ СКВОЗЬ ЗЕМЛЮ",
            [TextKey.FlagSaved] = "флажок сохранён на сфере",
            [TextKey.TransparentEarth] = "Прозрачная Земля",
            [TextKey.BeyondHorizon] = "Объекты за линией горизонта",
            [TextKey.On] = "ВКЛ",
            [TextKey.Off] = "ВЫКЛ",
            [TextKey.GlobeLayers] = "СЛОИ ГЛОБУСА",
            [TextKey.Grid] = "СЕТКА",
            [TextKey.Continents] = "МАТЕРИКИ",
            [TextKey.Countries] = "СТРАНЫ",
            [TextKey.References] = "ОПОРНЫЕ",
            [TextKey.Overview] = "ОБЗОР",
            [TextKey.Antipode] = "АНТИПОД",
            [TextKey.Map] = "КАРТА",
            [TextKey.Places] = "МЕСТА",
            [TextKey.Profile] = "ПРОФИЛЬ",
            [TextKey.Real] = "РЕАЛ",
            [TextKey.Live] = "LIVE",
            [TextKey.Demo] = "ДЕМО",
            [TextKey.Kilometers] = "КМ"
        };

        private static readonly IReadOnlyDictionary<TextKey, string> Serbian = new Dictionary<TextKey, string>
        {
            [TextKey.LookThroughHorizon] = "Pogled kroz horizont",
            [TextKey.OtherSideOfEarth] = "Druga strana Zemlje",
            [TextKey.PhysicalHorizon] = "FIZIČKI HORIZONT",
            [TextKey.Below] = "ISPOD",
            [TextKey.OnHorizon] = "NA HORIZONTU",
            [TextKey.Nearby] = "U BLIZINI",
            [TextKey.Equator] = "EKVATOR · 0°",
            [TextKey.TropicCancer] = "SEVERNI POVRATNIK · 23.4° N",
            [TextKey.TropicCapricorn] = "JUŽNI POVRATNIK · 23.4° S",
            [TextKey.Greenwich] = "GRINIČ · 0°",
            [TextKey.DateLine] = "DATUMSKA GRANICA · 180°",
            [TextKey.LoadingMap] = "UČITAVANJE MAPE…",
            [TextKey.MapUnavailable] = "MAPA NIJE DOSTUPNA",
            [TextKey.ExactAntipode] = "TAČNI ANTIPOD",
            [TextKey.ThroughEarthDistance] = "UDALJENOST KROZ ZEMLJU",
            [TextKey.FlagSaved] = "zastavica je sačuvana na sferi",
            [TextKey.TransparentEarth] = "Prozirna Zemlja",
            [TextKey.BeyondHorizon] = "Objekti iza horizonta",
            [TextKey.On] = "UKLJ",
            [TextKey.Off] = "ISKLJ",
            [TextKey.GlobeLayers] = "SLOJEVI GLOBUSA",
            [TextKey.Grid] = "MREŽA",
            [TextKey.Continents] = "KONTINENTI",
            [TextKey.Countries] = "DRŽAVE",
            [TextKey.References] = "REFERENTNE",
            [TextKey.Overview] = "PREGLED",
            [TextKey.Antipode] = "ANTIPOD",
            [TextKey.Map] = "MAPA",
            [TextKey.Places] = "MESTA",
            [TextKey.Profile] = "PROFIL",
            [TextKey.Real] = "STVARNO",
            [TextKey.Live] = "UŽIVO",
            [TextKey.Demo] = "DEMO",
            [TextKey.Kilometers] = "KM"
        };

        public static string Get(TextKey key) => GetForLanguage(key, Application.systemLanguage);

        public static string GetForLanguage(TextKey key, SystemLanguage language)
        {
            var table = language switch
            {
                SystemLanguage.Russian => Russian,
                SystemLanguage.SerboCroatian => Serbian,
                _ => English
            };
            return table.TryGetValue(key, out var value) ? value : English[key];
        }
    }
}
