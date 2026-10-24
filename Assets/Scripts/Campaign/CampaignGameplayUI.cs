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

            // Compact floating status card on top-left
            Rect cardRect = new Rect(20, 52, 160, 205);
            DrawGlassCard(cardRect, UITheme.GlassCard, UITheme.BorderMuted, roleColor, 8);

            // Role & Match Mode badges
            DrawBadge(new Rect(28, 62, 85, 22), RoleName(Role), roleColor, Color.white, 10);
            string modeStr = IsRoundMatch ? $"RND {roundIndex + 1}/3" : "SOLO";
            DrawBadge(new Rect(118, 62, 54, 22), modeStr, new Color(0.10f, 0.16f, 0.26f, 0.85f), UITheme.TextMuted, 9);

            // Health or Energy progress bar
            float fraction = Role == PlayerRole.Tamer ? (tamerHealth / 100f) : (corruptionEnergy / 100f);
            Color barCol = Role == PlayerRole.Tamer
                ? (tamerHealth > 50 ? UITheme.ArenaGrass : (tamerHealth > 25 ? UITheme.AmberWarning : UITheme.CrimsonWarden))
                : UITheme.VioletCorruption;

            DrawModernBar(new Rect(28, 94, 144, 10), fraction, barCol, new Color(0.08f, 0.12f, 0.20f, 0.9f));

            string valText = Role == PlayerRole.Tamer ? $"HP  {tamerHealth} / 100" : $"ENERGY  {Mathf.FloorToInt(corruptionEnergy)} / 100";
            Text(28, 108, 144, 18, valText, new GUIStyle(smallStyle) { fontSize = 11, fontStyle = FontStyle.Bold });

            // Objective inset card
            Rect objRect = new Rect(28, 132, 144, 115);
            DrawGlassPanel(objRect, UITheme.GlassInset, UITheme.BorderMuted, 6);
            Text(36, 138, 128, 18, "OBJECTIVE", new GUIStyle(smallStyle) { fontSize = 10, fontStyle = FontStyle.Bold, normal = { textColor = UITheme.CyanPrimary } });

            string objText;
            if (Role == PlayerRole.Tamer)
            {
                objText = $"Capture: {Run.catches} / {CampaignRun.RequiredCatches}";
                if (slowSeconds > 0) objText += $"\nSlow: {Mathf.CeilToInt(slowSeconds)}s";
                else objText += $"\nBonus: +{TamerBonus} (at 6)";
            }
            else
            {
                objText = $"Corrupt: {CorruptionCount} / {CampaignRun.RequiredCorruptions}\nBonus: +{WardenBonus}";
            }
            Text(36, 158, 128, 80, objText, new GUIStyle(smallStyle) { fontSize = 11 });

            // Finish Goal CTA Button
            if (Role == PlayerRole.Tamer && ScreenState == CampaignScreen.Explore && Run.catches >= CampaignRun.RequiredCatches)
            {
                Rect finishRect = new Rect(190, 52, 200, 38);
                if (ModernButton(finishRect, IsRoundMatch ? "FINISH ROUND ▶" : "FINISH LEVEL ▶", true, true, false, UITheme.GoldChampion))
                    FinishCaptureGoal();
            }

            DrawRadar();
        }

        private void DrawRadar()
        {
            Rect radar = new Rect(1120, 52, 140, 140);

            // Dark circular radar disc
            DrawGlassPanel(radar, new Color(0.015f, 0.035f, 0.07f, 0.94f), UITheme.BorderMuted, 8);

            // Range rings & tactical grid
            Color gridCol = new Color(0.12f, 0.22f, 0.35f, 0.5f);
            for (int i = 1; i < 4; i++)
            {
                GUI.DrawTexture(new Rect(radar.x + i * radar.width / 4, radar.y + 4, 1, radar.height - 8), UITextures.Solid(gridCol));
                GUI.DrawTexture(new Rect(radar.x + 4, radar.y + i * radar.height / 4, radar.width - 8, 1), UITextures.Solid(gridCol));
            }

            // Cardinal North marker
            GUIStyle nStyle = new GUIStyle(smallStyle) { fontSize = 10, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            nStyle.normal.textColor = UITheme.CyanGlow;
            GUI.Label(new Rect(radar.x + radar.width / 2 - 10, radar.y + 3, 20, 16), "N", nStyle);

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
            string rBadge = scanSeconds > 0 ? "SCAN ACTIVE" : "RADAR";
            Color rBadgeCol = scanSeconds > 0 ? UITheme.GoldChampion : UITheme.CyanPrimary;
            DrawBadge(new Rect(radar.x + 15, radar.y + radar.height + 4, radar.width - 30, 20), rBadge, rBadgeCol, Color.white, 9);
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

                Rect aRect = new Rect(960, 330 + i * 56, 285, 50);
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
