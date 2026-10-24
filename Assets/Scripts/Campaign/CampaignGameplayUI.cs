using System;
using System.Linq;
using UnityEngine;

namespace Valmon.Campaign
{
    public partial class CampaignController
    {
        private void DrawRoleStatus()
        {
            Color roleColor = Role == PlayerRole.Tamer ? UITheme.CyanPrimary : UITheme.CrimsonWarden;

            // Spacious, razor-sharp floating status card on top-left
            Rect cardRect = new Rect(20, 52, 210, 210);
            DrawGlassCard(cardRect, UITheme.GlassDark, UITheme.BorderMuted, roleColor);

            // Role Badge & Match Mode pill
            DrawBadge(new Rect(32, 62, 100, 24), RoleName(Role), roleColor, Color.white, 11);
            string modeStr = IsRoundMatch ? $"RND {roundIndex + 1}/3" : "SOLO";
            DrawBadge(new Rect(138, 62, 78, 24), modeStr, new Color(0.12f, 0.20f, 0.34f, 0.95f), UITheme.TextWhite, 10);

            // Health or Energy Header
            float fraction = Role == PlayerRole.Tamer ? (tamerHealth / 100f) : (corruptionEnergy / 100f);
            Color barCol = Role == PlayerRole.Tamer
                ? (tamerHealth > 50 ? UITheme.ArenaGrass : (tamerHealth > 25 ? UITheme.AmberWarning : UITheme.CrimsonWarden))
                : UITheme.VioletCorruption;

            string lblTitle = Role == PlayerRole.Tamer ? "PLAYER HEALTH" : "CORRUPTION ENERGY";
            string valText = Role == PlayerRole.Tamer ? $"{tamerHealth} / 100 HP" : $"{Mathf.FloorToInt(corruptionEnergy)} / 100 NRG";

            Text(32, 94, 95, 18, lblTitle, styleSubLabel);
            GUIStyle valStyle = new GUIStyle(styleCardValue) { fontSize = 12, fontStyle = FontStyle.Bold };
            valStyle.normal.textColor = Color.white;
            GUI.Label(new Rect(125, 94, 91, 18), valText, valStyle);

            // 14px thick progress bar with specular highlight
            DrawModernBar(new Rect(32, 114, 184, 14), fraction, barCol, new Color(0.06f, 0.10f, 0.18f, 0.95f));

            // Inset Objective Card
            Rect objRect = new Rect(32, 138, 184, 112);
            DrawGlassPanel(objRect, UITheme.GlassInset, UITheme.BorderMuted);

            GUIStyle objHeaderStyle = new GUIStyle(headingStyle) { fontSize = 11, fontStyle = FontStyle.Bold };
            objHeaderStyle.normal.textColor = UITheme.CyanPrimary;
            GUI.Label(new Rect(42, 144, 164, 18), "CURRENT OBJECTIVE", objHeaderStyle);

            if (Role == PlayerRole.Tamer)
            {
                string goalStr = $"{Run.catches} / {CampaignRun.RequiredCatches} Captured";
                GUIStyle goalStyle = new GUIStyle(headingStyle) { fontSize = 14, fontStyle = FontStyle.Bold };
                goalStyle.normal.textColor = Color.white;
                GUI.Label(new Rect(42, 166, 164, 22), goalStr, goalStyle);

                string bonusStr = slowSeconds > 0
                    ? $"Slowed: {Mathf.CeilToInt(slowSeconds)}s remaining"
                    : $"Bonus: +{TamerBonus} point at 6 catches";
                GUIStyle subStyle = new GUIStyle(smallStyle) { fontSize = 11 };
                subStyle.normal.textColor = slowSeconds > 0 ? UITheme.AmberWarning : UITheme.GoldChampion;
                GUI.Label(new Rect(42, 194, 164, 38), bonusStr, subStyle);
            }
            else
            {
                string goalStr = $"{CorruptionCount} / {CampaignRun.RequiredCorruptions} Corrupted";
                GUIStyle goalStyle = new GUIStyle(headingStyle) { fontSize = 14, fontStyle = FontStyle.Bold };
                goalStyle.normal.textColor = Color.white;
                GUI.Label(new Rect(42, 166, 164, 22), goalStr, goalStyle);

                string bonusStr = $"Bonus: +{WardenBonus} point at 6";
                GUIStyle subStyle = new GUIStyle(smallStyle) { fontSize = 11 };
                subStyle.normal.textColor = UITheme.GoldChampion;
                GUI.Label(new Rect(42, 194, 164, 38), bonusStr, subStyle);
            }

            // Finish Goal CTA Button
            if (Role == PlayerRole.Tamer && ScreenState == CampaignScreen.Explore && Run.catches >= CampaignRun.RequiredCatches)
            {
                Rect finishRect = new Rect(245, 52, 210, 42);
                if (ModernButton(finishRect, IsRoundMatch ? "FINISH ROUND ▶" : "FINISH LEVEL ▶", true, true, false, UITheme.GoldChampion))
                    FinishCaptureGoal();
            }

            DrawRadar();
        }

        private void DrawRadar()
        {
            Rect radar = new Rect(1120, 52, 140, 140);

            // Fully opaque dark disc (blocks any 3D beams or lights from shining through!)
            GUI.DrawTexture(radar, UITextures.Solid(new Color(0.012f, 0.024f, 0.055f, 1.0f)));

            // Outer 1px frame
            GUI.DrawTexture(new Rect(radar.x, radar.y, radar.width, 1), UITextures.Solid(UITheme.BorderCyan));
            GUI.DrawTexture(new Rect(radar.x, radar.y + radar.height - 1, radar.width, 1), UITextures.Solid(UITheme.BorderCyan));
            GUI.DrawTexture(new Rect(radar.x, radar.y + 1, 1, radar.height - 2), UITextures.Solid(UITheme.BorderCyan));
            GUI.DrawTexture(new Rect(radar.x + radar.width - 1, radar.y + 1, 1, radar.height - 2), UITextures.Solid(UITheme.BorderCyan));

            // Range rings & tactical grid
            Color gridCol = new Color(0.12f, 0.28f, 0.44f, 0.45f);
            for (int i = 1; i < 4; i++)
            {
                GUI.DrawTexture(new Rect(radar.x + i * radar.width / 4, radar.y + 2, 1, radar.height - 4), UITextures.Solid(gridCol));
                GUI.DrawTexture(new Rect(radar.x + 2, radar.y + i * radar.height / 4, radar.width - 4, 1), UITextures.Solid(gridCol));
            }

            // Cardinal North marker
            GUIStyle nStyle = new GUIStyle(smallStyle) { fontSize = 10, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            nStyle.normal.textColor = UITheme.CyanGlow;
            GUI.Label(new Rect(radar.x + radar.width / 2 - 10, radar.y + 2, 20, 16), "N", nStyle);

            // Radar dots
            GameObject own = Role == PlayerRole.Tamer ? World.tamer : World.warden;

            // Wild & Corrupted creatures
            foreach (WildCreature wild in World.wild)
            {
                if (!wild.captured && (scanSeconds > 0 || Vector3.Distance(own.transform.position, wild.root.transform.position) < 24))
                {
                    Color cCol = wild.corrupted ? UITheme.CrimsonWarden : UITheme.ArenaGrass;
                    RadarDot(radar, wild.root.transform.position, cCol, 4f);
                }
            }

            // Warden position
            if (World.warden != null && (Role == PlayerRole.Corruptor || Vector3.Distance(own.transform.position, World.warden.transform.position) < 15))
            {
                RadarDot(radar, World.warden.transform.position, UITheme.CrimsonWarden, 6f);
            }

            // Tamer position
            if (Role == PlayerRole.Tamer || scanSeconds > 0 || Vector3.Distance(own.transform.position, World.tamer.transform.position) < 24)
            {
                RadarDot(radar, World.tamer.transform.position, UITheme.CyanPrimary, 6f);
            }

            // Gates
            foreach (GameObject gate in World.gates)
            {
                if (gate != null) RadarDot(radar, gate.transform.position, UITheme.GoldChampion, 3.5f);
            }

            // Bottom radar status badge
            string rBadge = scanSeconds > 0 ? "SCAN ACTIVE" : "RADAR ACTIVE";
            Color rBadgeCol = scanSeconds > 0 ? UITheme.GoldChampion : UITheme.CyanPrimary;
            DrawBadge(new Rect(radar.x + 10, radar.y + radar.height + 4, radar.width - 20, 22), rBadge, rBadgeCol, Color.white, 10);
        }

        private void RadarDot(Rect radar, Vector3 position, Color color, float size)
        {
            float x = radar.x + Mathf.Clamp01((position.x + 46) / 92) * radar.width;
            float y = radar.y + (1 - Mathf.Clamp01((position.z + 46) / 92)) * radar.height;

            Rect dRect = new Rect(x - size / 2, y - size / 2, size, size);
            GUI.DrawTexture(dRect, UITextures.Solid(color));
        }

        private void DrawWardenAbilities()
        {
            string[] actions = { "corruptNearest", "trap", "gate", "scan" };
            string[] names = { "CORRUPT CREATURE", "PLACE TRAP", "LOCK GATE", "RADAR SCAN" };
            string[] keys = { "Q", "E", "R", "F" };
            int[] cost = { 25, 30, 35, 15 };
            float[] cooldown = { wardenCorruptCooldown, trapCooldown, gateCooldown, scanCooldown };

            for (int i = 0; i < 4; i++)
            {
                bool can = corruptionEnergy >= cost[i] && cooldown[i] <= 0 && (ScreenState == CampaignScreen.Explore || ScreenState == CampaignScreen.Capture);

                Rect aRect = new Rect(960, 320 + i * 58, 290, 52);
                string costOrCd = cooldown[i] > 0 ? $"{Mathf.CeilToInt(cooldown[i])}s cooldown" : $"{cost[i]} energy";
                string btnLabel = $"[{keys[i]}]  {names[i]}\n{costOrCd}";

                if (ModernButton(aRect, btnLabel, can, false, true, UITheme.CrimsonWarden))
                {
                    UseAbility(actions[i]);
                }
            }
        }
    }
}
