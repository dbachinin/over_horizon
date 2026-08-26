using System;
using System.Collections.Generic;
using UnityEngine;

namespace TransparentEarth.I18n
{
    public static class PlaceNames
    {
        private static readonly IReadOnlyDictionary<string, string> Russian =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Novi Sad"] = "Нови-Сад",
                ["Belgrade"] = "Белград",
                ["Budapest"] = "Будапешт",
                ["Sarajevo"] = "Сараево",
                ["Ljubljana"] = "Любляна",
                ["Zagreb"] = "Загреб",
                ["Podgorica"] = "Подгорица",
                ["Pristina"] = "Приштина",
                ["Skopje"] = "Скопье",
                ["Tirana"] = "Тирана",
                ["Sofia"] = "София",
                ["Bucharest"] = "Бухарест",
                ["Chisinau"] = "Кишинёв",
                ["Bratislava"] = "Братислава",
                ["Prague"] = "Прага",
                ["Warsaw"] = "Варшава",
                ["Kyiv"] = "Киев",
                ["Minsk"] = "Минск",
                ["Vilnius"] = "Вильнюс",
                ["Riga"] = "Рига",
                ["Tallinn"] = "Таллин",
                ["Vienna"] = "Вена",
                ["Athens"] = "Афины",
                ["Berlin"] = "Берлин",
                ["London"] = "Лондон",
                ["Paris"] = "Париж",
                ["Madrid"] = "Мадрид",
                ["Rome"] = "Рим",
                ["Istanbul"] = "Стамбул",
                ["Ankara"] = "Анкара",
                ["Moscow"] = "Москва",
                ["Cairo"] = "Каир",
                ["Lagos"] = "Лагос",
                ["Nairobi"] = "Найроби",
                ["Cape Town"] = "Кейптаун",
                ["Dubai"] = "Дубай",
                ["Delhi"] = "Дели",
                ["Mumbai"] = "Мумбаи",
                ["Bangkok"] = "Бангкок",
                ["Singapore"] = "Сингапур",
                ["Hong Kong"] = "Гонконг",
                ["Seoul"] = "Сеул",
                ["Beijing"] = "Пекин",
                ["Shanghai"] = "Шанхай",
                ["Tokyo"] = "Токио",
                ["Jakarta"] = "Джакарта",
                ["Manila"] = "Манила",
                ["Perth"] = "Перт",
                ["New York"] = "Нью-Йорк",
                ["Toronto"] = "Торонто",
                ["Chicago"] = "Чикаго",
                ["Mexico City"] = "Мехико",
                ["Los Angeles"] = "Лос-Анджелес",
                ["San Francisco"] = "Сан-Франциско",
                ["Vancouver"] = "Ванкувер",
                ["Honolulu"] = "Гонолулу",
                ["Sydney"] = "Сидней",
                ["Melbourne"] = "Мельбурн",
                ["Auckland"] = "Окленд",
                ["Buenos Aires"] = "Буэнос-Айрес",
                ["Santiago"] = "Сантьяго",
                ["Lima"] = "Лима",
                ["Bogota"] = "Богота",
                ["Sao Paulo"] = "Сан-Паулу",
                ["Rio de Janeiro"] = "Рио-де-Жанейро"
            };

        private static readonly IReadOnlyDictionary<string, string> Serbian =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Novi Sad"] = "Novi Sad",
                ["Belgrade"] = "Beograd",
                ["Budapest"] = "Budimpešta",
                ["Sarajevo"] = "Sarajevo",
                ["Ljubljana"] = "Ljubljana",
                ["Zagreb"] = "Zagreb",
                ["Podgorica"] = "Podgorica",
                ["Pristina"] = "Priština",
                ["Skopje"] = "Skoplje",
                ["Tirana"] = "Tirana",
                ["Sofia"] = "Sofija",
                ["Bucharest"] = "Bukurešt",
                ["Chisinau"] = "Kišinjev",
                ["Bratislava"] = "Bratislava",
                ["Prague"] = "Prag",
                ["Warsaw"] = "Varšava",
                ["Kyiv"] = "Kijev",
                ["Minsk"] = "Minsk",
                ["Vilnius"] = "Vilnjus",
                ["Riga"] = "Riga",
                ["Tallinn"] = "Talin",
                ["Vienna"] = "Beč",
                ["Athens"] = "Atina",
                ["Berlin"] = "Berlin",
                ["London"] = "London",
                ["Paris"] = "Pariz",
                ["Madrid"] = "Madrid",
                ["Rome"] = "Rim",
                ["Istanbul"] = "Istanbul",
                ["Ankara"] = "Ankara",
                ["Moscow"] = "Moskva",
                ["Cairo"] = "Kairo",
                ["Lagos"] = "Lagos",
                ["Nairobi"] = "Najrobi",
                ["Cape Town"] = "Kejptaun",
                ["Dubai"] = "Dubai",
                ["Delhi"] = "Delhi",
                ["Mumbai"] = "Mumbaj",
                ["Bangkok"] = "Bangkok",
                ["Singapore"] = "Singapur",
                ["Hong Kong"] = "Hongkong",
                ["Seoul"] = "Seul",
                ["Beijing"] = "Peking",
                ["Shanghai"] = "Šangaj",
                ["Tokyo"] = "Tokio",
                ["Jakarta"] = "Džakarta",
                ["Manila"] = "Manila",
                ["Perth"] = "Pert",
                ["New York"] = "Njujork",
                ["Toronto"] = "Toronto",
                ["Chicago"] = "Čikago",
                ["Mexico City"] = "Meksiko Siti",
                ["Los Angeles"] = "Los Anđeles",
                ["San Francisco"] = "San Francisko",
                ["Vancouver"] = "Vankuver",
                ["Honolulu"] = "Honolulu",
                ["Sydney"] = "Sidnej",
                ["Melbourne"] = "Melburn",
                ["Auckland"] = "Okland",
                ["Buenos Aires"] = "Buenos Ajres",
                ["Santiago"] = "Santjago",
                ["Lima"] = "Lima",
                ["Bogota"] = "Bogota",
                ["Sao Paulo"] = "Sao Paulo",
                ["Rio de Janeiro"] = "Rio de Žaneiro"
            };

        public static string Get(string canonicalName) =>
            GetForLanguage(canonicalName, Application.systemLanguage);

        public static string GetForLanguage(string canonicalName, SystemLanguage language)
        {
            if (string.IsNullOrWhiteSpace(canonicalName)) return canonicalName;
            var table = language switch
            {
                SystemLanguage.Russian => Russian,
                SystemLanguage.SerboCroatian => Serbian,
                _ => null
            };
            return table != null && table.TryGetValue(canonicalName, out var localized)
                ? localized
                : canonicalName;
        }

        public static bool HasTranslationForLanguage(string canonicalName, SystemLanguage language) =>
            language switch
            {
                SystemLanguage.Russian => Russian.ContainsKey(canonicalName),
                SystemLanguage.SerboCroatian => Serbian.ContainsKey(canonicalName),
                _ => true
            };
    }
}
