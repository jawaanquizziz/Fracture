using System;
using System.Collections.Generic;
using UnityEngine;

namespace Valmon.Campaign
{
    public partial class CampaignController
    {
        // -------------------------------------------------------------
        // VALMON DESIGN SYSTEM: THEME PALETTE & TOKENS
        // -------------------------------------------------------------
        private static class UITheme
        {
            // Backgrounds (Glassmorphic dark navy & charcoal)
            public static readonly Color GlassDark = new Color(0.024f, 0.043f, 0.082f, 0.88f);
            public static readonly Color GlassMid = new Color(0.035f, 0.063f, 0.118f, 0.82f);
            public static readonly Color GlassCard = new Color(0.047f, 0.082f, 0.149f, 0.76f);
            public static readonly Color GlassInset = new Color(0.016f, 0.027f, 0.055f, 0.75f);
            public static readonly Color GlassHeader = new Color(0.020f, 0.035f, 0.071f, 0.94f);

            // Borders
            public static readonly Color BorderMuted = new Color(0.18f, 0.28f, 0.44f, 0.45f);
            public static readonly Color BorderLight = new Color(0.28f, 0.46f, 0.72f, 0.75f);
            public static readonly Color BorderCyan = new Color(0.10f, 0.75f, 1.00f, 0.85f);
            public static readonly Color BorderCrimson = new Color(1.00f, 0.22f, 0.35f, 0.85f);

            // Accents
            public static readonly Color CyanPrimary = new Color(0.08f, 0.78f, 1.00f);
            public static readonly Color CyanGlow = new Color(0.15f, 0.88f, 1.00f);
            public static readonly Color BluePrimary = new Color(0.12f, 0.48f, 0.95f);
            public static readonly Color CrimsonWarden = new Color(1.00f, 0.24f, 0.33f);
            public static readonly Color CoralWarden = new Color(1.00f, 0.42f, 0.46f);
            public static readonly Color VioletCorruption = new Color(0.72f, 0.36f, 1.00f);
            public static readonly Color AmberWarning = new Color(1.00f, 0.74f, 0.18f);
            public static readonly Color GoldChampion = new Color(1.00f, 0.86f, 0.24f);

            // Arenas
            public static readonly Color ArenaGrass = new Color(0.14f, 0.82f, 0.48f);
            public static readonly Color ArenaFire = new Color(1.00f, 0.46f, 0.18f);
            public static readonly Color ArenaWater = new Color(0.08f, 0.72f, 1.00f);

            // Typography
            public static readonly Color TextWhite = new Color(0.96f, 0.98f, 1.00f);
            public static readonly Color TextMuted = new Color(0.60f, 0.72f, 0.88f);
            public static readonly Color TextDim = new Color(0.38f, 0.48f, 0.64f);

            public static Color GetArenaColor(int index)
            {
                return index == 0 ? ArenaGrass : (index == 1 ? ArenaFire : ArenaWater);
            }

            public static Color GetTypeColor(string type)
            {
                switch (type?.ToLowerInvariant())
                {
                    case "grass": return ArenaGrass;
                    case "fire": return ArenaFire;
                    case "water": return ArenaWater;
                    case "poison": return new Color(0.68f, 0.34f, 0.92f);
                    case "flying": return new Color(0.48f, 0.64f, 1.00f);
                    case "electric": return new Color(1.00f, 0.82f, 0.12f);
                    case "ice": return new Color(0.36f, 0.86f, 1.00f);
                    case "dragon": return new Color(0.42f, 0.36f, 0.96f);
                    case "psychic": return new Color(0.96f, 0.32f, 0.64f);
                    case "ground": return new Color(0.78f, 0.54f, 0.28f);
                    case "rock": return new Color(0.62f, 0.58f, 0.48f);
                    case "dark": return new Color(0.32f, 0.28f, 0.34f);
                    case "fighting": return new Color(0.85f, 0.35f, 0.25f);
                    default: return new Color(0.55f, 0.62f, 0.72f);
                }
            }
        }

        // -------------------------------------------------------------
        // PROCEDURAL TEXTURE GENERATION CACHE
        // -------------------------------------------------------------
        private static class UITextures
        {
            private static readonly Dictionary<string, Texture2D> cache = new Dictionary<string, Texture2D>();

            public static Texture2D Solid(Color c)
            {
                string key = $"solid_{c.r:F3}_{c.g:F3}_{c.b:F3}_{c.a:F3}";
                if (cache.TryGetValue(key, out Texture2D tex) && tex != null) return tex;
                tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                Color32 c32 = c;
                tex.SetPixels32(new[] { c32, c32, c32, c32 });
                tex.Apply();
                cache[key] = tex;
                return tex;
            }

            public static Texture2D RoundedRect(int w, int h, int radius, Color fill, Color border, int borderWidth)
            {
                string key = $"rr_{w}x{h}_{radius}_{fill.GetHashCode()}_{border.GetHashCode()}_{borderWidth}";
                if (cache.TryGetValue(key, out Texture2D tex) && tex != null) return tex;
                tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
                Color32[] pixels = new Color32[w * h];

                float r = radius;
                float rInner = Mathf.Max(0, r - borderWidth);

                for (int y = 0; y < h; y++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        float cx = x < r ? r - x : (x >= w - r ? x - (w - 1 - r) : 0);
                        float cy = y < r ? r - y : (y >= h - r ? y - (h - 1 - r) : 0);
                        float dist = Mathf.Sqrt(cx * cx + cy * cy);

                        if (dist > r + 0.5f)
                        {
                            pixels[y * w + x] = new Color32(0, 0, 0, 0);
                        }
                        else
                        {
                            float alphaBorder = Mathf.Clamp01(r + 0.5f - dist);
                            if (dist > rInner)
                            {
                                Color c = border;
                                c.a *= alphaBorder;
                                pixels[y * w + x] = c;
                            }
                            else
                            {
                                pixels[y * w + x] = fill;
                            }
                        }
                    }
                }

                tex.SetPixels32(pixels);
                tex.wrapMode = TextureWrapMode.Clamp;
                tex.filterMode = FilterMode.Bilinear;
                tex.Apply();
                cache[key] = tex;
                return tex;
            }

            public static Texture2D GradientVertical(int h, Color top, Color bottom)
            {
                string key = $"grad_{h}_{top.GetHashCode()}_{bottom.GetHashCode()}";
                if (cache.TryGetValue(key, out Texture2D tex) && tex != null) return tex;
                tex = new Texture2D(2, h, TextureFormat.RGBA32, false);
                Color32[] pixels = new Color32[2 * h];
                for (int y = 0; y < h; y++)
                {
                    float t = (float)y / (h - 1);
                    Color c = Color.Lerp(bottom, top, t);
                    pixels[y * 2] = c;
                    pixels[y * 2 + 1] = c;
                }
                tex.SetPixels32(pixels);
                tex.wrapMode = TextureWrapMode.Clamp;
                tex.Apply();
                cache[key] = tex;
                return tex;
            }

            public static Texture2D Circle(int size, Color fill, Color border, int borderWidth)
            {
                return RoundedRect(size, size, size / 2, fill, border, borderWidth);
            }
        }

        // -------------------------------------------------------------
        // MODERN GUI DRAWING PRIMITIVES & CONTROLS
        // -------------------------------------------------------------
        private GUIStyle stylePillBadge, styleKeyBadge, styleHeaderTitle, styleHeaderSub, styleSubLabel, styleCardValue;

        private void InitExtendedStyles()
        {
            if (stylePillBadge != null) return;

            stylePillBadge = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            stylePillBadge.normal.textColor = Color.white;

            styleKeyBadge = new GUIStyle(stylePillBadge)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleCenter
            };

            styleHeaderTitle = new GUIStyle(headingStyle)
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold
            };
            styleHeaderTitle.normal.textColor = UITheme.TextWhite;

            styleHeaderSub = new GUIStyle(smallStyle)
            {
                fontSize = 12
            };
            styleHeaderSub.normal.textColor = UITheme.TextMuted;

            styleSubLabel = new GUIStyle(smallStyle)
            {
                fontSize = 11
            };
            styleSubLabel.normal.textColor = UITheme.TextDim;

            styleCardValue = new GUIStyle(headingStyle)
            {
                fontSize = 18,
                alignment = TextAnchor.MiddleRight
            };
            styleCardValue.normal.textColor = UITheme.TextWhite;
        }

        // Sleek glassmorphism panel with smooth corner radius and 1px border
        private void DrawGlassPanel(Rect r, Color bg, Color border, int radius = 6)
        {
            // Generate or fetch 9-slice rounded rectangle texture (40x40, radius 10)
            int texSize = Mathf.Max(24, radius * 3);
            Texture2D tex = UITextures.RoundedRect(texSize, texSize, radius, bg, border, 1);
            GUI.DrawTexture(r, tex, ScaleMode.StretchToFill, true, 0, Color.white, 0, radius);
        }

        // Glass panel with top accent stripe
        private void DrawGlassCard(Rect r, Color bg, Color border, Color topStripe, int radius = 6)
        {
            DrawGlassPanel(r, bg, border, radius);
            // Crisp top highlight line
            Rect stripe = new Rect(r.x + radius, r.y, r.width - radius * 2, 2);
            GUI.DrawTexture(stripe, UITextures.Solid(topStripe));
        }

        // Rounded badge chip (e.g. "TAMER", "WARDEN", "LV. 15", "GRASS")
        private void DrawBadge(Rect r, string text, Color bgColor, Color textColor, int fontSize = 11)
        {
            int radius = Mathf.RoundToInt(r.height / 2f);
            Texture2D pill = UITextures.RoundedRect((int)r.width, (int)r.height, radius, bgColor, new Color(bgColor.r * 1.3f, bgColor.g * 1.3f, bgColor.b * 1.3f, 0.8f), 1);
            GUI.DrawTexture(r, pill, ScaleMode.StretchToFill, true, 0, Color.white, 0, radius);

            GUIStyle st = new GUIStyle(stylePillBadge) { fontSize = fontSize };
            st.normal.textColor = textColor;
            GUI.Label(r, text, st);
        }

        // Keycap indicator (e.g. [Q], [E], [WASD], [Z])
        private void DrawKeycap(Rect r, string key, Color accent)
        {
            DrawGlassPanel(r, new Color(0.06f, 0.10f, 0.18f, 0.95f), accent, 4);
            GUIStyle kStyle = new GUIStyle(styleKeyBadge) { fontSize = Mathf.RoundToInt(r.height * 0.55f) };
            kStyle.normal.textColor = accent;
            GUI.Label(r, key, kStyle);
        }

        // High-fidelity progress bar with smooth rounded track and animated glowing fill
        private void DrawModernBar(Rect r, float fraction, Color fillColor, Color trackColor, string label = null)
        {
            fraction = Mathf.Clamp01(fraction);
            int radius = Mathf.RoundToInt(r.height / 2f);

            // Track background
            DrawGlassPanel(r, trackColor, new Color(trackColor.r * 1.5f, trackColor.g * 1.5f, trackColor.b * 1.5f, 0.4f), radius);

            // Fill
            if (fraction > 0.01f)
            {
                float fillW = Mathf.Max(r.height, r.width * fraction);
                Rect fillRect = new Rect(r.x, r.y, fillW, r.height);
                DrawGlassPanel(fillRect, fillColor, new Color(1f, 1f, 1f, 0.45f), radius);

                // Subtle inner top specular highlight on fill
                Rect glossRect = new Rect(r.x + radius, r.y + 1, fillW - radius * 2, Mathf.Max(1, r.height * 0.35f));
                GUI.DrawTexture(glossRect, UITextures.Solid(new Color(1f, 1f, 1f, 0.22f)));
            }

            // Optional label inside or over bar
            if (!string.IsNullOrEmpty(label))
            {
                GUIStyle lblStyle = new GUIStyle(tinyStyle)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = Mathf.RoundToInt(r.height * 0.72f),
                    fontStyle = FontStyle.Bold
                };
                lblStyle.normal.textColor = Color.white;
                GUI.Label(r, label, lblStyle);
            }
        }

        // Modern Interactive Button with Hover, Active Pulse, and Crisp Border
        private bool ModernButton(Rect r, string label, bool enabled = true, bool primary = false, bool small = false, Color? customAccent = null)
        {
            InitExtendedStyles();
            bool was = GUI.enabled;
            GUI.enabled = was && enabled;

            Vector2 mouse = Event.current.mousePosition;
            bool hovered = enabled && r.Contains(mouse);
            bool pressed = hovered && Input.GetMouseButton(0);

            Color accent = customAccent ?? (primary ? UITheme.CyanPrimary : UITheme.BorderLight);

            // Button base colors based on state
            Color bg;
            Color border;
            Color textColor;

            if (!enabled)
            {
                bg = new Color(0.04f, 0.06f, 0.10f, 0.45f);
                border = new Color(0.12f, 0.18f, 0.26f, 0.35f);
                textColor = new Color(0.42f, 0.50f, 0.62f, 0.6f);
            }
            else if (primary)
            {
                float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 3.2f);
                Color baseCyan = Color.Lerp(new Color(0.05f, 0.48f, 0.85f), new Color(0.10f, 0.76f, 1.0f), pulse);
                bg = pressed ? new Color(0.04f, 0.38f, 0.70f) : (hovered ? Color.Lerp(baseCyan, Color.white, 0.15f) : baseCyan);
                border = hovered ? Color.white : UITheme.CyanGlow;
                textColor = Color.white;
            }
            else if (hovered)
            {
                bg = new Color(0.10f, 0.18f, 0.30f, 0.90f);
                border = accent;
                textColor = Color.white;
            }
            else
            {
                bg = new Color(0.045f, 0.080f, 0.145f, 0.82f);
                border = UITheme.BorderMuted;
                textColor = UITheme.TextWhite;
            }

            int radius = small ? 4 : 6;
            DrawGlassPanel(r, bg, border, radius);

            // Subtle top highlight when normal or hovered
            if (enabled && !pressed)
            {
                Rect shine = new Rect(r.x + radius, r.y + 1, r.width - radius * 2, 1);
                GUI.DrawTexture(shine, UITextures.Solid(new Color(1f, 1f, 1f, primary ? 0.35f : 0.15f)));
            }

            GUIStyle bStyle = new GUIStyle(small ? tinyButton : buttonStyle)
            {
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true,
                fontSize = small ? 12 : (primary ? 18 : 15),
                fontStyle = primary ? FontStyle.Bold : FontStyle.Normal
            };
            bStyle.normal.background = bStyle.hover.background = bStyle.active.background = Texture2D.whiteTexture;
            bStyle.normal.textColor = bStyle.hover.textColor = bStyle.active.textColor = textColor;

            // Transparent overlay button to capture click & event handling
            Color prevBg = GUI.backgroundColor;
            GUI.backgroundColor = Color.clear;
            bool clicked = GUI.Button(r, label, bStyle);
            GUI.backgroundColor = prevBg;

            GUI.enabled = was;
            return clicked;
        }
    }
}
