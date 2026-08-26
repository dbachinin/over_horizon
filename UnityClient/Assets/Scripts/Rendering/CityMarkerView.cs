using TransparentEarth.Data;
using TransparentEarth.Geo;
using UnityEngine;

namespace TransparentEarth.Rendering
{
    public sealed class CityMarkerView
    {
        private const double NearbyLeaderRadiusKm = 30d;

        public readonly City City;
        public GeoProjection Projection;
        public readonly Transform Anchor;
        public readonly Transform Flag;
        public readonly bool IsNearbySettlement;
        private float _reveal;

        public Color Accent => TransparentEarthStyle.BlueprintGold;
        public bool IsNearby => IsNearbySettlement || Projection.DistanceKm <= NearbyLeaderRadiusKm;
        public bool HasLeaderLine => Projection.DistanceKm <= GeoMath.OneThirdCircumferenceKm;

        public static float ConstrainToEarthSide(float markerScreenY, float horizonScreenY, float gapPixels) =>
            Mathf.Min(markerScreenY, horizonScreenY - Mathf.Max(0f, gapPixels));

        public CityMarkerView(City city, GeoProjection projection, Transform anchor, bool isNearbySettlement,
            Transform flag = null)
        {
            City = city;
            Projection = projection;
            Anchor = anchor;
            Flag = flag;
            IsNearbySettlement = isNearbySettlement;
        }

        public float AccumulateReveal(float waveReveal)
        {
            _reveal = Mathf.Max(_reveal, Mathf.Clamp01(waveReveal));
            return _reveal;
        }
    }
}
