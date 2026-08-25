using UnityEngine;

namespace TransparentEarth.Rendering
{
    public static class TransparentEarthStyle
    {
        public static readonly Color Ink = Hex("07110F");
        public static readonly Color Panel = Hex("12211D");
        public static readonly Color Mint = Hex("9DF6D2");
        public static readonly Color Signal = Hex("FFD66D");
        public static readonly Color Muted = Hex("91A79F");
        public static readonly Color Line = Hex("29463E");

        private static Color Hex(string value)
        {
            ColorUtility.TryParseHtmlString("#" + value, out var color);
            return color;
        }
    }
}
