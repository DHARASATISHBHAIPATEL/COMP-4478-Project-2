using System.Collections.Generic;
using UnityEngine;

namespace Tether
{
    public enum Era { Future, Modern, Past, Saved }

    /// <summary>
    /// Character sprites come from the Kenney Platformer Characters pack (CC0) in
    /// Resources/Characters. Everything else (tiles, skylines, props) is drawn
    /// procedurally here, so the project needs no other art files.
    /// </summary>
    public static class Art
    {
        public const float CharPPU = 80f;
        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();
        static Material spriteMat;

        public static Material SpriteMat
        {
            get
            {
                if (spriteMat == null)
                {
                    // TetherSprite.mat is created by the editor setup so the shader is always included in builds
                    var asset = Resources.Load<Material>("TetherSprite");
                    if (asset != null) { spriteMat = asset; return spriteMat; }
                    var sh = Shader.Find("Sprites/Default");
                    if (sh != null) spriteMat = new Material(sh);
                }
                return spriteMat;
            }
        }

        public static SpriteRenderer MakeRenderer(GameObject go, Sprite s, int order, Color? c = null)
        {
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = s;
            sr.sortingOrder = order;
            if (SpriteMat != null) sr.sharedMaterial = SpriteMat;
            if (c.HasValue) sr.color = c.Value;
            return sr;
        }

        // ---------------- palettes ----------------

        public struct Palette
        {
            public Color SkyTop, SkyBottom, Far, Near, Tile, TileDark, TileTop, Accent, Light, Particle;
        }

        public static Palette Pal(Era e, bool underground = false)
        {
            Palette p = new Palette();
            switch (e)
            {
                case Era.Future:
                    p.SkyTop = Hex("1c1216"); p.SkyBottom = Hex("7a3b2c"); p.Far = Hex("3a2224"); p.Near = Hex("231719");
                    p.Tile = Hex("4d5b5c"); p.TileDark = Hex("343f40"); p.TileTop = Hex("9a9a90"); p.Accent = Hex("c0503a");
                    p.Light = Hex("ff8a5c"); p.Particle = new Color(0.75f, 0.72f, 0.7f, 0.7f);
                    break;
                case Era.Modern:
                    p.SkyTop = Hex("3d8fd1"); p.SkyBottom = Hex("cdeefe"); p.Far = Hex("8db4d8"); p.Near = Hex("5579a3");
                    p.Tile = Hex("d7dde3"); p.TileDark = Hex("aab4bf"); p.TileTop = Hex("5fd6ff"); p.Accent = Hex("2fb7ff");
                    p.Light = Hex("fff2a8"); p.Particle = new Color(1f, 1f, 1f, 0.35f);
                    break;
                case Era.Past:
                    p.SkyTop = Hex("e7a96a"); p.SkyBottom = Hex("f9e3b8"); p.Far = Hex("c98f5c"); p.Near = Hex("8f5d3a");
                    p.Tile = Hex("7a5436"); p.TileDark = Hex("5a3c25"); p.TileTop = Hex("b18a55"); p.Accent = Hex("e39a3b");
                    p.Light = Hex("ffc76b"); p.Particle = new Color(1f, 0.9f, 0.6f, 0.45f);
                    break;
                default: // Saved 2190
                    p.SkyTop = Hex("6cc7d8"); p.SkyBottom = Hex("fff0c8"); p.Far = Hex("9fcfb6"); p.Near = Hex("5f9f86");
                    p.Tile = Hex("8a7a64"); p.TileDark = Hex("6c5e4c"); p.TileTop = Hex("7ccc5a"); p.Accent = Hex("ff9fb8");
                    p.Light = Hex("fff6d0"); p.Particle = new Color(1f, 0.75f, 0.85f, 0.8f);
                    break;
            }
            if (underground)
            {
                p.SkyTop = Color.Lerp(p.SkyTop, Hex("120c0a"), 0.85f);
                p.SkyBottom = Color.Lerp(p.SkyBottom, Hex("2a1c14"), 0.8f);
                p.Far = Color.Lerp(p.Far, Hex("2e2018"), 0.75f);
                p.Near = Color.Lerp(p.Near, Hex("1b120d"), 0.75f);
            }
            return p;
        }

        public static Color Hex(string h)
        {
            ColorUtility.TryParseHtmlString("#" + h, out var c);
            return c;
        }

        public static string EraLabel(Era e)
        {
            switch (e)
            {
                case Era.Future: return "HALDEN  2190";
                case Era.Modern: return "HALDEN  2041";
                case Era.Past: return "BRASK HOLLOW  1889";
                default: return "HALDEN  2190";
            }
        }

        // ---------------- characters ----------------

        public static Sprite Char(string set, string pose)
        {
            string key = "c_" + set + "_" + pose;
            if (cache.TryGetValue(key, out var s)) return s;
            var tex = Resources.Load<Texture2D>("Characters/" + set + "/" + set.ToLower() + "_" + pose);
            if (tex == null)
            {
                Debug.LogWarning("Tether: missing character sprite " + set + "/" + pose);
                s = Pixel;
            }
            else
            {
                s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0f), CharPPU);
            }
            cache[key] = s;
            return s;
        }

        // ---------------- basic shapes ----------------

        static Sprite pixel;
        public static Sprite Pixel
        {
            get
            {
                if (pixel == null)
                {
                    var t = new Texture2D(4, 4) { filterMode = FilterMode.Point };
                    var px = new Color[16];
                    for (int i = 0; i < 16; i++) px[i] = Color.white;
                    t.SetPixels(px); t.Apply();
                    pixel = Sprite.Create(t, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f, 0, SpriteMeshType.FullRect);
                }
                return pixel;
            }
        }

        static Texture2D NewTex(int w, int h, bool repeat = false, bool smooth = false)
        {
            var t = new Texture2D(w, h, TextureFormat.RGBA32, false);
            t.filterMode = smooth ? FilterMode.Bilinear : FilterMode.Point;
            t.wrapMode = repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            return t;
        }

        static Sprite Finish(string key, Texture2D t, Color[] px, Vector2 pivot, float ppu, bool fullRect = false)
        {
            t.SetPixels(px);
            t.Apply();
            var s = Sprite.Create(t, new Rect(0, 0, t.width, t.height), pivot, ppu, 0, fullRect ? SpriteMeshType.FullRect : SpriteMeshType.Tight);
            cache[key] = s;
            return s;
        }

        static float Hash(int x, int y, int seed)
        {
            unchecked
            {
                int h = x * 374761393 + y * 668265263 + seed * 1442695041;
                h = (h ^ (h >> 13)) * 1274126177;
                return ((h ^ (h >> 16)) & 0xffff) / 65535f;
            }
        }

        // ---------------- tiles (16 px = 1 unit) ----------------

        /// <param name="top">true when the cell above is empty, so the tile shows a surface edge</param>
        public static Sprite Tile(Era e, bool top, int variant, bool underground)
        {
            string key = "t_" + e + top + variant + underground;
            if (cache.TryGetValue(key, out var s)) return s;
            var p = Pal(e);
            Color baseC = p.Tile, dark = p.TileDark, edge = p.TileTop;
            if (underground && (e == Era.Past))
            {
                baseC = Hex("5c4030"); dark = Hex("3e2b20"); edge = Hex("8a6a48");
            }
            var t = NewTex(16, 16);
            var px = new Color[256];
            for (int y = 0; y < 16; y++)
                for (int x = 0; x < 16; x++)
                {
                    float n = Hash(x, y, variant + (int)e * 7);
                    Color c = Color.Lerp(baseC, dark, n * 0.35f);
                    switch (e)
                    {
                        case Era.Future: // cracked concrete
                            if (Hash(x / 3, y / 3, variant) > 0.86f) c = Color.Lerp(c, dark, 0.7f);
                            if ((x + y * 3 + variant) % 23 == 0) c = dark * 0.8f;
                            break;
                        case Era.Modern: // clean panels
                            if (x == 0 || y == 0) c = dark;
                            if (x == 8 && y > 4 && y < 12) c = Color.Lerp(c, p.Accent, 0.25f);
                            break;
                        case Era.Past: // planks or rock
                            if (underground) { if (Hash(x / 4, y / 4, variant) > 0.7f) c = Color.Lerp(c, dark, 0.5f); }
                            else if (y % 5 == 0) c = dark;
                            break;
                        case Era.Saved:
                            if (Hash(x / 2, y / 2, variant) > 0.8f) c = Color.Lerp(c, dark, 0.4f);
                            break;
                    }
                    if (top && y >= 13)
                    {
                        c = edge;
                        if (e == Era.Saved && y == 13 && Hash(x, 0, variant) > 0.5f) c = Hex("5aa83c");
                        if (e == Era.Future && Hash(x, y, 99 + variant) > 0.7f) c = Color.Lerp(edge, baseC, 0.5f);
                    }
                    px[y * 16 + x] = c;
                }
            return Finish(key, t, px, new Vector2(0.5f, 0.5f), 16f, true);
        }

        public static Sprite Ladder(Era e)
        {
            string key = "lad_" + e;
            if (cache.TryGetValue(key, out var s)) return s;
            var p = Pal(e);
            Color rail = e == Era.Modern ? Hex("8796a6") : e == Era.Past ? Hex("8a5a2e") : Hex("6a6a64");
            var t = NewTex(16, 16);
            var px = new Color[256];
            for (int y = 0; y < 16; y++)
                for (int x = 0; x < 16; x++)
                {
                    Color c = Color.clear;
                    if (x == 2 || x == 3 || x == 12 || x == 13) c = rail;
                    if ((y == 3 || y == 4 || y == 11 || y == 12) && x > 1 && x < 14) c = rail * 1.1f;
                    px[y * 16 + x] = c;
                }
            return Finish(key, t, px, new Vector2(0.5f, 0.5f), 16f, true);
        }

        public static Sprite Spikes(Era e)
        {
            string key = "spk_" + e;
            if (cache.TryGetValue(key, out var s)) return s;
            Color c = e == Era.Future ? Hex("a8b8b4") : e == Era.Modern ? Hex("ff5a5a") : Hex("6f5a4a");
            var t = NewTex(16, 8);
            var px = new Color[128];
            for (int y = 0; y < 8; y++)
                for (int x = 0; x < 16; x++)
                {
                    int lx = x % 4;
                    bool on = Mathf.Abs(lx - 1.5f) <= (7 - y) * 0.25f;
                    px[y * 16 + x] = on ? Color.Lerp(c, Color.black, y * 0.03f) : Color.clear;
                }
            return Finish(key, t, px, new Vector2(0.5f, 0f), 16f, true);
        }

        // ---------------- props ----------------

        public static Sprite Prop(string name)
        {
            string key = "p_" + name;
            if (cache.TryGetValue(key, out var s)) return s;
            switch (name)
            {
                case "lantern": return Shape(key, 16, 32, (x, y) =>
                    (x >= 7 && x <= 8 && y < 22) ? Hex("3b3b3b") :
                    (y >= 22 && y <= 29 && x >= 4 && x <= 11) ? ((x == 4 || x == 11 || y == 22 || y == 29) ? Hex("3b3b3b") : Hex("ffcf6b")) :
                    Color.clear, new Vector2(0.5f, 0f));
                case "lanternOff": return Shape(key, 16, 32, (x, y) =>
                    (x >= 7 && x <= 8 && y < 22) ? Hex("3b3b3b") :
                    (y >= 22 && y <= 29 && x >= 4 && x <= 11) ? ((x == 4 || x == 11 || y == 22 || y == 29) ? Hex("3b3b3b") : Hex("5c6670")) :
                    Color.clear, new Vector2(0.5f, 0f));
                case "poster": return Shape(key, 24, 28, (x, y) =>
                    (x == 0 || y == 0 || x == 23 || y == 27) ? new Color(0, 0, 0, 0.6f) :
                    (y > 18 ? new Color(1, 1, 1, 0.85f) : (y > 6 && x > 4 && x < 19 && (y % 3 != 0)) ? new Color(1, 1, 1, 0.4f) : new Color(1, 1, 1, 0.65f)),
                    new Vector2(0.5f, 0f));
                case "beam": return Shape(key, 48, 8, (x, y) =>
                    (y == 0 || y == 7) ? Hex("4a2e19") : (x % 12 == 0 ? Hex("5a3a20") : Hex("8a5a30")), new Vector2(0.5f, 0.5f));
                case "tether": return Shape(key, 16, 16, (x, y) =>
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(7.5f, 7.5f));
                    if (d < 5.2f) return (Mathf.Abs(x - 7.5f) < 0.6f && y > 7) || (Mathf.Abs(y - 7.5f) < 0.6f && x > 7) ? Hex("3a2a10") : Hex("ffe08a");
                    if (d < 6.5f) return Hex("b8862e");
                    if (y > 13 && x >= 6 && x <= 9) return Hex("b8862e");
                    return Color.clear;
                }, new Vector2(0.5f, 0.5f));
                case "glow": return Shape(key, 64, 64, (x, y) =>
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(31.5f, 31.5f)) / 32f;
                    float a = Mathf.Clamp01(1 - d);
                    return new Color(1, 1, 1, a * a);
                }, new Vector2(0.5f, 0.5f), true);
                case "crystal": return Shape(key, 32, 48, (x, y) =>
                {
                    float cx = 16 + Mathf.Sin(y * 0.4f) * 3;
                    float half = (48 - y) * 0.33f;
                    if (Mathf.Abs(x - cx) < half)
                        return Color.Lerp(Hex("7ef0ff"), Hex("e6ffff"), Hash(x / 3, y / 3, 5) * 0.6f + (Mathf.Abs(x - cx) < 2 ? 0.4f : 0));
                    return Color.clear;
                }, new Vector2(0.5f, 0f), true);
                case "support": return Shape(key, 16, 64, (x, y) =>
                    (x >= 4 && x <= 11) ? (x == 4 || x == 11 ? Hex("4a2e19") : Hex("8a5a30")) : (y > 58 ? Hex("6a4224") : Color.clear),
                    new Vector2(0.5f, 0f));
                case "desk": return Shape(key, 32, 16, (x, y) =>
                    (y >= 11) ? Hex("4b5563") : ((x < 3 || x > 28) ? Hex("374151") : Color.clear), new Vector2(0.5f, 0f));
                case "fire": return Shape(key, 16, 16, (x, y) =>
                {
                    float w = (16 - y) * 0.35f;
                    if (y < 3 && x > 2 && x < 13) return Hex("4a2e19");
                    if (Mathf.Abs(x - 7.5f) < w && y >= 3) return Color.Lerp(Hex("ffd36b"), Hex("ff5a2a"), y / 16f);
                    return Color.clear;
                }, new Vector2(0.5f, 0f));
                case "easel": return Shape(key, 32, 40, (x, y) =>
                {
                    if (y > 14 && y < 38 && x > 3 && x < 28)
                        return (x == 4 || x == 27 || y == 15 || y == 37) ? Hex("6a4224") :
                            Color.Lerp(Hex("6cc7d8"), Hex("fff0c8"), (y - 15) / 22f) * (y < 22 ? 0.0f : 1f) + (y < 22 ? Hex("7ccc5a") : Color.clear);
                    if ((x == 8 || x == 23) && y <= 15) return Hex("6a4224");
                    return Color.clear;
                }, new Vector2(0.5f, 0f));
                case "rock": return Shape(key, 32, 32, (x, y) =>
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(15.5f, 13f));
                    return d < 13 + Hash(x / 4, y / 4, 3) * 3 ? Color.Lerp(Hex("3a2a20"), Hex("5c4535"), Hash(x / 3, y / 3, 1)) : Color.clear;
                }, new Vector2(0.5f, 0.5f));
                case "window": return Shape(key, 32, 24, (x, y) =>
                    (x < 2 || x > 29 || y < 2 || y > 21 || x == 15 || x == 16) ? Hex("4b5563") : new Color(1f, 0.95f, 0.7f, 0.9f),
                    new Vector2(0.5f, 0f));
                case "lever": return Shape(key, 16, 24, (x, y) =>
                    (y < 6 && x > 1 && x < 14) ? Hex("4b5563") : (Mathf.Abs(x - (8 + (y - 6) * 0.3f)) < 1.2f && y < 20) ? Hex("9aa4b1") :
                    (y >= 19 && y <= 22 && x >= 11 && x <= 15) ? Hex("ff4a4a") : Color.clear, new Vector2(0.5f, 0f));
                case "shopsign": return Shape(key, 48, 16, (x, y) =>
                    (x == 0 || x == 47 || y == 0 || y == 15) ? Hex("6a4224") : Hex("f4d58d"), new Vector2(0.5f, 0f));
                case "tree": return Shape(key, 32, 48, (x, y) =>
                {
                    if (y < 18 && x >= 14 && x <= 17) return Hex("6a4a2a");
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(15.5f, 30));
                    if (d < 14 - Hash(x / 3, y / 3, 2) * 3) return Color.Lerp(Hex("4f9a45"), Hex("7ccc5a"), Hash(x / 2, y / 2, 7));
                    return Color.clear;
                }, new Vector2(0.5f, 0f));
            }
            return Pixel;
        }

        delegate Color PixelFn(int x, int y);

        static Sprite Shape(string key, int w, int h, PixelFn fn, Vector2 pivot, bool smooth = false)
        {
            var t = NewTex(w, h, false, smooth);
            var px = new Color[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++) px[y * w + x] = fn(x, y);
            return Finish(key, t, px, pivot, 16f);
        }

        // ---------------- backdrop ----------------

        public static Sprite SkyGradient(Era e, bool underground)
        {
            string key = "sky_" + e + underground;
            if (cache.TryGetValue(key, out var s)) return s;
            var p = Pal(e, underground);
            var t = NewTex(1, 64, false, true);
            var px = new Color[64];
            for (int y = 0; y < 64; y++) px[y] = Color.Lerp(p.SkyBottom, p.SkyTop, y / 63f);
            return Finish(key, t, px, new Vector2(0.5f, 0.5f), 1f, true);
        }

        /// <summary>A horizontally tiling skyline strip, 256x96 px (16 x 6 units).</summary>
        public static Sprite Skyline(Era e, int layer, bool underground)
        {
            string key = "sl_" + e + layer + underground;
            if (cache.TryGetValue(key, out var s)) return s;
            var p = Pal(e, underground);
            Color col = layer == 0 ? p.Far : p.Near;
            int W = 256, H = 96;
            var t = NewTex(W, H, true);
            var px = new Color[W * H];
            var heights = new int[W];
            var window = new bool[W * H];
            int seed = (int)e * 31 + layer * 7;

            if (underground)
            {
                // stalactites / cave wall
                for (int x = 0; x < W; x++)
                {
                    float n = Mathf.PerlinNoise(x * 0.04f + seed, layer * 3.1f);
                    heights[x] = (int)(18 + n * 30);
                }
                for (int y = 0; y < H; y++)
                    for (int x = 0; x < W; x++)
                    {
                        bool bottom = y < heights[x] * 0.6f;
                        bool top = y > H - heights[(x + 37) % W] * 0.7f;
                        px[y * W + x] = bottom || top ? col : Color.clear;
                    }
                return Finish(key, t, px, new Vector2(0.5f, 0f), 16f, true);
            }

            int bx = 0;
            var rng = new System.Random(seed);
            while (bx < W)
            {
                int bw, bh;
                switch (e)
                {
                    case Era.Past: bw = rng.Next(14, 30); bh = layer == 0 ? 0 : rng.Next(10, 22); break;
                    case Era.Modern: bw = rng.Next(14, 26); bh = rng.Next(layer == 0 ? 40 : 24, layer == 0 ? 90 : 60); break;
                    case Era.Saved: bw = rng.Next(14, 26); bh = rng.Next(layer == 0 ? 36 : 20, layer == 0 ? 80 : 52); break;
                    default: bw = rng.Next(10, 24); bh = rng.Next(layer == 0 ? 20 : 14, layer == 0 ? 70 : 48); break;
                }
                for (int x = bx; x < Mathf.Min(W, bx + bw); x++)
                {
                    int h = bh;
                    if (e == Era.Future) h -= (int)(Mathf.Abs(Mathf.Sin((x - bx) * 0.9f + bx)) * 10 * Hash(x, 0, seed)); // broken tops
                    if (e == Era.Past && layer == 1) h = (x - bx) < bw / 2 ? bh - (bw / 2 - (x - bx)) / 2 : bh - ((x - bx) - bw / 2) / 2; // roofs
                    heights[x] = Mathf.Max(heights[x], h);
                }
                if (e == Era.Modern || e == Era.Saved)
                    for (int x = bx + 2; x < Mathf.Min(W, bx + bw - 2); x += 3)
                        for (int y = 4; y < bh - 3; y += 4)
                            if (rng.NextDouble() < 0.55) window[y * W + x] = true;
                bx += bw + (e == Era.Past ? rng.Next(4, 20) : rng.Next(0, 4));
            }
            // hills for the past (far) and parks for the saved city
            for (int x = 0; x < W; x++)
            {
                if (e == Era.Past && layer == 0)
                    heights[x] = (int)(20 + 18 * Mathf.Sin(x / (float)W * Mathf.PI * 4) + 8 * Mathf.Sin(x / (float)W * Mathf.PI * 10));
                if (e == Era.Saved && layer == 1)
                {
                    float tr = Mathf.PerlinNoise(x * 0.12f, 3);
                    heights[x] = Mathf.Max(heights[x], (int)(6 + tr * 10));
                }
            }
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    Color c = y < heights[x] ? col : Color.clear;
                    if (c.a > 0 && window[y * W + x]) c = Color.Lerp(col, p.Light, layer == 0 ? 0.35f : 0.7f);
                    if (c.a > 0 && e == Era.Future && Hash(x, y, 4) > 0.985f) c = Color.Lerp(col, p.Light, 0.4f);
                    px[y * W + x] = c;
                }
            return Finish(key, t, px, new Vector2(0.5f, 0f), 16f, true);
        }

        public static Texture2D Vignette()
        {
            string key = "vig";
            if (cache.TryGetValue(key, out var s)) return s.texture;
            int n = 128;
            var t = NewTex(n, n, false, true);
            var px = new Color[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float dx = (x - n / 2f) / (n / 2f), dy = (y - n / 2f) / (n / 2f);
                    float d = Mathf.Sqrt(dx * dx * 0.8f + dy * dy);
                    px[y * n + x] = new Color(0, 0, 0, Mathf.Clamp01((d - 0.55f) * 1.4f));
                }
            return Finish(key, t, px, Vector2.one * 0.5f, 1f).texture;
        }

        static Texture2D white;
        public static Texture2D White
        {
            get
            {
                if (white == null)
                {
                    white = new Texture2D(2, 2);
                    white.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
                    white.Apply();
                }
                return white;
            }
        }
    }
}
