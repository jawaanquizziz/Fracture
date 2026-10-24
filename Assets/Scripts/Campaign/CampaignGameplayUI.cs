using System;
using System.Linq;
using UnityEngine;

namespace Valmon.Campaign
{
    public partial class CampaignController
    {
        private void DrawRoleStatus()
        {
            bool isTamer = Role == PlayerRole.Tamer;
            Color roleColor = isTamer ? UITheme.CyanPrimary : UITheme.CrimsonWarden;

            // ---------------------------------------------------------
            // TOP-LEFT: PLAYER STATUS CARD (MATCHING REFERENCE MOCKUP)
            // ---------------------------------------------------------
            Rect playerCard = new Rect(20, 52, 230, 72);
            DrawGlassCard(playerCard, UITheme.GlassDark, UITheme.BorderMuted, roleColor, 8);

            // Circular Avatar disc
            Rect avatarRect = new Rect(28, 60, 48, 48);
            Texture2D avatarTex = isTamer ? UITextures.AvatarTamer(48) : UITextures.AvatarWarden(48);
            GUI.DrawTexture(avatarRect, avatarTex);

            // Role Title & Level
            string roleTitle = RoleName(Role).ToUpperInvariant();
            GUIStyle nameStyle = new GUIStyle(headingStyle) { fontSize = 15, fontStyle = FontStyle.Bold };
            if (fontInterBold != null) nameStyle.font = fontInterBold;
            nameStyle.normal.textColor = roleColor;
            GUI.Label(new Rect(86, 60, 95, 20), roleTitle, nameStyle);

            GUIStyle lvStyle = new GUIStyle(smallStyle) { fontSize = 11, alignment = TextAnchor.MiddleRight };
            if (fontInterRegular != null) lvStyle.font = fontInterRegular;
            lvStyle.normal.textColor = UITheme.TextDim;
            GUI.Label(new Rect(180, 62, 58, 18), "Lv. 10", lvStyle);

            // Health or Energy bar
            float fraction = isTamer ? (tamerHealth / 100f) : (corruptionEnergy / 100f);
            Color barCol = isTamer
                ? (tamerHealth > 50 ? UITheme.ArenaGrass : (tamerHealth > 25 ? UITheme.AmberWarning : UITheme.CrimsonWarden))
                : UITheme.VioletCorruption;
            string hpText = isTamer ? $"HP {tamerHealth}/100" : $"NRG {Mathf.FloorToInt(corruptionEnergy)}/100";
            DrawModernBar(new Rect(86, 84, 152, 14), fraction, barCol, new Color(0.05f, 0.08f, 0.15f, 0.95f), hpText);

            // ---------------------------------------------------------
            // OBJECTIVE CARD
            // ---------------------------------------------------------
            Rect objCard = new Rect(20, 130, 230, 105);
            DrawGlassPanel(objCard, UITheme.GlassInset, UITheme.BorderMuted, 8);

            GUIStyle objHead = new GUIStyle(headingStyle) { fontSize = 11, fontStyle = FontStyle.Bold };
            if (fontInterBold != null) objHead.font = fontInterBold;
            objHead.normal.textColor = UITheme.CyanPrimary;
            GUI.Label(new Rect(32, 136, 200, 18), "⚙ OBJECTIVE", objHead);

            if (isTamer)
            {
                GUIStyle checkStyle = new GUIStyle(smallStyle) { fontSize = 12, fontStyle = FontStyle.Bold };
                if (fontInterBold != null) checkStyle.font = fontInterBold;
                checkStyle.normal.textColor = Color.white;
                GUI.Label(new Rect(32, 156, 200, 20), "✔ Catch 5 creatures", checkStyle);

                GUIStyle sub1 = new GUIStyle(smallStyle) { fontSize = 11 };
                if (fontInterRegular != null) sub1.font = fontInterRegular;
                sub1.normal.textColor = UITheme.TextMuted;
                GUI.Label(new Rect(32, 178, 200, 18), $"Catch: {Run.catches} / {CampaignRun.RequiredCatches}", sub1);

                string bonusText = slowSeconds > 0
                    ? $"Slowed: {Mathf.CeilToInt(slowSeconds)}s left"
                    : $"Bonus: +{TamerBonus} (at 6)";
                GUIStyle sub2 = new GUIStyle(smallStyle) { fontSize = 11 };
                if (fontInterRegular != null) sub2.font = fontInterRegular;
                sub2.normal.textColor = slowSeconds > 0 ? UITheme.AmberWarning : UITheme.GoldChampion;
                GUI.Label(new Rect(32, 198, 200, 18), bonusText, sub2);
            }
            else
            {
                GUIStyle checkStyle = new GUIStyle(smallStyle) { fontSize = 12, fontStyle = FontStyle.Bold };
                if (fontInterBold != null) checkStyle.font = fontInterBold;
                checkStyle.normal.textColor = Color.white;
                GUI.Label(new Rect(32, 156, 200, 20), "✔ Stop the Tamer", checkStyle);

                GUIStyle sub1 = new GUIStyle(smallStyle) { fontSize = 11 };
                if (fontInterRegular != null) sub1.font = fontInterRegular;
                sub1.normal.textColor = UITheme.TextMuted;
                GUI.Label(new Rect(32, 178, 200, 18), $"Corrupt: {CorruptionCount} / {CampaignRun.RequiredCorruptions}", sub1);

                GUIStyle sub2 = new GUIStyle(smallStyle) { fontSize = 11 };
                if (fontInterRegular != null) sub2.font = fontInterRegular;
                sub2.normal.textColor = UITheme.GoldChampion;
                GUI.Label(new Rect(32, 198, 200, 18), $"Bonus: +{WardenBonus} (at 6)", sub2);
            }

            // Finish Goal CTA Button
            if (isTamer && ScreenState == CampaignScreen.Explore && Run.catches >= CampaignRun.RequiredCatches)
            {
                Rect finishRect = new Rect(20, 242, 230, 40);
                if (ModernButton(finishRect, IsRoundMatch ? "FINISH ROUND ▶" : "FINISH LEVEL ▶", true, true, false, UITheme.GoldChampion))
                    FinishCaptureGoal();
            }

            // Circular D-Pad on bottom-left
            DrawDPad(new Rect(35, 570, 95, 95));

            // Circular Action Skill Bar on bottom-right
            DrawSkillBar();

            // Circular Radar on top-right
            DrawRadar();
        }

        private void DrawSkillBar()
        {
            if (Role == PlayerRole.Tamer)
            {
                // Skill 1: [Q] Quick Dash
                bool canDash = dashCooldown <= 0 && ScreenState == CampaignScreen.Explore;
                if (DrawCircleButton(new Rect(950, 580, 54, 54), UITextures.DashIcon(32, UITheme.CyanPrimary), "Q", "Quick Dash", canDash, UITheme.CyanPrimary, dashCooldown))
                {
                    UseAbility("dash");
                }

                // Skill 2: [Shift] Sprint
                bool isSprinting = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
                DrawCircleButton(new Rect(1035, 580, 54, 54), UITextures.SprintIcon(32, UITheme.ArenaGrass), "Shift", "Sprint", true, UITheme.ArenaGrass, 0f, isSprinting);

                // Skill 3: [Z] Catch (Large 64x64 primary Pokéball button!)
                bool nearWild = ScreenState == CampaignScreen.Explore && selected != null && !selected.captured;
                float distance = (nearWild && World != null && World.tamer != null && selected.root != null)
                    ? Vector3.Distance(World.tamer.transform.position, selected.root.transform.position)
                    : 999f;
                bool canCatch = nearWild && distance <= 4.4f;

                if (DrawCircleButton(new Rect(1120, 572, 64, 64), UITextures.PokeBallIcon(42), "Z", "Catch", canCatch, canCatch ? UITheme.GoldChampion : UITheme.CrimsonWarden, 0f, canCatch))
                {
                    if (canCatch) BeginCapture(selected);
                }
            }
            else
            {
                // 4 Circular Skills for Warden: Corrupt, Trap, Lock Gate, Scan!
                // Q: Corrupt (25 NRG)
                bool canCorrupt = corruptionEnergy >= 25 && wardenCorruptCooldown <= 0 && ScreenState == CampaignScreen.Explore;
                if (DrawCircleButton(new Rect(870, 580, 54, 54), UITextures.FlameIcon(32, UITheme.CrimsonWarden), "Q", "Corrupt", canCorrupt, UITheme.CrimsonWarden, wardenCorruptCooldown))
                {
                    UseAbility("corruptNearest");
                }

                // E: Trap (30 NRG)
                bool canTrap = corruptionEnergy >= 30 && ScreenState == CampaignScreen.Explore;
                if (DrawCircleButton(new Rect(955, 580, 54, 54), UITextures.TrapIcon(32, UITheme.CrimsonWarden), "E", "Trap", canTrap, UITheme.CrimsonWarden))
                {
                    UseAbility("trap");
                }

                // R: Lock Gate (35 NRG)
                bool canGate = corruptionEnergy >= 35 && ScreenState == CampaignScreen.Explore;
                if (DrawCircleButton(new Rect(1040, 580, 54, 54), UITextures.LockIcon(32, UITheme.CrimsonWarden), "R", "Lock Gate", canGate, UITheme.CrimsonWarden))
                {
                    UseAbility("gate");
                }

                // F: Scan (15 NRG)
                bool canScan = corruptionEnergy >= 15 && ScreenState == CampaignScreen.Explore;
                if (DrawCircleButton(new Rect(1125, 580, 54, 54), UITextures.ScanIcon(32, UITheme.CrimsonWarden), "F", "Scan", canScan, UITheme.CrimsonWarden))
                {
                    UseAbility("scan");
                }
            }
        }

        private void DrawRadar()
        {
            Rect radar = new Rect(1125, 48, 130, 130);

            // 100% opaque dark disc with smooth anti-aliased outer circular border
            // Completely blocks any 3D light beams from piercing through!
            Texture2D radarBg = UITextures.Circle(130, new Color(0.012f, 0.024f, 0.055f, 1.0f), UITheme.BorderCyan, 1.8f);
            GUI.DrawTexture(radar, radarBg);

            // Tactical crosshair
            Color gridCol = new Color(0.12f, 0.28f, 0.44f, 0.40f);
            GUI.DrawTexture(new Rect(radar.x + radar.width * 0.5f - 0.5f, radar.y + 4, 1, radar.height - 8), UITextures.Solid(gridCol));
            GUI.DrawTexture(new Rect(radar.x + 4, radar.y + radar.height * 0.5f - 0.5f, radar.width - 8, 1), UITextures.Solid(gridCol));

            // Inner range ring
            Texture2D innerRing = UITextures.Circle(70, Color.clear, gridCol, 1.0f);
            GUI.DrawTexture(new Rect(radar.x + 30, radar.y + 30, 70, 70), innerRing);

            // Cardinal directions: N, S, E, W
            GUIStyle nStyle = new GUIStyle(smallStyle) { fontSize = 10, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            if (fontInterBold != null) nStyle.font = fontInterBold;
            nStyle.normal.textColor = UITheme.CyanGlow;
            GUI.Label(new Rect(radar.x + 55, radar.y + 4, 20, 14), "N", nStyle);

            GUIStyle cardStyle = new GUIStyle(smallStyle) { fontSize = 9, alignment = TextAnchor.MiddleCenter };
            if (fontInterRegular != null) cardStyle.font = fontInterRegular;
            cardStyle.normal.textColor = UITheme.TextDim;
            GUI.Label(new Rect(radar.x + 55, radar.y + radar.height - 18, 20, 14), "S", cardStyle);
            GUI.Label(new Rect(radar.x + 4, radar.y + 58, 14, 14), "W", cardStyle);
            GUI.Label(new Rect(radar.x + radar.width - 18, radar.y + 58, 14, 14), "E", cardStyle);

            // Blips
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

            // Sleek cyan "RADAR" pill badge below disc (Matching reference mockup!)
            DrawBadge(new Rect(radar.x + 25, radar.y + radar.height + 4, 80, 18), "RADAR", UITheme.CyanPrimary, Color.white, 10);
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
            // Now rendered in the bottom circular skill bar
        }
    }
}
