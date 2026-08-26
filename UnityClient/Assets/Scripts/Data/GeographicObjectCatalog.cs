using System.Collections.Generic;
using TransparentEarth.Geo;

namespace TransparentEarth.Data
{
    public readonly struct NamedGeographicObject
    {
        public readonly string Name;
        public readonly string Country;
        public readonly GeoPoint Position;

        public NamedGeographicObject(string name, string country, double latitude, double longitude)
        {
            Name = name;
            Country = country;
            Position = new GeoPoint(latitude, longitude);
        }
    }

    public static class GeographicObjectCatalog
    {
        // Remote landmarks complement the city catalog where antipodes commonly fall in an ocean.
        public static readonly IReadOnlyList<NamedGeographicObject> All = new[]
        {
            new NamedGeographicObject("Chatham Islands", "NZ", -43.9500, -176.5500),
            new NamedGeographicObject("Antipodes Islands", "NZ", -49.6800, 178.7700),
            new NamedGeographicObject("Bounty Islands", "NZ", -47.7500, 179.0500),
            new NamedGeographicObject("Campbell Island", "NZ", -52.5400, 169.1400),
            new NamedGeographicObject("Kerguelen Islands", "FR", -49.3500, 69.2200),
            new NamedGeographicObject("Crozet Islands", "FR", -46.4200, 51.7500),
            new NamedGeographicObject("Amsterdam Island", "FR", -37.8300, 77.5500),
            new NamedGeographicObject("Tristan da Cunha", "SH", -37.1100, -12.2800),
            new NamedGeographicObject("Saint Helena", "SH", -15.9700, -5.7200),
            new NamedGeographicObject("Ascension Island", "SH", -7.9500, -14.3600),
            new NamedGeographicObject("South Georgia", "GS", -54.4300, -36.5900),
            new NamedGeographicObject("Falkland Islands", "FK", -51.7500, -59.0000),
            new NamedGeographicObject("Easter Island", "CL", -27.1200, -109.3500),
            new NamedGeographicObject("Galapagos Islands", "EC", -0.9500, -90.9700),
            new NamedGeographicObject("Marquesas Islands", "PF", -9.4500, -139.3900),
            new NamedGeographicObject("Tahiti", "PF", -17.6500, -149.4300),
            new NamedGeographicObject("Samoa", "WS", -13.7600, -172.1000),
            new NamedGeographicObject("Fiji", "FJ", -17.7100, 178.0700),
            new NamedGeographicObject("New Caledonia", "NC", -21.3000, 165.5000),
            new NamedGeographicObject("Hawaiian Islands", "US", 20.8000, -156.3300),
            new NamedGeographicObject("Midway Atoll", "US", 28.2100, -177.3800),
            new NamedGeographicObject("Aleutian Islands", "US", 52.2000, -174.2000),
            new NamedGeographicObject("Bermuda", "BM", 32.3100, -64.7500),
            new NamedGeographicObject("Azores", "PT", 37.7400, -25.6700),
            new NamedGeographicObject("Madeira", "PT", 32.7600, -16.9600),
            new NamedGeographicObject("Canary Islands", "ES", 28.2900, -16.6300),
            new NamedGeographicObject("Cape Verde", "CV", 15.1200, -23.6200),
            new NamedGeographicObject("Svalbard", "NO", 78.2200, 15.6300)
        };
    }
}
