using System.Collections.Generic;
using TransparentEarth.Geo;

namespace TransparentEarth.Data
{
    public readonly struct City
    {
        public readonly string Name;
        public readonly string Country;
        public readonly GeoPoint Position;
        public readonly int Importance;

        public City(string name, string country, double latitude, double longitude, int importance)
        {
            Name = name;
            Country = country;
            Position = new GeoPoint(latitude, longitude);
            Importance = importance;
        }
    }

    public static class CityCatalog
    {
        public static readonly IReadOnlyList<City> All = new[]
        {
            new City("Novi Sad", "RS", 45.2671, 19.8335, 70),
            new City("Belgrade", "RS", 44.8125, 20.4612, 91),
            new City("Budapest", "HU", 47.4979, 19.0402, 88),
            new City("Sarajevo", "BA", 43.8563, 18.4131, 76),
            new City("Vienna", "AT", 48.2082, 16.3738, 90),
            new City("Athens", "GR", 37.9838, 23.7275, 90),
            new City("Berlin", "DE", 52.5200, 13.4050, 96),
            new City("London", "GB", 51.5074, -0.1278, 100),
            new City("Paris", "FR", 48.8566, 2.3522, 100),
            new City("Madrid", "ES", 40.4168, -3.7038, 95),
            new City("Rome", "IT", 41.9028, 12.4964, 96),
            new City("Istanbul", "TR", 41.0082, 28.9784, 99),
            new City("Moscow", "RU", 55.7558, 37.6173, 98),
            new City("Cairo", "EG", 30.0444, 31.2357, 98),
            new City("Lagos", "NG", 6.5244, 3.3792, 96),
            new City("Nairobi", "KE", -1.2921, 36.8219, 90),
            new City("Cape Town", "ZA", -33.9249, 18.4241, 94),
            new City("Dubai", "AE", 25.2048, 55.2708, 96),
            new City("Delhi", "IN", 28.6139, 77.2090, 100),
            new City("Mumbai", "IN", 19.0760, 72.8777, 99),
            new City("Bangkok", "TH", 13.7563, 100.5018, 97),
            new City("Singapore", "SG", 1.3521, 103.8198, 98),
            new City("Hong Kong", "HK", 22.3193, 114.1694, 98),
            new City("Seoul", "KR", 37.5665, 126.9780, 98),
            new City("Beijing", "CN", 39.9042, 116.4074, 100),
            new City("Shanghai", "CN", 31.2304, 121.4737, 100),
            new City("Tokyo", "JP", 35.6762, 139.6503, 100),
            new City("Jakarta", "ID", -6.2088, 106.8456, 98),
            new City("Manila", "PH", 14.5995, 120.9842, 96),
            new City("Perth", "AU", -31.9523, 115.8613, 87),
            new City("New York", "US", 40.7128, -74.0060, 100),
            new City("Toronto", "CA", 43.6532, -79.3832, 96),
            new City("Chicago", "US", 41.8781, -87.6298, 96),
            new City("Mexico City", "MX", 19.4326, -99.1332, 99),
            new City("Los Angeles", "US", 34.0522, -118.2437, 100),
            new City("San Francisco", "US", 37.7749, -122.4194, 97),
            new City("Vancouver", "CA", 49.2827, -123.1207, 92),
            new City("Honolulu", "US", 21.3069, -157.8583, 84),
            new City("Sydney", "AU", -33.8688, 151.2093, 98),
            new City("Melbourne", "AU", -37.8136, 144.9631, 95),
            new City("Auckland", "NZ", -36.8509, 174.7645, 90),
            new City("Buenos Aires", "AR", -34.6037, -58.3816, 95),
            new City("Santiago", "CL", -33.4489, -70.6693, 94),
            new City("Lima", "PE", -12.0464, -77.0428, 95),
            new City("Bogota", "CO", 4.7110, -74.0721, 96),
            new City("Sao Paulo", "BR", -23.5505, -46.6333, 100),
            new City("Rio de Janeiro", "BR", -22.9068, -43.1729, 97)
        };
    }
}
