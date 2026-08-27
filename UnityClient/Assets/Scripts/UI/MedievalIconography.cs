using System.Collections.Generic;
using UnityEngine;

namespace TransparentEarth.UI
{
    /// <summary>
    /// Procedurally drawn art for the "Flat Earther" mode, kept in the same all-code style as the
    /// rest of the overlay (no imported sprites). Everything is sepia ink on parchment: a sun and a
    /// moon with faces, the heads of the four winds, a compass rose and the arse-with-a-horn button
    /// that opens the mode.
    /// </summary>
    public static class MedievalIconography
    {
        public static readonly Color Parchment = new(.87f, .78f, .58f, 1f);
        public static readonly Color ParchmentDeep = new(.78f, .66f, .45f, 1f);
        public static readonly Color Ink = new(.24f, .16f, .09f, 1f);
        public static readonly Color Gilt = new(.72f, .52f, .18f, 1f);
        public static readonly Color Blood = new(.55f, .18f, .12f, 1f);
        // The map keeps the sea as bare vellum and inks the land, in the manner of a mappa mundi.
        public static readonly Color Ocean = new(.85f, .76f, .56f, 1f);
        public static readonly Color Land = new(.45f, .55f, .58f, 1f);
        public static readonly Color BorderInk = new(.16f, .22f, .24f, .55f);

        public static Texture2D ParchmentSheet(int size = 128)
        {
            var texture = NewTexture(size);
            var pixels = new Color32[size * size];
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                var n = Mathf.PerlinNoise(x * .09f, y * .09f) * .5f + Mathf.PerlinNoise(x * .31f, y * .27f) * .5f;
                var grain = Mathf.PerlinNoise(x * .9f, y * .9f);
                var shade = Color.Lerp(ParchmentDeep, Parchment, n * .7f + grain * .3f);
                var cx = (x / (float)size - .5f) * 2f;
                var cy = (y / (float)size - .5f) * 2f;
                var vignette = Mathf.Clamp01(1f - (cx * cx + cy * cy) * .35f);
                shade *= Mathf.Lerp(.82f, 1f, vignette);
                shade.a = 1f;
                pixels[y * size + x] = shade;
            }
            texture.SetPixels32(pixels);
            texture.Apply();
            texture.wrapMode = TextureWrapMode.Repeat;
            return texture;
        }

        public static Texture2D SunFace(int size = 96) => Luminary(size, isSun: true);
        public static Texture2D MoonFace(int size = 96) => Luminary(size, isSun: false);

        private static Texture2D Luminary(int size, bool isSun)
        {
            var canvas = new Canvas(size);
            var c = new Vector2(size * .5f, size * .5f);
            var discRadius = size * (isSun ? .27f : .30f);

            if (isSun)
            {
                for (var i = 0; i < 16; i++)
                {
                    var a = i / 16f * Mathf.PI * 2f;
                    var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                    var tip = c + dir * size * .47f;
                    var baseA = c + new Vector2(-dir.y, dir.x) * size * .05f + dir * discRadius;
                    var baseB = c - new Vector2(-dir.y, dir.x) * size * .05f + dir * discRadius;
                    canvas.FillTriangle(tip, baseA, baseB, Gilt);
                }
            }

            canvas.FillDisc(c, discRadius, isSun ? new Color(.83f, .62f, .22f, 1f) : new Color(.80f, .80f, .74f, 1f));
            canvas.StrokeCircle(c, discRadius, 2f, Ink);

            if (!isSun)
            {
                // Bite a crescent out of the moon.
                canvas.FillDisc(c + new Vector2(discRadius * .55f, discRadius * .18f), discRadius * .92f,
                    new Color(0, 0, 0, 0), erase: true);
                canvas.StrokeCircle(c + new Vector2(discRadius * .55f, discRadius * .18f), discRadius * .92f, 1.5f, Ink);
            }

            var eyeOffset = discRadius * (isSun ? .34f : .18f);
            var eyeY = c.y + discRadius * .12f;
            canvas.FillDisc(new Vector2(c.x - eyeOffset, eyeY), size * .022f, Ink);
            canvas.FillDisc(new Vector2(c.x + eyeOffset, eyeY), size * .022f, Ink);
            canvas.StrokeArc(new Vector2(c.x, c.y - discRadius * .18f), discRadius * .42f,
                isSun ? 200f : 210f, isSun ? 340f : 330f, 2f, Ink);
            canvas.Stroke(new Vector2(c.x - eyeOffset * 1.3f, eyeY + size * .06f),
                new Vector2(c.x - eyeOffset * .5f, eyeY + size * .04f), 1.5f, Ink);
            canvas.Stroke(new Vector2(c.x + eyeOffset * 1.3f, eyeY + size * .06f),
                new Vector2(c.x + eyeOffset * .5f, eyeY + size * .04f), 1.5f, Ink);
            return canvas.ToTexture();
        }

        /// A puffing cherub head. <paramref name="blowAngleDegrees"/> is the direction the wind blows.
        public static Texture2D WindHead(int size = 88, float blowAngleDegrees = 0f)
        {
            var canvas = new Canvas(size);
            var c = new Vector2(size * .42f, size * .5f);
            var head = size * .26f;
            var dir = new Vector2(Mathf.Cos(blowAngleDegrees * Mathf.Deg2Rad), Mathf.Sin(blowAngleDegrees * Mathf.Deg2Rad));

            for (var i = 0; i < 5; i++)
            {
                var spread = (i - 2) * .28f;
                var perp = new Vector2(-dir.y, dir.x) * size * spread * .5f;
                var start = c + dir * head * 1.15f + perp;
                canvas.StrokeArc(start + dir * size * .12f, size * .12f,
                    blowAngleDegrees - 60f + spread * 20f, blowAngleDegrees + 60f + spread * 20f, 1.6f, Gilt);
            }

            // Puffed cheeks + head.
            canvas.FillDisc(c - dir * head * .35f + new Vector2(-dir.y, dir.x) * head * .5f, head * .5f, ParchmentDeep);
            canvas.FillDisc(c - dir * head * .35f - new Vector2(-dir.y, dir.x) * head * .5f, head * .5f, ParchmentDeep);
            canvas.FillDisc(c, head, new Color(.82f, .70f, .48f, 1f));
            canvas.StrokeCircle(c, head, 2f, Ink);
            canvas.StrokeCircle(c - dir * head * .35f + new Vector2(-dir.y, dir.x) * head * .5f, head * .5f, 1.4f, Ink);
            canvas.StrokeCircle(c - dir * head * .35f - new Vector2(-dir.y, dir.x) * head * .5f, head * .5f, 1.4f, Ink);

            // Curls.
            for (var i = 0; i < 7; i++)
            {
                var a = 90f + i * 30f;
                var p = c + new Vector2(Mathf.Cos(a * Mathf.Deg2Rad), Mathf.Sin(a * Mathf.Deg2Rad)) * head;
                canvas.StrokeCircle(p, size * .04f, 1.4f, Ink);
            }

            canvas.FillDisc(c + dir * head * .1f + new Vector2(-dir.y, dir.x) * head * .3f, size * .018f, Ink);
            canvas.FillDisc(c + dir * head * .1f - new Vector2(-dir.y, dir.x) * head * .3f, size * .018f, Ink);
            canvas.FillDisc(c + dir * head * .7f, size * .05f, ParchmentDeep);
            canvas.StrokeCircle(c + dir * head * .7f, size * .05f, 1.4f, Ink);
            return canvas.ToTexture();
        }

        public static Texture2D CompassRose(int size = 128)
        {
            var canvas = new Canvas(size);
            var c = new Vector2(size * .5f, size * .5f);
            canvas.StrokeCircle(c, size * .44f, 2f, Ink);
            canvas.StrokeCircle(c, size * .34f, 1f, Gilt);
            for (var i = 0; i < 8; i++)
            {
                var a = i / 8f * Mathf.PI * 2f;
                var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                var perp = new Vector2(-dir.y, dir.x);
                var length = i % 2 == 0 ? .42f : .30f;
                var tip = c + dir * size * length;
                canvas.FillTriangle(tip, c + perp * size * .05f, c - perp * size * .05f,
                    i % 2 == 0 ? Ink : Gilt);
            }
            canvas.FillDisc(c, size * .05f, Parchment);
            canvas.StrokeCircle(c, size * .05f, 1.5f, Ink);
            return canvas.ToTexture();
        }

        /// The entry button: a rear end sounding a horn, in the spirit of a marginal drollery.
        public static Texture2D ArseHorn(int size = 96)
        {
            var canvas = new Canvas(size);
            var c = new Vector2(size * .40f, size * .46f);
            var cheek = size * .17f;

            // Two cheeks with a cleft.
            canvas.FillDisc(new Vector2(c.x - cheek * .7f, c.y), cheek, new Color(.82f, .68f, .47f, 1f));
            canvas.FillDisc(new Vector2(c.x + cheek * .7f, c.y), cheek, new Color(.82f, .68f, .47f, 1f));
            canvas.StrokeArc(new Vector2(c.x - cheek * .7f, c.y), cheek, 60f, 330f, 2f, Ink);
            canvas.StrokeArc(new Vector2(c.x + cheek * .7f, c.y), cheek, 210f, 480f, 2f, Ink);
            canvas.Stroke(new Vector2(c.x, c.y + cheek * .7f), new Vector2(c.x, c.y - cheek * .7f), 1.6f, Ink);
            // Legs.
            canvas.Stroke(new Vector2(c.x - cheek * .8f, c.y - cheek * .8f), new Vector2(c.x - cheek * 1.1f, c.y - cheek * 1.7f), 3f, Ink);
            canvas.Stroke(new Vector2(c.x + cheek * .8f, c.y - cheek * .8f), new Vector2(c.x + cheek * 1.1f, c.y - cheek * 1.7f), 3f, Ink);

            // Horn wedged in the cleft, bell pointing up and away.
            var mouth = new Vector2(c.x + cheek * .1f, c.y + cheek * .2f);
            var bell = new Vector2(size * .80f, size * .80f);
            var axis = (bell - mouth).normalized;
            var perp = new Vector2(-axis.y, axis.x);
            canvas.FillTriangle(mouth + perp * size * .02f, mouth - perp * size * .02f, bell + perp * size * .11f, Gilt);
            canvas.FillTriangle(mouth - perp * size * .02f, bell - perp * size * .11f, bell + perp * size * .11f, Gilt);
            canvas.Stroke(mouth + perp * size * .02f, bell + perp * size * .11f, 1.6f, Ink);
            canvas.Stroke(mouth - perp * size * .02f, bell - perp * size * .11f, 1.6f, Ink);
            canvas.Stroke(bell + perp * size * .11f, bell - perp * size * .11f, 1.6f, Ink);

            // Three toots.
            for (var i = 0; i < 3; i++)
            {
                var p = bell + axis * size * (.08f + i * .05f);
                canvas.StrokeArc(p, size * (.05f + i * .03f), 200f, 340f, 1.4f, Blood);
            }
            return canvas.ToTexture();
        }

        /// The flat-earth map plate: ocean field, land masses and coastlines under the flat-earth
        /// distortion, concentric parallels, radial meridians, a marked North Pole hub and the
        /// thick "ice wall" rim standing in for the South Pole. Pass the projected Natural Earth
        /// paths to draw real continents and borders; omit them for a blank graticule.
        public static Texture2D FlatEarthPlate(int size = 512,
            IReadOnlyList<Vector2[]> coastlines = null, IReadOnlyList<Vector2[]> borders = null)
        {
            var canvas = new Canvas(size);
            var c = new Vector2(size * .5f, size * .5f);
            var r = size * .46f;
            var hasMap = coastlines != null && coastlines.Count > 0;

            canvas.FillDisc(c, r, hasMap ? Ocean : new Color(.80f, .69f, .47f, 1f));

            if (hasMap)
            {
                var coastPixels = ToPixels(coastlines, c, r);
                canvas.FillEvenOdd(coastPixels, Land, c, r - 1.5f);
                foreach (var path in coastPixels) canvas.StrokePolyline(path, 1.3f, Ink, r * 1.4f);
                if (borders != null)
                    foreach (var path in ToPixels(borders, c, r)) canvas.StrokePolyline(path, 1f, BorderInk, r * 0.9f);
            }

            for (var lat = 60; lat >= -60; lat -= 30)
            {
                var ring = r * (float)FlatEarthProjectionRadius(lat);
                canvas.StrokeCircle(c, ring, lat == 0 ? 2f : 1f, lat == 0 ? Gilt : new Color(Ink.r, Ink.g, Ink.b, .5f));
            }
            for (var meridian = 0; meridian < 12; meridian++)
            {
                var a = meridian / 12f * Mathf.PI * 2f;
                var dir = new Vector2(Mathf.Sin(a), Mathf.Cos(a));
                canvas.Stroke(c + dir * (r * .02f), c + dir * r, meridian % 3 == 0 ? 1.1f : .7f,
                    new Color(Ink.r, Ink.g, Ink.b, .45f));
            }

            // Ice wall.
            canvas.StrokeCircle(c, r, 6f, new Color(.90f, .92f, .95f, .9f));
            canvas.StrokeCircle(c, r, 2f, Ink);
            canvas.StrokeCircle(c, r * .995f, 1.4f, Ink);
            for (var i = 0; i < 96; i++)
            {
                var a = i / 96f * Mathf.PI * 2f;
                var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                canvas.Stroke(c + dir * (r - size * .02f), c + dir * (r + size * .006f), 1f,
                    new Color(.86f, .88f, .92f, .8f));
            }

            // North Pole hub.
            canvas.FillDisc(c, size * .012f, Blood);
            canvas.StrokeCircle(c, size * .03f, 1.6f, Ink);
            return canvas.ToTexture();
        }

        private static double FlatEarthProjectionRadius(double latitude) => (90.0 - latitude) / 180.0;

        // Unit-disc paths (y up) -> texture pixels, matching the meridian convention (0° up, 90°E right).
        private static List<Vector2[]> ToPixels(IReadOnlyList<Vector2[]> discPaths, Vector2 center, float radius)
        {
            var result = new List<Vector2[]>(discPaths.Count);
            foreach (var path in discPaths)
            {
                var pixels = new Vector2[path.Length];
                for (var i = 0; i < path.Length; i++)
                    pixels[i] = new Vector2(center.x + path[i].x * radius, center.y + path[i].y * radius);
                result.Add(pixels);
            }
            return result;
        }

        private static Texture2D NewTexture(int size)
        {
            return new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
        }

        /// Tiny software rasteriser working on a transparent RGBA buffer.
        private sealed class Canvas
        {
            private readonly int _size;
            private readonly Color[] _pixels;

            public Canvas(int size)
            {
                _size = size;
                _pixels = new Color[size * size];
            }

            public Texture2D ToTexture()
            {
                var texture = NewTexture(_size);
                texture.SetPixels(_pixels);
                texture.Apply(false, true);
                return texture;
            }

            public void FillDisc(Vector2 center, float radius, Color color, bool erase = false)
            {
                var min = center - new Vector2(radius + 1f, radius + 1f);
                var max = center + new Vector2(radius + 1f, radius + 1f);
                for (var y = Mathf.Max(0, (int)min.y); y <= Mathf.Min(_size - 1, (int)max.y); y++)
                for (var x = Mathf.Max(0, (int)min.x); x <= Mathf.Min(_size - 1, (int)max.x); x++)
                {
                    var d = Vector2.Distance(new Vector2(x + .5f, y + .5f), center);
                    var cover = Mathf.Clamp01(radius - d + .5f);
                    if (erase) Erase(x, y, cover);
                    else Blend(x, y, color, cover);
                }
            }

            public void StrokeCircle(Vector2 center, float radius, float width, Color color) =>
                StrokeArc(center, radius, 0f, 360f, width, color);

            public void StrokeArc(Vector2 center, float radius, float fromDeg, float toDeg, float width, Color color)
            {
                var steps = Mathf.Max(12, Mathf.CeilToInt(Mathf.Abs(toDeg - fromDeg) / 4f));
                Vector2? prev = null;
                for (var i = 0; i <= steps; i++)
                {
                    var a = Mathf.Lerp(fromDeg, toDeg, i / (float)steps) * Mathf.Deg2Rad;
                    var p = center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius;
                    if (prev.HasValue) Stroke(prev.Value, p, width, color);
                    prev = p;
                }
            }

            public void Stroke(Vector2 a, Vector2 b, float width, Color color)
            {
                var length = Vector2.Distance(a, b);
                var steps = Mathf.Max(1, Mathf.CeilToInt(length));
                var half = width * .5f;
                for (var i = 0; i <= steps; i++)
                {
                    var p = Vector2.Lerp(a, b, i / (float)steps);
                    for (var y = Mathf.Max(0, (int)(p.y - half - 1)); y <= Mathf.Min(_size - 1, (int)(p.y + half + 1)); y++)
                    for (var x = Mathf.Max(0, (int)(p.x - half - 1)); x <= Mathf.Min(_size - 1, (int)(p.x + half + 1)); x++)
                    {
                        var cover = Mathf.Clamp01(half - Vector2.Distance(new Vector2(x + .5f, y + .5f), p) + .5f);
                        Blend(x, y, color, cover);
                    }
                }
            }

            public void StrokePolyline(IReadOnlyList<Vector2> points, float width, Color color, float maxSegment)
            {
                for (var i = 0; i + 1 < points.Count; i++)
                {
                    if (Vector2.Distance(points[i], points[i + 1]) > maxSegment) continue;
                    Stroke(points[i], points[i + 1], width, color);
                }
            }

            /// Even-odd fill of every path treated as one edge set. In flat-earth disc space the
            /// antimeridian is not special, so continent arcs split at ±180° still bound land.
            public void FillEvenOdd(IReadOnlyList<Vector2[]> paths, Color color, Vector2 clipCenter, float clipRadius)
            {
                var minY = float.MaxValue;
                var maxY = float.MinValue;
                foreach (var path in paths)
                foreach (var point in path)
                {
                    minY = Mathf.Min(minY, point.y);
                    maxY = Mathf.Max(maxY, point.y);
                }
                var y0 = Mathf.Max(0, Mathf.FloorToInt(minY));
                var y1 = Mathf.Min(_size - 1, Mathf.CeilToInt(maxY));
                var crossings = new List<float>(64);
                for (var y = y0; y <= y1; y++)
                {
                    var scanY = y + .5f;
                    crossings.Clear();
                    foreach (var path in paths)
                        for (var e = 0; e + 1 < path.Length; e++)
                        {
                            var a = path[e];
                            var b = path[e + 1];
                            if (a.y > scanY == b.y > scanY) continue;
                            crossings.Add(a.x + (scanY - a.y) / (b.y - a.y) * (b.x - a.x));
                        }
                    if (crossings.Count < 2) continue;
                    crossings.Sort();
                    for (var pair = 0; pair + 1 < crossings.Count; pair += 2)
                    {
                        var xa = Mathf.Max(0, Mathf.CeilToInt(crossings[pair]));
                        var xb = Mathf.Min(_size - 1, Mathf.FloorToInt(crossings[pair + 1]));
                        for (var x = xa; x <= xb; x++)
                        {
                            if (clipRadius > 0f &&
                                Vector2.Distance(new Vector2(x + .5f, y + .5f), clipCenter) > clipRadius) continue;
                            Blend(x, y, color, 1f);
                        }
                    }
                }
            }

            public void FillTriangle(Vector2 a, Vector2 b, Vector2 c, Color color)
            {
                var min = Vector2.Min(a, Vector2.Min(b, c));
                var max = Vector2.Max(a, Vector2.Max(b, c));
                for (var y = Mathf.Max(0, (int)min.y); y <= Mathf.Min(_size - 1, (int)max.y); y++)
                for (var x = Mathf.Max(0, (int)min.x); x <= Mathf.Min(_size - 1, (int)max.x); x++)
                {
                    var p = new Vector2(x + .5f, y + .5f);
                    var d1 = Sign(p, a, b);
                    var d2 = Sign(p, b, c);
                    var d3 = Sign(p, c, a);
                    var hasNeg = d1 < 0 || d2 < 0 || d3 < 0;
                    var hasPos = d1 > 0 || d2 > 0 || d3 > 0;
                    if (!(hasNeg && hasPos)) Blend(x, y, color, 1f);
                }
            }

            private static float Sign(Vector2 p, Vector2 a, Vector2 b) =>
                (p.x - b.x) * (a.y - b.y) - (a.x - b.x) * (p.y - b.y);

            private void Blend(int x, int y, Color color, float coverage)
            {
                if (coverage <= 0f) return;
                var index = y * _size + x;
                var src = color;
                src.a *= Mathf.Clamp01(coverage);
                var dst = _pixels[index];
                var outA = src.a + dst.a * (1f - src.a);
                if (outA <= 0f) { _pixels[index] = default; return; }
                var rgb = (new Vector3(src.r, src.g, src.b) * src.a +
                           new Vector3(dst.r, dst.g, dst.b) * dst.a * (1f - src.a)) / outA;
                _pixels[index] = new Color(rgb.x, rgb.y, rgb.z, outA);
            }

            private void Erase(int x, int y, float coverage)
            {
                var index = y * _size + x;
                var dst = _pixels[index];
                dst.a *= Mathf.Clamp01(1f - coverage);
                _pixels[index] = dst;
            }
        }
    }
}
