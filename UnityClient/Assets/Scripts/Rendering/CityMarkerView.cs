using TransparentEarth.Data;
using TransparentEarth.Geo;
using UnityEngine;

namespace TransparentEarth.Rendering
{
    public sealed class CityMarkerView
    {
        public readonly City City;
        public readonly GeoProjection Projection;
        public readonly Transform Anchor;
        public readonly Transform Flag;
        public readonly Color Accent;

        public CityMarkerView(City city, GeoProjection projection, Transform anchor, Transform flag = null)
        {
            City = city;
            Projection = projection;
            Anchor = anchor;
            Flag = flag;
            Accent = projection.ElevationDegrees < -35 ? TransparentEarthStyle.Signal : TransparentEarthStyle.Mint;
        }
    }
}
