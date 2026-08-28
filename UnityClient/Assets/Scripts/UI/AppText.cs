using System.Collections.Generic;
using UnityEngine;

namespace TransparentEarth.I18n
{
    public enum TextKey
    {
        LookThroughHorizon, OtherSideOfEarth, PhysicalHorizon, Below, OnHorizon, Nearby,
        Equator, TropicCancer, TropicCapricorn, Greenwich, DateLine,
        LoadingMap, MapUnavailable, ExactAntipode, ThroughEarthDistance, FlagSaved,
        AntipodePoint, NearestGeographicObject, Direction, SurfaceDistance,
        TransparentEarth, BeyondHorizon, On, Off, GlobeLayers, Grid, Continents, Countries, References,
        Overview, Antipode, Map, Places, Profile, Real, Live, Demo, Kilometers,
        PlaceSearchTitle, PlaceSearchHint, Search, SearchResults, AddPlace, Added,
        NoPlacesFound, SearchUnavailable, SavedPlaces, PrivacyOptions,
        FlatEarthMode, FlatEarthTitle, FlatEarthTagline, Subscribe, SubscriptionTerms,
        RestorePurchase, ManageSubscription, StoreConnecting, PurchasePending, PurchaseFailed,
        BackToGlobe, Azimuth, IceWall, NorthPoleHub,
        OneHorizonLine, WhereToLook, SecretInitiate, Gaze
    }

    public static class AppText
    {
        private static readonly IReadOnlyDictionary<TextKey, string> English = new Dictionary<TextKey, string>
        {
            [TextKey.LookThroughHorizon] = "View through Earth",
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
            [TextKey.AntipodePoint] = "ANTIPODE POINT",
            [TextKey.NearestGeographicObject] = "NEAREST GEOGRAPHIC OBJECT",
            [TextKey.Direction] = "DIRECTION",
            [TextKey.SurfaceDistance] = "SURFACE DISTANCE",
            [TextKey.TransparentEarth] = "Display controls",
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
            [TextKey.Kilometers] = "KM",
            [TextKey.PlaceSearchTitle] = "Find a city or settlement",
            [TextKey.PlaceSearchHint] = "City, town or village",
            [TextKey.Search] = "SEARCH",
            [TextKey.SearchResults] = "SEARCH RESULTS",
            [TextKey.AddPlace] = "ADD",
            [TextKey.Added] = "ADDED",
            [TextKey.NoPlacesFound] = "NOTHING FOUND",
            [TextKey.SearchUnavailable] = "SEARCH UNAVAILABLE",
            [TextKey.SavedPlaces] = "SAVED PLACES",
            [TextKey.PrivacyOptions] = "PRIVACY",
            [TextKey.FlatEarthMode] = "FLAT EARTHER MODE",
            [TextKey.FlatEarthTitle] = "Flat Earth Mode",
            [TextKey.FlatEarthTagline] =
                "The sun, the moon and the four winds. Every city pinned to one honest line, " +
                "the South Pole stretched round the ice wall, and a true bearing to steer by.",
            [TextKey.Subscribe] = "SUBSCRIBE",
            [TextKey.SubscriptionTerms] = "Monthly auto-renewing subscription. Cancel anytime in Google Play.",
            [TextKey.RestorePurchase] = "RESTORE PURCHASE",
            [TextKey.ManageSubscription] = "MANAGE SUBSCRIPTION",
            [TextKey.StoreConnecting] = "CONNECTING TO GOOGLE PLAY…",
            [TextKey.PurchasePending] = "CONSULTING THE GUILD…",
            [TextKey.PurchaseFailed] = "THE BARGAIN FELL THROUGH",
            [TextKey.BackToGlobe] = "BACK TO THE GLOBE",
            [TextKey.Azimuth] = "AZ",
            [TextKey.IceWall] = "ICE WALL · SOUTH POLE",
            [TextKey.NorthPoleHub] = "NORTH POLE",
            [TextKey.OneHorizonLine] = "EVERY CITY ON ONE LINE",
            [TextKey.WhereToLook] = "TAP A CITY TO TAKE ITS BEARING",
            [TextKey.SecretInitiate] =
                "You now hold the great secret the scientists keep hidden from the world.",
            [TextKey.Gaze] = "YOUR GAZE"
        };

        private static readonly IReadOnlyDictionary<TextKey, string> Russian = new Dictionary<TextKey, string>
        {
            [TextKey.LookThroughHorizon] = "Вид сквозь Землю",
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
            [TextKey.AntipodePoint] = "ТОЧКА АНТИПОДА",
            [TextKey.NearestGeographicObject] = "БЛИЖАЙШИЙ ГЕОГРАФИЧЕСКИЙ ОБЪЕКТ",
            [TextKey.Direction] = "НАПРАВЛЕНИЕ",
            [TextKey.SurfaceDistance] = "ПО ПОВЕРХНОСТИ",
            [TextKey.TransparentEarth] = "Управление отображением",
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
            [TextKey.Kilometers] = "КМ",
            [TextKey.PlaceSearchTitle] = "Поиск города или поселения",
            [TextKey.PlaceSearchHint] = "Город, посёлок или деревня",
            [TextKey.Search] = "НАЙТИ",
            [TextKey.SearchResults] = "РЕЗУЛЬТАТЫ ПОИСКА",
            [TextKey.AddPlace] = "ДОБАВИТЬ",
            [TextKey.Added] = "ДОБАВЛЕНО",
            [TextKey.NoPlacesFound] = "НИЧЕГО НЕ НАЙДЕНО",
            [TextKey.SearchUnavailable] = "ПОИСК НЕДОСТУПЕН",
            [TextKey.SavedPlaces] = "СОХРАНЁННЫЕ МЕСТА",
            [TextKey.PrivacyOptions] = "КОНФИДЕНЦИАЛЬНОСТЬ",
            [TextKey.FlatEarthMode] = "РЕЖИМ ПЛОСКОЗЕМЕЛЬЩИКА",
            [TextKey.FlatEarthTitle] = "Режим плоской земли",
            [TextKey.FlatEarthTagline] =
                "Солнце, луна и четыре ветра. Все города — на одной честной линии, " +
                "Южный полюс растянут по ледяной стене, а азимут укажет курс.",
            [TextKey.Subscribe] = "ПОДПИСАТЬСЯ",
            [TextKey.SubscriptionTerms] =
                "Ежемесячная подписка с автопродлением. Отменить можно в Google Play.",
            [TextKey.RestorePurchase] = "ВОССТАНОВИТЬ ПОКУПКУ",
            [TextKey.ManageSubscription] = "УПРАВЛЕНИЕ ПОДПИСКОЙ",
            [TextKey.StoreConnecting] = "ПОДКЛЮЧЕНИЕ К GOOGLE PLAY…",
            [TextKey.PurchasePending] = "СОВЕТ С ГИЛЬДИЕЙ…",
            [TextKey.PurchaseFailed] = "СДЕЛКА СОРВАЛАСЬ",
            [TextKey.BackToGlobe] = "ВЕРНУТЬСЯ К ШАРУ",
            [TextKey.Azimuth] = "АЗ",
            [TextKey.IceWall] = "ЛЕДЯНАЯ СТЕНА · ЮЖНЫЙ ПОЛЮС",
            [TextKey.NorthPoleHub] = "СЕВЕРНЫЙ ПОЛЮС",
            [TextKey.OneHorizonLine] = "ВСЕ ГОРОДА НА ОДНОЙ ЛИНИИ",
            [TextKey.WhereToLook] = "КОСНИТЕСЬ ГОРОДА, ЧТОБЫ ВЗЯТЬ АЗИМУТ",
            [TextKey.SecretInitiate] =
                "Отныне вы — обладатель великой тайны, которую учёные скрывают от мира.",
            [TextKey.Gaze] = "ВАШ ВЗГЛЯД"
        };

        private static readonly IReadOnlyDictionary<TextKey, string> Serbian = new Dictionary<TextKey, string>
        {
            [TextKey.LookThroughHorizon] = "Pogled kroz Zemlju",
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
            [TextKey.AntipodePoint] = "TAČKA ANTIPODA",
            [TextKey.NearestGeographicObject] = "NAJBLIŽI GEOGRAFSKI OBJEKAT",
            [TextKey.Direction] = "PRAVAC",
            [TextKey.SurfaceDistance] = "PO POVRŠINI",
            [TextKey.TransparentEarth] = "Upravljanje prikazom",
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
            [TextKey.Kilometers] = "KM",
            [TextKey.PlaceSearchTitle] = "Pretraga grada ili naselja",
            [TextKey.PlaceSearchHint] = "Grad, varoš ili selo",
            [TextKey.Search] = "TRAŽI",
            [TextKey.SearchResults] = "REZULTATI PRETRAGE",
            [TextKey.AddPlace] = "DODAJ",
            [TextKey.Added] = "DODATO",
            [TextKey.NoPlacesFound] = "NEMA REZULTATA",
            [TextKey.SearchUnavailable] = "PRETRAGA NIJE DOSTUPNA",
            [TextKey.SavedPlaces] = "SAČUVANA MESTA",
            [TextKey.PrivacyOptions] = "PRIVATNOST",
            [TextKey.FlatEarthMode] = "REŽIM RAVNOZEMLJAŠA",
            [TextKey.FlatEarthTitle] = "Režim ravne zemlje",
            [TextKey.FlatEarthTagline] =
                "Sunce, mesec i četiri vetra. Svaki grad na jednoj poštenoj liniji, " +
                "Južni pol razvučen po ledenom zidu, a azimut da drži kurs.",
            [TextKey.Subscribe] = "PRETPLATI SE",
            [TextKey.SubscriptionTerms] =
                "Mesečna pretplata sa automatskom obnovom. Otkažite bilo kada na Google Play-u.",
            [TextKey.RestorePurchase] = "OBNOVI KUPOVINU",
            [TextKey.ManageSubscription] = "UPRAVLJAJ PRETPLATOM",
            [TextKey.StoreConnecting] = "POVEZIVANJE SA GOOGLE PLAY-OM…",
            [TextKey.PurchasePending] = "DOGOVOR SA ESNAFOM…",
            [TextKey.PurchaseFailed] = "POGODBA JE PROPALA",
            [TextKey.BackToGlobe] = "NAZAD NA GLOBUS",
            [TextKey.Azimuth] = "AZ",
            [TextKey.IceWall] = "LEDENI ZID · JUŽNI POL",
            [TextKey.NorthPoleHub] = "SEVERNI POL",
            [TextKey.OneHorizonLine] = "SVI GRADOVI NA JEDNOJ LINIJI",
            [TextKey.WhereToLook] = "DODIRNITE GRAD ZA AZIMUT",
            [TextKey.SecretInitiate] =
                "Od sada ste čuvar velike tajne koju naučnici kriju od sveta.",
            [TextKey.Gaze] = "VAŠ POGLED"
        };

        public static string Get(TextKey key) => GetForLanguage(key, Application.systemLanguage);

        public static string CardinalDirection(double bearingDegrees) =>
            CardinalDirectionForLanguage(bearingDegrees, Application.systemLanguage);

        public static string CardinalDirectionForLanguage(double bearingDegrees, SystemLanguage language)
        {
            var directions = language switch
            {
                SystemLanguage.Russian => new[] { "С", "СВ", "В", "ЮВ", "Ю", "ЮЗ", "З", "СЗ" },
                SystemLanguage.SerboCroatian => new[] { "S", "SI", "I", "JI", "J", "JZ", "Z", "SZ" },
                _ => new[] { "N", "NE", "E", "SE", "S", "SW", "W", "NW" }
            };
            var normalized = Mathf.Repeat((float)bearingDegrees, 360f);
            var index = Mathf.FloorToInt((normalized + 22.5f) / 45f) % directions.Length;
            return directions[index];
        }

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
