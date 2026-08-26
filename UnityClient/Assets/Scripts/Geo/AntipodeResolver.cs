using TransparentEarth.Data;

namespace TransparentEarth.Geo
{
    public readonly struct GeographicObjectDirection
    {
        public readonly NamedGeographicObject Object;
        public readonly GeoProjection Projection;

        public GeographicObjectDirection(NamedGeographicObject geographicObject, GeoProjection projection)
        {
            Object = geographicObject;
            Projection = projection;
        }
    }

    public static class AntipodeResolver
    {
        public static GeographicObjectDirection FindNearestNamedObject(GeoPoint antipode)
        {
            var firstCity = CityCatalog.All[0];
            var nearest = new NamedGeographicObject(firstCity.Name, firstCity.Country,
                firstCity.Position.Latitude, firstCity.Position.Longitude);
            var nearestProjection = GeoMath.Project(antipode, nearest.Position);
            for (var i = 1; i < CityCatalog.All.Count; i++)
            {
                var candidate = CityCatalog.All[i];
                var projection = GeoMath.Project(antipode, candidate.Position);
                if (projection.DistanceKm >= nearestProjection.DistanceKm) continue;
                nearest = new NamedGeographicObject(candidate.Name, candidate.Country,
                    candidate.Position.Latitude, candidate.Position.Longitude);
                nearestProjection = projection;
            }

            foreach (var candidate in GeographicObjectCatalog.All)
            {
                var projection = GeoMath.Project(antipode, candidate.Position);
                if (projection.DistanceKm >= nearestProjection.DistanceKm) continue;
                nearest = candidate;
                nearestProjection = projection;
            }

            return new GeographicObjectDirection(nearest, nearestProjection);
        }
    }
}
