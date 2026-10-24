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
            public static readonly Color GlassDark = new Color(0.024f, 0.043f, 0.082f, 0.94f);
            public static readonly Color GlassMid = new Color(0.035f, 0.063f, 0.118f, 0.88f);
            public static readonly Color GlassCard = new Color(0.047f, 0.082f, 0.149f, 0.92f);
            public static readonly Color GlassInset = new Color(0.016f, 0.027f, 0.055f, 0.85f);
            public static readonly Color GlassHeader = new Color(0.020f, 0.035f, 0.071f, 0.96f);

            // Borders
            public static readonly Color BorderMuted = new Color(0.18f, 0.28f, 0.44f, 0.60f);
            public static readonly Color BorderLight = new Color(0.28f, 0.46f, 0.72f, 0.85f);
            public static readonly Color BorderCyan = new Color(0.10f, 0.75f, 1.00f, 0.95f);
            public static readonly Color BorderCrimson = new Color(1.00f, 0.22f, 0.35f, 0.95f);

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
            public static readonly Color TextMuted = new Color(0.72f, 0.82f, 0.94f);
            public static readonly Color TextDim = new Color(0.46f, 0.56f, 0.70f);

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
        // PROCEDURAL PIXEL-PERFECT TEXTURE CACHE
        // -------------------------------------------------------------
        private static class UITextures
        {
            private static readonly Dictionary<string, Texture2D> cache = new Dictionary<string, Texture2D>();

            public static Texture2D Solid(Color c)
            {
                string key = $"s_{c.r:F3}_{c.g:F3}_{c.b:F3}_{c.a:F3}";
                if (cache.TryGetValue(key, out Texture2D tex) && tex != null) return tex;
                tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                Color32 c32 = c;
                tex.SetPixels32(new[] { c32, c32, c32, c32 });
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
        }

        // -------------------------------------------------------------
        // RAZOR-SHARP VECTOR DRAWING ENGINE (ZERO STRETCHING ARTIFACTS)
        // -------------------------------------------------------------
        private GUIStyle stylePillBadge, styleKeyBadge, styleHeaderTitle, styleHeaderSub, styleSubLabel, styleCardValue;
        private Font fontInterBold, fontInterRegular, fontRobotoMono;

        private void InitExtendedStyles()
        {
            if (fontInterBold == null)
            {
                fontInterBold = Resources.Load<Font>("Fonts/Inter-SemiBold");
                fontInterRegular = Resources.Load<Font>("Fonts/Inter-Regular");
                fontRobotoMono = Resources.Load<Font>("Fonts/RobotoMono-Regular");

                if (fontInterBold != null)
                {
                    headingStyle.font = fontInterBold;
                    titleStyle.font = fontInterBold;
                    logoStyle.font = fontInterBold;
                    buttonStyle.font = fontInterBold;
                    tinyButton.font = fontInterBold;
                }
                if (fontInterRegular != null)
                {
                    textStyle.font = fontInterRegular;
                    smallStyle.font = fontInterRegular;
                    tinyStyle.font = fontRobotoMono ?? fontInterRegular;
                    fieldStyle.font = fontInterRegular;
                }
            }

            if (stylePillBadge != null) return;

            stylePillBadge = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            if (fontInterBold != null) stylePillBadge.font = fontInterBold;
            stylePillBadge.normal.textColor = Color.white;

            styleKeyBadge = new GUIStyle(stylePillBadge)
            {
                fontSize = 11,
                alignment = TextAnchor.MiddleCenter
            };

            styleHeaderTitle = new GUIStyle(headingStyle)
            {
                fontSize = 19,
                fontStyle = FontStyle.Bold
            };
            if (fontInterBold != null) styleHeaderTitle.font = fontInterBold;
            styleHeaderTitle.normal.textColor = UITheme.TextWhite;

            styleHeaderSub = new GUIStyle(smallStyle)
            {
                fontSize = 12
            };
            if (fontInterRegular != null) styleHeaderSub.font = fontInterRegular;
            styleHeaderSub.normal.textColor = UITheme.TextMuted;

            styleSubLabel = new GUIStyle(smallStyle)
            {
                fontSize = 11
            };
            if (fontInterRegular != null) styleSubLabel.font = fontInterRegular;
            styleSubLabel.normal.textColor = UITheme.TextDim;

            styleCardValue = new GUIStyle(headingStyle)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleRight
            };
            if (fontInterBold != null) styleCardValue.font = fontInterBold;
            styleCardValue.normal.textColor = UITheme.TextWhite;
        }

        // Razor-sharp 1px outlined glass panel with zero bilinear blur
        private void DrawGlassPanel(Rect r, Color bg, Color border, int radius = 0)
        {
            // Center fill
            GUI.DrawTexture(new Rect(r.x + 1, r.y + 1, r.width - 2, r.height - 2), UITextures.Solid(bg));

            // Sharp 1px borders
            GUI.DrawTexture(new Rect(r.x, r.y, r.width, 1), UITextures.Solid(border));
            GUI.DrawTexture(new Rect(r.x, r.y + r.height - 1, r.width, 1), UITextures.Solid(border));
            GUI.DrawTexture(new Rect(r.x, r.y + 1, 1, r.height - 2), UITextures.Solid(border));
            GUI.DrawTexture(new Rect(r.x + r.width - 1, r.y + 1, 1, r.height - 2), UITextures.Solid(border));
        }

        // Glass panel with 2px accent top stripe and 1px border
        private void DrawGlassCard(Rect r, Color bg, Color border, Color topStripe, int radius = 0)
        {
            // Center fill
            GUI.DrawTexture(new Rect(r.x + 1, r.y + 2, r.width - 2, r.height - 3), UITextures.Solid(bg));

            // Top accent stripe (2px thick)
            GUI.DrawTexture(new Rect(r.x, r.y, r.width, 2), UITextures.Solid(topStripe));

            // Left, right, bottom 1px borders
            GUI.DrawTexture(new Rect(r.x, r.y + 2, 1, r.height - 3), UITextures.Solid(border));
            GUI.DrawTexture(new Rect(r.x + r.width - 1, r.y + 2, 1, r.height - 3), UITextures.Solid(border));
            GUI.DrawTexture(new Rect(r.x, r.y + r.height - 1, r.width, 1), UITextures.Solid(border));
        }

        // Crisp vector badge chip
        private void DrawBadge(Rect r, string text, Color bgColor, Color textColor, int fontSize = 11)
        {
            // Fill
            GUI.DrawTexture(new Rect(r.x + 2, r.y + 1, r.width - 4, r.height - 2), UITextures.Solid(bgColor));

            // Left and right 1px end caps
            GUI.DrawTexture(new Rect(r.x + 1, r.y + 2, 1, r.height - 4), UITextures.Solid(bgColor));
            GUI.DrawTexture(new Rect(r.x + r.width - 2, r.y + 2, 1, r.height - 4), UITextures.Solid(bgColor));

            // 1px subtle border highlight
            Color border = new Color(bgColor.r * 1.35f, bgColor.g * 1.35f, bgColor.b * 1.35f, 0.9f);
            GUI.DrawTexture(new Rect(r.x + 2, r.y, r.width - 4, 1), UITextures.Solid(border));
            GUI.DrawTexture(new Rect(r.x + 2, r.y + r.height - 1, r.width - 4, 1), UITextures.Solid(border));

            GUIStyle st = new GUIStyle(stylePillBadge) { fontSize = fontSize };
            if (fontInterBold != null) st.font = fontInterBold;
            st.normal.textColor = textColor;
            GUI.Label(r, text, st);
        }

        // Keycap indicator (e.g. [Q], [E], [WASD], [Z])
        private void DrawKeycap(Rect r, string key, Color accent)
        {
            DrawGlassPanel(r, new Color(0.06f, 0.10f, 0.18f, 0.95f), accent);
            GUIStyle kStyle = new GUIStyle(styleKeyBadge) { fontSize = Mathf.RoundToInt(r.height * 0.55f) };
            if (fontInterBold != null) kStyle.font = fontInterBold;
            kStyle.normal.textColor = accent;
            GUI.Label(r, key, kStyle);
        }

        // High-fidelity progress bar with sharp vector track and inner specular highlight
        private void DrawModernBar(Rect r, float fraction, Color fillColor, Color trackColor, string label = null)
        {
            fraction = Mathf.Clamp01(fraction);

            // Track background with 1px border
            GUI.DrawTexture(new Rect(r.x + 1, r.y + 1, r.width - 2, r.height - 2), UITextures.Solid(trackColor));
            Color trackBorder = new Color(trackColor.r * 1.6f, trackColor.g * 1.6f, trackColor.b * 1.6f, 0.6f);
            GUI.DrawTexture(new Rect(r.x, r.y, r.width, 1), UITextures.Solid(trackBorder));
            GUI.DrawTexture(new Rect(r.x, r.y + r.height - 1, r.width, 1), UITextures.Solid(trackBorder));
            GUI.DrawTexture(new Rect(r.x, r.y + 1, 1, r.height - 2), UITextures.Solid(trackBorder));
            GUI.DrawTexture(new Rect(r.x + r.width - 1, r.y + 1, 1, r.height - 2), UITextures.Solid(trackBorder));

            // Fill
            if (fraction > 0.005f)
            {
                float fillW = Mathf.Max(2, (r.width - 2) * fraction);
                Rect fillRect = new Rect(r.x + 1, r.y + 1, fillW, r.height - 2);
                GUI.DrawTexture(fillRect, UITextures.Solid(fillColor));

                // 1px top specular shine on fill
                GUI.DrawTexture(new Rect(r.x + 1, r.y + 1, fillW, 1), UITextures.Solid(new Color(1f, 1f, 1f, 0.40f)));
            }

            // Optional label inside bar
            if (!string.IsNullOrEmpty(label))
            {
                GUIStyle lblStyle = new GUIStyle(tinyStyle)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = Mathf.RoundToInt(r.height * 0.75f),
                    fontStyle = FontStyle.Bold
                };
                if (fontInterBold != null) lblStyle.font = fontInterBold;
                lblStyle.normal.textColor = Color.white;
                GUI.Label(r, label, lblStyle);
            }
        }

        // Modern Interactive Button with Hover, Active Pulse, and Crisp 1px Border
        private bool ModernButton(Rect r, string label, bool enabled = true, bool primary = false, bool small = false, Color? customAccent = null)
        {
            InitExtendedStyles();
            bool was = GUI.enabled;
            GUI.enabled = was && enabled;

            Vector2 mouse = Event.current.mousePosition;
            bool hovered = enabled && r.Contains(mouse);
            bool pressed = hovered && Input.GetMouseButton(0);

            Color accent = customAccent ?? (primary ? UITheme.CyanPrimary : UITheme.BorderLight);

            Color bg;
            Color border;
            Color textColor;

            if (!enabled)
            {
                bg = new Color(0.04f, 0.06f, 0.10f, 0.50f);
                border = new Color(0.12f, 0.18f, 0.26f, 0.40f);
                textColor = new Color(0.42f, 0.50f, 0.62f, 0.6f);
            }
            else if (primary)
            {
                float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 3.4f);
                Color baseCyan = Color.Lerp(new Color(0.05f, 0.46f, 0.82f), new Color(0.08f, 0.72f, 0.98f), pulse);
                bg = pressed ? new Color(0.04f, 0.38f, 0.68f) : (hovered ? Color.Lerp(baseCyan, Color.white, 0.18f) : baseCyan);
                border = hovered ? Color.white : UITheme.CyanGlow;
                textColor = Color.white;
            }
            else if (hovered)
            {
                bg = new Color(0.10f, 0.18f, 0.30f, 0.92f);
                border = accent;
                textColor = Color.white;
            }
            else
            {
                bg = new Color(0.045f, 0.080f, 0.145f, 0.88f);
                border = UITheme.BorderMuted;
                textColor = UITheme.TextWhite;
            }

            // Draw crisp button box
            GUI.DrawTexture(new Rect(r.x + 1, r.y + 1, r.width - 2, r.height - 2), UITextures.Solid(bg));
            GUI.DrawTexture(new Rect(r.x, r.y, r.width, 1), UITextures.Solid(border));
            GUI.DrawTexture(new Rect(r.x, r.y + r.height - 1, r.width, 1), UITextures.Solid(border));
            GUI.DrawTexture(new Rect(r.x, r.y + 1, 1, r.height - 2), UITextures.Solid(border));
            GUI.DrawTexture(new Rect(r.x + r.width - 1, r.y + 1, 1, r.height - 2), UITextures.Solid(border));

            // Top highlight
            if (enabled && !pressed)
            {
                GUI.DrawTexture(new Rect(r.x + 1, r.y + 1, r.width - 2, 1), UITextures.Solid(new Color(1f, 1f, 1f, primary ? 0.35f : 0.12f)));
            }

            GUIStyle bStyle = new GUIStyle(small ? tinyButton : buttonStyle)
            {
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true,
                fontSize = small ? 11 : (primary ? 16 : 14),
                fontStyle = primary ? FontStyle.Bold : FontStyle.Normal
            };
            if (fontInterBold != null) bStyle.font = fontInterBold;
            bStyle.normal.background = bStyle.hover.background = bStyle.active.background = Texture2D.whiteTexture;
            bStyle.normal.textColor = bStyle.hover.textColor = bStyle.active.textColor = textColor;

            Color prevBg = GUI.backgroundColor;
            GUI.backgroundColor = Color.clear;
            bool clicked = GUI.Button(r, label, bStyle);
            GUI.backgroundColor = prevBg;

            GUI.enabled = was;
            return clicked;
        }
    }
}
