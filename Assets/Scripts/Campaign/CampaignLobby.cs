using UnityEngine;

namespace Valmon.Campaign
{
    public partial class CampaignController
    {
        private GUIStyle headingStyle, titleStyle, textStyle, smallStyle, tinyStyle, buttonStyle, tinyButton, logoStyle, fieldStyle;
        private Vector2 partyScroll, corruptionScroll;
        private float uiScale = 1;
        private Vector2 uiOffset;
        private bool collection;
        private int collectionIndex;
        private string lobbyJoinIp = "", lobbyPort = "7788";
        private readonly Color ink = new Color(0.025f, 0.045f, 0.095f, 0.94f);
        private readonly Color accent = new Color(0.12f, 0.72f, 1.0f);
        private readonly Color violet = new Color(0.72f, 0.36f, 1.0f);
        private readonly Color gold = new Color(1.0f, 0.86f, 0.24f);

        private void InitUI()
        {
            if (textStyle != null) return;
            textStyle = new GUIStyle(GUI.skin.label) { fontSize = 16, wordWrap = true };
            textStyle.normal.textColor = UITheme.TextWhite;

            smallStyle = new GUIStyle(textStyle) { fontSize = 13 };
            smallStyle.normal.textColor = UITheme.TextMuted;

            tinyStyle = new GUIStyle(textStyle) { fontSize = 11 };
            tinyStyle.normal.textColor = UITheme.TextDim;

            headingStyle = new GUIStyle(textStyle) { fontSize = 24, fontStyle = FontStyle.Bold };
            headingStyle.normal.textColor = UITheme.TextWhite;

            titleStyle = new GUIStyle(headingStyle) { fontSize = 42, fontStyle = FontStyle.Bold };
            titleStyle.normal.textColor = UITheme.TextWhite;

            logoStyle = new GUIStyle(titleStyle) { fontSize = 96, alignment = TextAnchor.MiddleCenter, wordWrap = false };
            logoStyle.normal.textColor = UITheme.TextWhite;

            fieldStyle = new GUIStyle(GUI.skin.textField)
            {
                fontSize = 18,
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(14, 14, 8, 8)
            };
            fieldStyle.normal.textColor = UITheme.TextWhite;

            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                wordWrap = true,
                padding = new RectOffset(12, 12, 8, 8)
            };
            buttonStyle.normal.background = buttonStyle.hover.background = buttonStyle.active.background = Texture2D.whiteTexture;
            buttonStyle.normal.textColor = buttonStyle.hover.textColor = buttonStyle.active.textColor = Color.white;

            tinyButton = new GUIStyle(buttonStyle) { fontSize = 12, padding = new RectOffset(6, 6, 4, 4) };

            InitExtendedStyles();
        }

        private void OnGUI()
        {
            InitUI();
            uiScale = Mathf.Min(Screen.width / 1280f, Screen.height / 720f);
            uiOffset = new Vector2((Screen.width - 1280 * uiScale) / 2, (Screen.height - 720 * uiScale) / 2);
            Matrix4x4 previous = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(uiOffset, Quaternion.identity, Vector3.one * uiScale);

            if (ScreenState == CampaignScreen.Intro) DrawOpening();
            else if (ScreenState == CampaignScreen.Lobby) DrawLobby();
            else if (ScreenState == CampaignScreen.Menu) DrawMenu();
            else if (ScreenState == CampaignScreen.Countdown) DrawCountdown();
            else if (ScreenState == CampaignScreen.RoundResult || ScreenState == CampaignScreen.MatchResult) DrawRoundResults();
            else if (ScreenState == CampaignScreen.Result) DrawResult();
            else if (Run != null)
            {
                GUI.enabled = !paused;
                DrawTopBar();
                if (ScreenState == CampaignScreen.Battle) DrawBattle();
                else if (ScreenState == CampaignScreen.StageClear) DrawStageClear();
                else
                {
                    DrawWorldLabels();
                    DrawPartySidebar();
                    if (Role == PlayerRole.Corruptor) DrawCorruptionPanel();
                    DrawCapturePanel();
                    if (ScreenState == CampaignScreen.ChoosePartner) DrawPartnerSelection();
                }
                GUI.enabled = true;
                if (paused) DrawPause();
            }
            GUI.matrix = previous;
        }

        private void Panel(Rect r, Color c)
        {
            Color border = new Color(Mathf.Min(1f, c.r * 1.5f), Mathf.Min(1f, c.g * 1.5f), Mathf.Min(1f, c.b * 1.5f), Mathf.Min(0.55f, c.a * 0.8f));
            DrawGlassPanel(r, c, border, 6);
        }

        private void Text(float x, float y, float w, float h, string value, GUIStyle style = null) => GUI.Label(new Rect(x, y, w, h), value, style ?? textStyle);

        private bool Button(Rect r, string label, bool enabled = true, bool chosen = false, bool small = false)
        {
            return ModernButton(r, label, enabled, chosen, small);
        }

        private void Bar(float x, float y, float w, float fraction, Color color)
        {
            DrawModernBar(new Rect(x, y, w, 8), fraction, color, new Color(0.08f, 0.12f, 0.20f, 0.85f));
        }

        private void Logo(float y, float size = 96)
        {
            float breathe = 1f + 0.014f * Mathf.Sin(Time.unscaledTime * 1.8f);
            logoStyle.fontSize = Mathf.RoundToInt(size * breathe);

            // Subtle cyan aura halo behind title
            Color old = GUI.contentColor;
            float glow = 0.35f + 0.20f * (Mathf.Sin(Time.unscaledTime * 2.4f) * 0.5f + 0.5f);
            GUI.contentColor = new Color(0.08f, 0.75f, 1f, glow);
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI / 4f;
                GUI.Label(new Rect(Mathf.Cos(a) * 4, y + Mathf.Sin(a) * 4, 1280, 125), "VALMON", logoStyle);
            }

            // Shadow
            GUI.contentColor = new Color(0.01f, 0.03f, 0.08f, 0.88f);
            GUI.Label(new Rect(0, y + 4, 1280, 125), "VALMON", logoStyle);

            // Front text
            GUI.contentColor = UITheme.TextWhite;
            GUI.Label(new Rect(0, y, 1280, 125), "VALMON", logoStyle);
            GUI.contentColor = old;
        }

        private static string LoadingDots()
        {
            int d = ((int)(Time.unscaledTime * 2f)) % 4;
            return new string('.', d);
        }

        private void MenuChrome()
        {
            DrawGlassPanel(new Rect(0, 0, 1280, 52), UITheme.GlassHeader, UITheme.BorderMuted, 0);
            GUI.DrawTexture(new Rect(0, 52, 1280, 2), UITextures.Solid(UITheme.CyanPrimary));

            DrawBadge(new Rect(28, 14, 86, 24), "VALMON", UITheme.CyanPrimary, Color.white, 11);
            Text(124, 17, 500, 24, "THE RIFT CHAMPIONSHIP", smallStyle);

            GUIStyle rightInfo = new GUIStyle(smallStyle) { alignment = TextAnchor.MiddleRight };
            rightInfo.normal.textColor = UITheme.TextDim;
            Text(920, 17, 330, 24, "22 CREATURES  •  4 CHARACTERS  •  LAN LINK", rightInfo);
        }

        private void DrawOpening()
        {
            if (video != null && video.isPrepared)
            {
                GUI.DrawTexture(new Rect(0, 0, 1280, 720), videoTexture, ScaleMode.ScaleToFit);
                Text(900, 675, 340, 30, "CLICK / ANY KEY / CONTROLLER TO SKIP", smallStyle);
                return;
            }
            float t = videoWait;
            MenuChrome();
            if (t > 1) Logo(118, Mathf.Lerp(76, 96, Mathf.Clamp01((t - 1) / 3)));
            if (t > 4) Text(385, 251, 530, 55, "Catch the creature. Resist the corruption.\nMaster the rift.", textStyle);
            if (t > 7)
            {
                DrawGlassPanel(new Rect(410, 350, 460, 44), UITheme.GlassCard, UITheme.CyanPrimary, 8);
                Text(420, 362, 440, 30, "TWO RIVALS • THREE HABITATS • ONE CHAMPION", new GUIStyle(smallStyle) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold });
            }
            Text(900, 675, 340, 30, "CLICK / ANY KEY / CONTROLLER TO SKIP", smallStyle);
            float darkness = 1 - Mathf.Clamp01(t / 1.1f);
            if (darkness > 0) GUI.DrawTexture(new Rect(0, 0, 1280, 720), UITextures.Solid(new Color(0, 0, 0, darkness)));
        }

        private void DrawMenu()
        {
            MenuChrome();
            if (collection) { DrawCollection(); return; }
            if (settings) { DrawSettings(); return; }
            if (helpPage != "") { DrawInfoPage(); return; }
            if (menuStep == 0) { DrawHomeMenu(); return; }

            Rect modalRect = new Rect(250, 95, 780, 580);
            DrawGlassCard(modalRect, UITheme.GlassDark, UITheme.BorderMuted, UITheme.CyanPrimary, 10);

            if (Button(new Rect(275, 115, 95, 34), "◀ BACK", true, false, true)) menuStep--;

            if (menuStep == 1)
            {
                Text(390, 115, 600, 40, "01 / SELECT YOUR HABITAT", headingStyle);
                for (int i = 0; i < 3; i++)
                {
                    float y = 175 + i * 118;
                    Rect hCard = new Rect(275, y, 730, 100);
                    Color theme = World.Theme(i);
                    DrawGlassCard(hCard, UITheme.GlassCard, i == selectedLevel ? UITheme.CyanPrimary : UITheme.BorderMuted, theme, 8);

                    DrawBadge(new Rect(295, y + 16, 90, 24), i == 0 ? "GRASS" : (i == 1 ? "FIRE" : "WATER"), theme, Color.white, 11);
                    Text(395, y + 14, 400, 32, mapNames[i], headingStyle);
                    Text(295, y + 54, 420, 24, i == 0 ? "Levels 1–15  •  10 Poké Balls  •  8 Minutes" : i == 1 ? "Levels 16–30  •  Ember Ruin Environment" : "Levels 31–50  •  Tidal Sanctuary", smallStyle);

                    bool isUnlocked = i <= unlocked;
                    if (ModernButton(new Rect(775, y + 26, 210, 48), isUnlocked ? (i == selectedLevel ? "SELECTED" : "SELECT") : "LOCKED", isUnlocked, i == selectedLevel, false, theme))
                    {
                        selectedLevel = i;
                        menuStep = 2;
                    }
                }
<<<<<<< HEAD
                Text(310,578,640,55,"10 Valmon Balls. 8 minutes. Catch 5 to advance. Your surviving team carries forward.");
=======
                Text(275, 545, 730, 30, "10 Poké Balls • 8 minutes • Catch 5 to advance • Surviving team carries forward", new GUIStyle(smallStyle) { alignment = TextAnchor.MiddleCenter });
>>>>>>> ui-redesign
            }
            else if (menuStep == 2)
            {
                Text(390, 115, 600, 40, "02 / CHOOSE YOUR CHARACTER", headingStyle);
                for (int i = 0; i < 4; i++)
                {
                    float bx = 285 + (i % 2) * 365;
                    float by = 185 + (i / 2) * 130;
                    Rect cCard = new Rect(bx, by, 345, 115);
                    bool isSelected = selectedCharacter == i;
                    DrawGlassCard(cCard, UITheme.GlassCard, isSelected ? UITheme.CyanPrimary : UITheme.BorderMuted, isSelected ? UITheme.CyanPrimary : UITheme.BorderLight, 8);

                    Text(bx + 20, by + 18, 305, 30, catalog.characters[i].name.ToUpperInvariant(), headingStyle);
                    Text(bx + 20, by + 50, 305, 24, "Combatant Profile #" + (i + 1), smallStyle);

                    if (ModernButton(new Rect(bx + 20, by + 76, 305, 30), isSelected ? "SELECTED" : "CHOOSE", true, isSelected, true))
                        selectedCharacter = i;
                }
                Text(285, 470, 710, 30, "Your character represents you in the arena. Choose role in next step.", new GUIStyle(smallStyle) { alignment = TextAnchor.MiddleCenter });
                if (ModernButton(new Rect(285, 520, 710, 52), "CONTINUE TO ROLE & STARTER ▶", true, true)) menuStep = 3;
            }
            else
            {
<<<<<<< HEAD
                Text(410,132,590,45,"03 / ROLE & STARTER",headingStyle);
                if(Button(new Rect(310,215,315,70),"TAMER",true,Role==PlayerRole.Tamer)) Role=PlayerRole.Tamer;
                if(Button(new Rect(650,215,315,70),"CORRUPTOR",true,Role==PlayerRole.Corruptor)) Role=PlayerRole.Corruptor;
                Text(312,306,650,65,Role==PlayerRole.Tamer?"Explore and capture creatures. Defeat corrupted Valmons in turn battles.":"Control corruption and the wild Valmons' moves. Eliminate the AI Tamer's team.");
                Text(312,396,650,30,"TAMER'S STARTER",headingStyle);
                for(int i=0;i<3;i++) if(Button(new Rect(310+i*223,443,210,68),catalog.Find(starterIds[i]).displayName,true,selectedStarter==i)) selectedStarter=i;
                if(Button(new Rect(310,581,650,62),"ENTER THE HABITAT",true,true)) StartCampaign(selectedLevel,selectedCharacter,Role,starterIds[selectedStarter]);
=======
                Text(390, 115, 600, 40, "03 / ROLE & STARTER", headingStyle);

                // Role selection pills
                Rect tamerBtn = new Rect(285, 175, 345, 68);
                Rect wardenBtn = new Rect(650, 175, 345, 68);

                bool isTamer = Role == PlayerRole.Tamer;
                if (ModernButton(tamerBtn, isTamer ? "▶ TAMER (SELECTED)" : "TAMER", true, isTamer, false, UITheme.CyanPrimary)) Role = PlayerRole.Tamer;
                if (ModernButton(wardenBtn, !isTamer ? "▶ WARDEN (SELECTED)" : "WARDEN", true, !isTamer, false, UITheme.CrimsonWarden)) Role = PlayerRole.Corruptor;

                string roleDesc = Role == PlayerRole.Tamer
                    ? "TAMER: Explore the arena, capture wild creatures with Poké Balls, and defeat corrupted Pokémon in turn battles."
                    : "WARDEN: Control corruption, lock gates, set traps, and battle the AI Tamer to protect the rift.";
                Text(285, 260, 710, 40, roleDesc, smallStyle);

                Text(285, 315, 710, 30, "TAMER'S STARTER COMPANION", headingStyle);
                for (int i = 0; i < 3; i++)
                {
                    float sx = 285 + i * 242;
                    Rect sCard = new Rect(sx, 355, 226, 95);
                    bool isStarter = selectedStarter == i;
                    SpeciesDefinition sp = catalog.Find(starterIds[i]);
                    Color sCol = i == 0 ? UITheme.ArenaGrass : (i == 1 ? UITheme.ArenaFire : UITheme.ArenaWater);

                    DrawGlassCard(sCard, UITheme.GlassCard, isStarter ? sCol : UITheme.BorderMuted, sCol, 8);
                    DrawBadge(new Rect(sx + 12, 365, 75, 20), sp.types[0], sCol, Color.white, 10);
                    Text(sx + 12, 390, 200, 28, sp.displayName, headingStyle);

                    if (ModernButton(new Rect(sx + 12, 420, 202, 24), isStarter ? "SELECTED" : "PICK", true, isStarter, true, sCol))
                        selectedStarter = i;
                }

                if (ModernButton(new Rect(285, 520, 710, 52), "ENTER THE HABITAT ▶", true, true))
                    StartCampaign(selectedLevel, selectedCharacter, Role, starterIds[selectedStarter]);
>>>>>>> ui-redesign
            }
        }

        private void DrawCollection()
        {
            MenuChrome();
            Text(48, 80, 620, 50, "CREATURE ROSTER", titleStyle);
            SpeciesDefinition species = catalog.species[collectionIndex];

            // Left inspector card
            Rect inspectCard = new Rect(48, 140, 450, 520);
            DrawGlassCard(inspectCard, UITheme.GlassDark, UITheme.CyanPrimary, UITheme.CyanPrimary, 10);

            Text(70, 165, 400, 40, species.displayName.ToUpperInvariant(), titleStyle);

            for (int t = 0; t < species.types.Length; t++)
            {
                Color tCol = UITheme.GetTypeColor(species.types[t]);
                DrawBadge(new Rect(70 + t * 95, 215, 85, 24), species.types[t], tCol, Color.white, 11);
            }

            // Stats breakdown
            float sy = 265;
            string[] statNames = { "HP", "ATTACK", "DEFENSE", "SP. ATK", "SP. DEF", "SPEED" };
            int[] statVals = { species.hp, species.attack, species.defense, species.specialAttack, species.specialDefense, species.speed };
            for (int s = 0; s < 6; s++)
            {
                Text(70, sy + s * 34, 120, 24, statNames[s], smallStyle);
                DrawModernBar(new Rect(190, sy + s * 34 + 6, 180, 12), statVals[s] / 150f, UITheme.CyanPrimary, new Color(0.06f, 0.10f, 0.18f, 0.8f));
                Text(380, sy + s * 34, 50, 24, statVals[s].ToString(), smallStyle);
            }

            // Moves
            Text(70, 485, 400, 26, "SIGNATURE MOVES", headingStyle);
            string movesList = string.Join("  •  ", System.Array.ConvertAll(species.moves, m => m.name));
            Text(70, 515, 400, 45, movesList, smallStyle);

            if (ModernButton(new Rect(70, 580, 406, 44), "BACK TO TITLE", true, false, true))
            {
                collection = false;
                World.ShowMenu();
            }

            // Right species grid
            Rect gridCard = new Rect(530, 140, 700, 520);
            DrawGlassCard(gridCard, UITheme.GlassDark, UITheme.BorderMuted, UITheme.BorderLight, 10);
            Text(555, 155, 650, 30, "ALL HABITAT SPECIES (" + catalog.species.Length + ")", headingStyle);

            for (int i = 0; i < catalog.species.Length; i++)
            {
                float bx = 555 + (i % 3) * 215;
                float by = 195 + (i / 3) * 58;
                bool isCur = i == collectionIndex;
                if (ModernButton(new Rect(bx, by, 205, 48), catalog.species[i].displayName, true, isCur, true))
                {
                    collectionIndex = i;
                    World.ShowCollection(i);
                }
            }
        }

        private void DrawLobby() { DrawMatchLobby(); }

        private bool ReadPort()
        {
            if (int.TryParse(lobbyPort, out int port) && port >= 1024 && port <= 65535)
            {
                netPort = port;
                return true;
            }
            netStatus = "Use a UDP port from 1024 to 65535.";
            return false;
        }

        private void DrawTopBar()
        {
            Rect barRect = new Rect(20, 10, 1240, 38);
            DrawGlassPanel(barRect, UITheme.GlassHeader, UITheme.BorderMuted, 6);

            Color stageColor = UITheme.GetArenaColor(Run.stage);
            Rect stripe = new Rect(20, 10, 1240, 2);
            GUI.DrawTexture(stripe, UITextures.Solid(stageColor));

            // Left Stage Pill: "MAP 1 / Verdant Ruins (TAMER SOLO)"
            string roleLabel = RoleName(Role).ToUpperInvariant();
            string modeTag = netMode == NetMode.None ? $" ({roleLabel} SOLO)" : $" ({roleLabel})";
            string mapTitle = finalBattle ? "FINAL DUEL" : $"MAP {Run.stage + 1} / {mapNames[Run.stage]}{modeTag}";
            DrawBadge(new Rect(32, 16, 260, 26), mapTitle, stageColor, Color.white, 11);

            // Centered match metrics cluster
            if (Role == PlayerRole.Tamer)
            {
                DrawBadge(new Rect(440, 16, 150, 26), $"CATCHES  {Run.catches} / {CampaignRun.RequiredCatches}", new Color(0.08f, 0.14f, 0.24f, 0.95f), UITheme.CyanPrimary, 11);
                DrawBadge(new Rect(605, 16, 140, 26), $"BALLS  {Run.balls} / {CampaignRun.BallsPerStage}", new Color(0.08f, 0.14f, 0.24f, 0.95f), UITheme.TextWhite, 11);
            }
            else
            {
                DrawBadge(new Rect(440, 16, 155, 26), $"CORRUPTIONS  {CorruptionCount} / {CampaignRun.RequiredCorruptions}", new Color(0.18f, 0.08f, 0.14f, 0.95f), UITheme.CrimsonWarden, 11);
                DrawBadge(new Rect(610, 16, 140, 26), $"ENERGY  {Mathf.FloorToInt(corruptionEnergy)} / 100", new Color(0.18f, 0.08f, 0.14f, 0.95f), UITheme.TextWhite, 11);
            }

            // Digital timer with pulse when urgent
            int secs = Mathf.Max(0, Mathf.CeilToInt(Run.seconds));
            string timeStr = finalBattle ? "DUEL" : $"{secs / 60:00}:{secs % 60:00}";
            bool isUrgent = secs < 60 && !finalBattle;
            Color timerCol = isUrgent ? (Mathf.Sin(Time.unscaledTime * 6f) > 0 ? Color.white : UITheme.CrimsonWarden) : UITheme.CyanGlow;

            Rect timerRect = new Rect(765, 16, 110, 26);
            DrawGlassPanel(timerRect, isUrgent ? new Color(0.35f, 0.05f, 0.08f, 0.95f) : new Color(0.06f, 0.10f, 0.18f, 0.95f), isUrgent ? UITheme.CrimsonWarden : UITheme.BorderCyan, 4);
            GUIStyle timerStyle = new GUIStyle(headingStyle) { fontSize = 14, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            if (fontInterBold != null) timerStyle.font = fontInterBold;
            timerStyle.normal.textColor = timerCol;
            GUI.Label(timerRect, timeStr, timerStyle);

            // Right Pause button (clean matching mockup)
            if (ModernButton(new Rect(1140, 15, 105, 28), "||  PAUSE", true, false, true)) TogglePause();
        }

        private void DrawPartySidebar() { DrawRoleStatus(); }

        private void DrawWorldLabels()
        {
            if (view == null || World?.wild == null) return;

            bool nearWild = ScreenState == CampaignScreen.Explore && selected != null && !selected.captured;
            float selDist = (nearWild && World != null && World.tamer != null && selected.root != null)
                ? Vector3.Distance(World.tamer.transform.position, selected.root.transform.position)
                : 999f;
            bool canCatch = nearWild && selDist <= 4.4f;

            foreach (WildCreature w in World.wild)
            {
                if (w == null || w.captured || w.root == null || !w.root.activeInHierarchy) continue;
                float dist = Vector3.Distance(view.transform.position, w.root.transform.position);
                if (dist > 35f) continue;
                Vector3 p = view.WorldToScreenPoint(w.root.transform.position + Vector3.up * (w.visualHeight + 0.35f));
                if (p.z <= 0) continue;
                float x = (p.x - uiOffset.x) / uiScale - 55, y = (Screen.height - p.y - uiOffset.y) / uiScale;
                if (x < 150 || x > 1100 || y < 45 || y > 660) continue;

                bool isTarget = (w == selected);

                if (isTarget && canCatch)
                {
                    // Floating in-world prompt badge: [ Z ] Catch (Matching reference mockup above creature!)
                    Rect promptRect = new Rect(x - 10, y - 12, 125, 28);
                    DrawGlassPanel(promptRect, new Color(0.02f, 0.04f, 0.08f, 0.95f), UITheme.CyanPrimary, 6);

                    // Keycap "Z"
                    DrawKeycap(new Rect(x - 5, y - 8, 20, 20), "Z", UITheme.CyanPrimary);

                    // Action label
                    GUIStyle actStyle = new GUIStyle(headingStyle) { fontSize = 12, fontStyle = FontStyle.Bold };
                    if (fontInterBold != null) actStyle.font = fontInterBold;
                    actStyle.normal.textColor = Color.white;
                    GUI.Label(new Rect(x + 22, y - 8, 90, 20), Role == PlayerRole.Tamer ? "Catch" : "Corrupt", actStyle);
                }
                else
                {
                    // Sleek compact creature chip: "Squirtle Lv.12"
                    Rect labelRect = new Rect(x, y, 115, 22);
                    Color tagCol = w.corrupted ? UITheme.CrimsonWarden : UITheme.ArenaGrass;
                    DrawGlassPanel(labelRect, new Color(0.02f, 0.04f, 0.08f, 0.88f), tagCol, 4);

                    string tagText = $"{w.creature.species.displayName} Lv.{w.creature.level}" + (w.corrupted ? " [C]" : "");
                    GUIStyle nameStyle = new GUIStyle(smallStyle) { fontSize = 10, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
                    if (fontInterBold != null) nameStyle.font = fontInterBold;
                    nameStyle.normal.textColor = w.corrupted ? UITheme.CoralWarden : Color.white;
                    GUI.Label(labelRect, tagText, nameStyle);
                }
            }
        }

        private void DrawCorruptionPanel() { DrawWardenAbilities(); }

        private void DrawCapturePanel()
        {
            if (ScreenState == CampaignScreen.Capture)
            {
                Rect capRect = new Rect(340, 560, 600, 80);
                DrawGlassCard(capRect, UITheme.GlassDark, UITheme.CyanPrimary, UITheme.CyanPrimary, 8);
                Text(360, 572, 560, 30, "CAPTURE IN PROGRESS...", headingStyle);
                DrawModernBar(new Rect(360, 612, 560, 14), captureClock / 2.7f, encounter != null && encounter.corrupted ? UITheme.VioletCorruption : UITheme.CyanPrimary, new Color(0.06f, 0.10f, 0.18f, 0.85f));
            }
            DrawFooter();
        }

        private void DrawFooter()
        {
            // Contextual hint banner in bottom-center (clean and uncluttered, leaving room for circular controls)
            bool nearWild = ScreenState == CampaignScreen.Explore && selected != null && !selected.captured;
            float distance = (nearWild && World != null && World.tamer != null && selected.root != null)
                ? Vector3.Distance(World.tamer.transform.position, selected.root.transform.position)
                : 999f;

            if (nearWild && distance <= 18f && distance > 4.4f)
            {
                Rect hintRect = new Rect(420, 678, 440, 26);
                DrawGlassPanel(hintRect, new Color(0.02f, 0.04f, 0.08f, 0.85f), UITheme.BorderMuted, 4);
                string hint = $"Target: {selected.creature.species.displayName} ({distance:0.0}m)  •  Approach within 4.0m to catch";
                GUIStyle hStyle = new GUIStyle(smallStyle) { fontSize = 11, alignment = TextAnchor.MiddleCenter };
                if (fontInterRegular != null) hStyle.font = fontInterRegular;
                hStyle.normal.textColor = UITheme.TextMuted;
                GUI.Label(hintRect, hint, hStyle);
            }
        }

        private void DrawPartnerSelection()
        {
            Rect pRect = new Rect(280, 180, 680, 360);
            DrawGlassCard(pRect, UITheme.GlassDark, UITheme.CyanPrimary, UITheme.CyanPrimary, 10);
            Text(305, 200, 630, 40, Role == PlayerRole.Tamer ? "CHOOSE YOUR ACTIVE BATTLER" : "WAITING FOR TAMER...", headingStyle);

            for (int i = 0; i < Run.party.Count; i++)
            {
                OwnedCreature c = Run.party[i];
                float bx = 305 + (i % 2) * 320;
                float by = 260 + (i / 2) * 54;
                string label = $"{c.species.displayName} Lv.{c.level} • {c.health} HP";
                if (ModernButton(new Rect(bx, by, 305, 46), label, Role == PlayerRole.Tamer, i == partyIndex, true))
                    ChoosePartner(i);
            }
        }

        private void HealthCard(float x, OwnedCreature c, string side, Color color)
        {
            Rect r = new Rect(x, 100, 400, 110);
            DrawGlassCard(r, UITheme.GlassCard, UITheme.BorderMuted, color, 8);

            DrawBadge(new Rect(x + 16, 112, 110, 22), side, color, Color.white, 10);
            Text(x + 135, 110, 245, 26, c.species.displayName + " Lv." + c.level, new GUIStyle(headingStyle) { fontSize = 18 });

            string typeInfo = string.Join(" / ", c.species.types) + (string.IsNullOrEmpty(c.status) ? "" : " • " + c.status.ToUpperInvariant());
            Text(x + 16, 142, 368, 20, typeInfo, new GUIStyle(smallStyle) { fontSize = 11, normal = { textColor = UITheme.TextMuted } });

            float hpFrac = (float)c.health / c.MaxHealth;
            Color hpCol = hpFrac > 0.5f ? UITheme.ArenaGrass : (hpFrac > 0.2f ? UITheme.AmberWarning : UITheme.CrimsonWarden);
            DrawModernBar(new Rect(x + 16, 170, 290, 14), hpFrac, hpCol, new Color(0.08f, 0.12f, 0.20f, 0.9f));

            Text(x + 312, 168, 72, 18, $"{c.health}/{c.MaxHealth}", new GUIStyle(smallStyle) { fontSize = 12, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleRight });
        }

        private void DrawBattle()
        {
            if (Run.party.Count == 0 || opponent == null) return;
            HealthCard(30, Run.party[partyIndex], "TAMER", UITheme.CyanPrimary);
            HealthCard(850, opponent, finalBattle ? "CORRUPTOR / " + bossTeam.Count + " LEFT" : "CORRUPTED WILD", UITheme.VioletCorruption);

            Color barAccent = HumanTurn ? UITheme.CyanPrimary : UITheme.VioletCorruption;

            // Compact left card: Turn status, battle message, and switch buttons
            Rect leftCard = new Rect(30, 595, 480, 110);
            DrawGlassCard(leftCard, UITheme.GlassCard, UITheme.BorderMuted, barAccent, 8);

            string turnText = turnDelay > 0 ? "RESOLVING TURN..." : HumanTurn ? "YOUR TURN" : "OPPONENT'S TURN";
            DrawBadge(new Rect(45, 605, 130, 22), turnText, barAccent, Color.white, 10);
            Text(185, 606, 310, 22, message, tinyStyle);

            var team = Role == PlayerRole.Tamer ? Run.party : bossTeam;
            if (Role == PlayerRole.Tamer || finalBattle)
            {
                Text(45, 638, 70, 24, "SWITCH:", tinyStyle);
                for (int i = 0; i < team.Count; i++)
                {
                    bool isCur = Role == PlayerRole.Tamer ? i == partyIndex : team[i] == opponent;
                    if (ModernButton(new Rect(115 + i * 86, 634, 80, 28), team[i].species.displayName, HumanTurn && turnDelay <= 0, isCur, true))
                        SwitchParty(i);
                }
            }

            // Compact right card: 2x2 Moves panel
            OwnedCreature actor = Role == PlayerRole.Tamer ? Run.party[partyIndex] : opponent;
            Rect rightCard = new Rect(530, 595, 720, 110);
            DrawGlassCard(rightCard, UITheme.GlassCard, UITheme.BorderMuted, barAccent, 8);

            if (BattleRules.HasMoves(actor))
            {
                for (int i = 0; i < actor.species.moves.Length; i++)
                {
                    MoveDefinition m = actor.species.moves[i];
                    float bx = 545 + (i % 2) * 350;
                    float by = 605 + (i / 2) * 48;
                    Color mTypeCol = UITheme.GetTypeColor(m.type);

                    Rect mRect = new Rect(bx, by, 340, 42);
                    bool canUse = HumanTurn && turnDelay <= 0 && actor.pp[i] > 0;
                    string moveDesc = $"{m.name.ToUpperInvariant()} ({m.type}) • {(m.power > 0 ? "Pwr " + m.power : m.effect.ToUpperInvariant())} • PP {actor.pp[i]}/{m.pp}";
                    if (ModernButton(mRect, moveDesc, canUse, false, true, mTypeCol))
                        PlayMove(i);
                }
            }
            else if (ModernButton(new Rect(545, 620, 690, 54), "STRUGGLE / RECOIL", HumanTurn && turnDelay <= 0))
            {
                PlayMove(-1);
            }
        }

        private void DrawStageClear()
        {
            Rect clearRect = new Rect(220, 110, 840, 510);
            DrawGlassCard(clearRect, UITheme.GlassDark, UITheme.CyanPrimary, UITheme.CyanPrimary, 10);

            Text(255, 135, 780, 60, "HABITAT CLEARED", titleStyle);
            Text(255, 215, 780, 50, "3 new Valmons secured in " + mapNames[Run.stage] + ".  •  " + Run.party.Count + " teammates survive.", headingStyle);
            Text(255, 280, 780, 40, Run.stage < 2 ? "Next habitat: team healed, ten balls and eight minutes." : "Final duel: defeat three random Corruptor Valmons.", smallStyle);

            bool canSwap = netMode == NetMode.None && catalog != null && catalog.characters != null && catalog.characters.Length > 0;
            if (canSwap)
            {
                Text(255, 340, 780, 26, "SELECT YOUR CHARACTER FOR NEXT HABITAT", smallStyle);
                int count = Mathf.Min(4, catalog.characters.Length);
                for (int i = 0; i < count; i++)
                    if (ModernButton(new Rect(255 + (i % 2) * 395, 375 + (i / 2) * 52, 375, 44), catalog.characters[i].name, true, selectedCharacter == i, true))
                        selectedCharacter = i;
            }

            if (ModernButton(new Rect(255, 520, 770, 58), netMode == NetMode.Client ? "WAITING FOR HOST..." : Run.stage < 2 ? "ADVANCE TO NEXT HABITAT ▶" : "FACE THE CORRUPTOR ▶", netMode != NetMode.Client, true))
                Advance();
        }

        private void DrawResult()
        {
            GUI.DrawTexture(new Rect(0, 0, 1280, 720), UITextures.Solid(new Color(0.015f, 0.025f, 0.06f, 0.96f)));
            Logo(35, 68);

            int habitatsCleared = outcomeTamer ? 3 : Mathf.Clamp(Run != null ? Run.stage : 0, 0, 3);
            int catches = Run != null ? Run.catches : 0;
            int balls = Run != null ? Run.balls : 0;
            int teamSize = Run != null ? Run.party.Count : 0;
            float secondsLeft = Run != null ? Mathf.Max(0, Run.seconds) : 0;
            int timeBonus = Mathf.RoundToInt(secondsLeft * 2);
            int catchPts = catches * 100;
            int ballPts = balls * 25;
            int habitatPts = habitatsCleared * 500;
            int teamPts = teamSize * 150;
            int winBonus = outcomeTamer ? 1000 : 0;
            int totalPts = catchPts + ballPts + timeBonus + habitatPts + teamPts + winBonus;

            Rect resRect = new Rect(140, 140, 1000, 475);
            DrawGlassCard(resRect, UITheme.GlassDark, UITheme.BorderMuted, outcomeTamer ? UITheme.GoldChampion : UITheme.VioletCorruption, 10);

            Text(170, 160, 940, 60, outcomeTamer ? "VICTORY • CHAMPION OF THE RIFT" : "MATCH FINISHED", titleStyle);
            Text(170, 222, 940, 30, resultReason, smallStyle);

            // Left stats
            float sx = 180, sy = 280, lineH = 34;
            Text(sx, sy + 0 * lineH, 240, 30, "Habitats cleared", smallStyle);
            Text(sx + 250, sy + 0 * lineH, 100, 30, habitatsCleared + " / 3", headingStyle);
            Text(sx, sy + 1 * lineH, 240, 30, "Creatures secured", smallStyle);
            Text(sx + 250, sy + 1 * lineH, 100, 30, catches.ToString(), headingStyle);
            Text(sx, sy + 2 * lineH, 240, 30, "Balls remaining", smallStyle);
            Text(sx + 250, sy + 2 * lineH, 100, 30, balls + " / 10", headingStyle);
            Text(sx, sy + 3 * lineH, 240, 30, "Surviving team", smallStyle);
            Text(sx + 250, sy + 3 * lineH, 100, 30, teamSize.ToString(), headingStyle);
            Text(sx, sy + 4 * lineH, 240, 30, "Time remaining", smallStyle);
            Text(sx + 250, sy + 4 * lineH, 160, 30, Mathf.FloorToInt(secondsLeft / 60f) + ":" + (Mathf.FloorToInt(secondsLeft) % 60).ToString("D2"), headingStyle);

            // Right score breakdown
            float bx = 630, by = 280;
            Text(bx, by - 26, 400, 30, "SCORE BREAKDOWN", headingStyle);
            Text(bx, by + 0 * lineH, 300, 30, "Catches x100", smallStyle);
            Text(bx + 300, by + 0 * lineH, 160, 30, "+ " + catchPts, textStyle);
            Text(bx, by + 1 * lineH, 300, 30, "Balls x25", smallStyle);
            Text(bx + 300, by + 1 * lineH, 160, 30, "+ " + ballPts, textStyle);
            Text(bx, by + 2 * lineH, 300, 30, "Time bonus (x2/s)", smallStyle);
            Text(bx + 300, by + 2 * lineH, 160, 30, "+ " + timeBonus, textStyle);
            Text(bx, by + 3 * lineH, 300, 30, "Habitats x500", smallStyle);
            Text(bx + 300, by + 3 * lineH, 160, 30, "+ " + habitatPts, textStyle);
            Text(bx, by + 4 * lineH, 300, 30, "Team bonus x150", smallStyle);
            Text(bx + 300, by + 4 * lineH, 160, 30, "+ " + teamPts, textStyle);
            if (outcomeTamer)
            {
                Text(bx, by + 5 * lineH, 300, 30, "Victory bonus", smallStyle);
                Text(bx + 300, by + 5 * lineH, 160, 30, "+ " + winBonus, textStyle);
            }

            // Total Score bar
            GUI.DrawTexture(new Rect(170, 520, 940, 1), UITextures.Solid(UITheme.BorderMuted));
            Text(180, 532, 400, 45, "TOTAL CHAMPIONSHIP SCORE", headingStyle);
            GUIStyle totalStyle = new GUIStyle(titleStyle)
            {
                alignment = TextAnchor.MiddleRight,
                fontSize = 32
            };
            totalStyle.normal.textColor = outcomeTamer ? UITheme.GoldChampion : UITheme.CyanPrimary;
            Text(600, 530, 510, 45, totalPts.ToString("N0") + " PTS", totalStyle);

            if (ModernButton(new Rect(140, 630, 480, 55), "RETURN TO TITLE", true, true)) BackToTitle();
            if (ModernButton(new Rect(660, 630, 480, 55), "EXIT GAME")) Quit();
        }

        private void DrawPause()
        {
            GUI.DrawTexture(new Rect(0, 0, 1280, 720), UITextures.Solid(new Color(0, 0, 0, 0.70f)));
            Rect pCard = new Rect(400, 180, 480, 350);
            DrawGlassCard(pCard, UITheme.GlassDark, UITheme.CyanPrimary, UITheme.CyanPrimary, 10);

            Text(435, 210, 410, 45, "MATCH PAUSED", headingStyle);
            Text(435, 265, 410, 40, netMode == NetMode.None ? "Game loop and timer are paused." : "Both laptops share this pause.", smallStyle);

            if (ModernButton(new Rect(435, 330, 410, 52), "RESUME MATCH", true, true)) TogglePause();
            if (ModernButton(new Rect(435, 400, 410, 52), "LEAVE MATCH TO TITLE")) BackToTitle();
        }

        private void DrawSettings()
        {
            Rect sCard = new Rect(330, 175, 620, 420);
            DrawGlassCard(sCard, UITheme.GlassDark, UITheme.CyanPrimary, UITheme.CyanPrimary, 10);

            Text(365, 205, 550, 50, "SETTINGS", headingStyle);
            Text(365, 275, 550, 30, "MASTER AUDIO VOLUME • " + Mathf.RoundToInt(volume * 100) + "%", smallStyle);

            volume = GUI.HorizontalSlider(new Rect(365, 315, 545, 25), volume, 0, 1);
            AudioListener.volume = volume;

            Text(365, 365, 550, 65, "Level unlocks are persisted locally.\nMultiplayer LAN link defaults to UDP port 7788.", smallStyle);

            if (ModernButton(new Rect(365, 485, 545, 52), "SAVE & RETURN", true, true))
            {
                PlayerPrefs.SetFloat("VALMON.Volume", volume);
                PlayerPrefs.Save();
                settings = false;
            }
        }

        private void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
