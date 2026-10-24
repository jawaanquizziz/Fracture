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
        private readonly Color ink = new Color(.025f, .045f, .095f, .96f);
        private readonly Color accent = new Color(.16f, .65f, 1f);
        private readonly Color violet = new Color(.76f, .40f, 1f);
        private readonly Color gold = new Color(1f, .92f, .24f);
        private void InitUI()
        {
            if (textStyle != null) return;
            textStyle = new GUIStyle(GUI.skin.label) { fontSize = 18, wordWrap = true }; textStyle.normal.textColor = Color.white;
            smallStyle = new GUIStyle(textStyle) { fontSize = 14 };
            tinyStyle = new GUIStyle(textStyle) { fontSize = 12 };
            headingStyle = new GUIStyle(textStyle) { fontSize = 27, fontStyle = FontStyle.Bold };
            titleStyle = new GUIStyle(headingStyle) { fontSize = 48 };
            logoStyle = new GUIStyle(titleStyle) { fontSize = 104, alignment = TextAnchor.MiddleCenter, wordWrap = false };
            fieldStyle = new GUIStyle(GUI.skin.textField) { fontSize = 22, alignment = TextAnchor.MiddleLeft, padding = new RectOffset(12,12,6,6) };
            buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 17, fontStyle = FontStyle.Bold, wordWrap = true, padding = new RectOffset(10,10,6,6) };
            buttonStyle.normal.background = buttonStyle.hover.background = buttonStyle.active.background = Texture2D.whiteTexture;
            buttonStyle.normal.textColor = buttonStyle.hover.textColor = buttonStyle.active.textColor = Color.white;
            tinyButton = new GUIStyle(buttonStyle) { fontSize = 12, padding = new RectOffset(3,3,3,3) };
        }
        private void OnGUI()
        {
            InitUI(); uiScale = Mathf.Min(Screen.width / 1280f, Screen.height / 720f);
            uiOffset = new Vector2((Screen.width - 1280 * uiScale) / 2, (Screen.height - 720 * uiScale) / 2);
            Matrix4x4 previous = GUI.matrix; GUI.matrix = Matrix4x4.TRS(uiOffset, Quaternion.identity, Vector3.one * uiScale);
            if (ScreenState == CampaignScreen.Intro) DrawOpening();
            else if (ScreenState == CampaignScreen.Lobby) DrawLobby();
            else if (ScreenState == CampaignScreen.Menu) DrawMenu();
            else if(ScreenState==CampaignScreen.Countdown) DrawCountdown();
            else if(ScreenState==CampaignScreen.RoundResult||ScreenState==CampaignScreen.MatchResult) DrawRoundResults();
            else if (ScreenState == CampaignScreen.Result) DrawResult();
            else if (Run != null)
            {
                GUI.enabled = !paused; DrawTopBar();
                if (ScreenState == CampaignScreen.Battle) DrawBattle();
                else if (ScreenState == CampaignScreen.StageClear) DrawStageClear();
                else { DrawWorldLabels(); DrawPartySidebar(); if (Role == PlayerRole.Corruptor) DrawCorruptionPanel(); DrawCapturePanel(); if (ScreenState == CampaignScreen.ChoosePartner) DrawPartnerSelection(); }
                GUI.enabled = true; if (paused) DrawPause();
            }
            GUI.matrix = previous;
        }
        private void Panel(Rect r, Color c) { Color old = GUI.color; GUI.color = c; GUI.DrawTexture(r, Texture2D.whiteTexture); GUI.color = old; }
        private void Text(float x, float y, float w, float h, string value, GUIStyle style = null) => GUI.Label(new Rect(x,y,w,h), value, style ?? textStyle);
        private bool Button(Rect r, string label, bool enabled = true, bool chosen = false, bool small = false)
        {
            bool was = GUI.enabled; GUI.enabled = was && enabled; Color old = GUI.backgroundColor;
            bool over = r.Contains(Event.current.mousePosition);
            // Primary (chosen) buttons breathe for extra pop; hovered buttons brighten.
            if (chosen && enabled)
            {
                float pulse = .5f + .5f * Mathf.Sin(Time.unscaledTime * 3.4f);
                GUI.backgroundColor = Color.Lerp(new Color(.07f,.44f,.82f), new Color(.26f,.68f,1f), pulse);
            }
            else GUI.backgroundColor = over ? new Color(.20f,.31f,.49f) : new Color(.11f,.19f,.32f);
            bool hit = GUI.Button(r, label, small ? tinyButton : buttonStyle); GUI.backgroundColor = old; GUI.enabled = was; return hit;
        }
        private void Bar(float x,float y,float w,float fraction,Color color) { Panel(new Rect(x,y,w,8), new Color(.12f,.18f,.26f)); Panel(new Rect(x,y,w*Mathf.Clamp01(fraction),8),color); }
        private void Logo(float y, float size = 104)
        {
            // Subtle breathing + glow halo on the title.
            float breathe = 1f + .018f * Mathf.Sin(Time.unscaledTime * 1.6f);
            logoStyle.fontSize = Mathf.RoundToInt(size * breathe);
            Color old = GUI.contentColor;
            GUI.contentColor = new Color(.02f,.06f,.16f); GUI.Label(new Rect(5,y+8,1280,125),"VALMON",logoStyle);
            float glow = .4f + .25f * (Mathf.Sin(Time.unscaledTime * 2.1f) * .5f + .5f);
            GUI.contentColor = new Color(.12f,.36f,.66f, glow);
            for (int i=0;i<8;i++) { float a=i*Mathf.PI/4; GUI.Label(new Rect(Mathf.Cos(a)*6,y+Mathf.Sin(a)*6,1280,125),"VALMON",logoStyle); }
            GUI.contentColor = gold; GUI.Label(new Rect(0,y,1280,125),"VALMON",logoStyle); GUI.contentColor=old;
        }

        private static string LoadingDots() { int d = ((int)(Time.unscaledTime * 2f)) % 4; return new string('.', d); }
        private void MenuChrome()
        {
            Panel(new Rect(0,0,1280,54),new Color(.015f,.035f,.08f,.88f));
            Text(32,17,550,28,"VALMON  /  THE RIFT CHAMPIONSHIP",smallStyle);
            Text(920,17,325,28,"22 CREATURES   /   4 CHARACTERS",smallStyle);
            Panel(new Rect(0,54,1280,2),new Color(.15f,.54f,.73f));
        }
        private void DrawOpening()
        {
            if (video != null && video.isPrepared)
            {
                GUI.DrawTexture(new Rect(0,0,1280,720),videoTexture,ScaleMode.ScaleToFit);
                Text(900,675,340,30,"CLICK / ANY KEY / CONTROLLER TO SKIP",smallStyle); return;
            }
            float t = videoWait;
            MenuChrome();
            if (t > 1) Logo(118,Mathf.Lerp(80,104,Mathf.Clamp01((t-1)/3)));
            if (t > 4) Text(385,251,530,55,"Catch the creature. Resist the corruption.\nMaster the rift.",textStyle);
            if (t > 7) { Panel(new Rect(420,354,440,42),ink); Text(444,363,410,30,"TWO RIVALS. THREE HABITATS. ONE CHAMPION.",smallStyle); }
            Text(900,675,340,30,"CLICK / ANY KEY / CONTROLLER TO SKIP",smallStyle);
            float darkness = 1-Mathf.Clamp01(t/1.1f); if (darkness>0) Panel(new Rect(0,0,1280,720),new Color(0,0,0,darkness));
        }
        private void DrawMenu()
        {
            MenuChrome();
            if (collection) { DrawCollection(); return; }
            if (settings) { DrawSettings(); return; }
            if(helpPage!="") { DrawInfoPage(); return; }
            if(menuStep==0) { DrawHomeMenu(); return; }
            Panel(new Rect(260,112,760,567),ink); Panel(new Rect(260,112,760,3),accent);
            if (Button(new Rect(285,132,100,36),"BACK")) menuStep--;
            if (menuStep==1)
            {
                Text(410,132,590,45,"01 / SELECT YOUR HABITAT",headingStyle);
                for(int i=0;i<3;i++)
                {
                    float y=214+i*112; Panel(new Rect(288,y,704,92),new Color(.045f,.095f,.16f));
                    Panel(new Rect(288,y,5,92),World.Theme(i));
                    Text(310,y+12,420,35,mapNames[i],headingStyle);
                    Text(310,y+53,400,29,i==0?"GRASS / LEVELS 1–15":i==1?"FIRE / LEVELS 16–30":"WATER / LEVELS 31–50",smallStyle);
                    if(Button(new Rect(758,y+17,210,57),i<=unlocked?"SELECT":"LOCKED",i<=unlocked)) { selectedLevel=i; menuStep=2; }
                }
                Text(310,578,640,55,"10 Poké Balls. 8 minutes. Catch 5 to advance. Your surviving team carries forward.");
            }
            else if(menuStep==2)
            {
                Text(410,132,590,45,"02 / CHOOSE YOUR CHARACTER",headingStyle);
                for(int i=0;i<4;i++) if(Button(new Rect(315+(i%2)*330,235+(i/2)*105,310,80),catalog.characters[i].name,true,selectedCharacter==i)) selectedCharacter=i;
                Text(315,490,620,50,"Your character appears in the arena. Choose either role in the next step.");
                if(Button(new Rect(315,583,640,60),"CONTINUE",true,true)) menuStep=3;
            }
            else
            {
                Text(410,132,590,45,"03 / ROLE & STARTER",headingStyle);
                if(Button(new Rect(310,215,315,70),"TAMER",true,Role==PlayerRole.Tamer)) Role=PlayerRole.Tamer;
                if(Button(new Rect(650,215,315,70),"CORRUPTOR",true,Role==PlayerRole.Corruptor)) Role=PlayerRole.Corruptor;
                Text(312,306,650,65,Role==PlayerRole.Tamer?"Explore and capture creatures. Defeat corrupted Pokémon in turn battles.":"Control corruption and the wild Pokémon's moves. Eliminate the AI Tamer's team.");
                Text(312,396,650,30,"TAMER'S STARTER",headingStyle);
                for(int i=0;i<3;i++) if(Button(new Rect(310+i*223,443,210,68),catalog.Find(starterIds[i]).displayName,true,selectedStarter==i)) selectedStarter=i;
                if(Button(new Rect(310,581,650,62),"ENTER THE HABITAT",true,true)) StartCampaign(selectedLevel,selectedCharacter,Role,starterIds[selectedStarter]);
            }
        }
        private void DrawCollection()
        {
            Text(48,93,620,60,"THE CREATURE ROSTER",titleStyle);
            SpeciesDefinition species=catalog.species[collectionIndex];
            Text(50,583,550,45,species.displayName,headingStyle); Text(50,629,550,40,string.Join(" / ",species.types));
            Panel(new Rect(640,172,595,475),ink);
            for(int i=0;i<catalog.species.Length;i++) if(Button(new Rect(660+i%3*185,192+i/3*54,174,45),catalog.species[i].displayName,true,i==collectionIndex,true)) { collectionIndex=i; World.ShowCollection(i); }
            if(Button(new Rect(1010,90,225,52),"BACK TO TITLE")) { collection=false; World.ShowMenu(); }
        }
        private void DrawLobby() { DrawMatchLobby(); }
        private bool ReadPort() { if(int.TryParse(lobbyPort,out int port) && port>=1024 && port<=65535) { netPort=port; return true; } netStatus="Use a UDP port from 1024 to 65535."; return false; }
        private void DrawTopBar()
        {
            Panel(new Rect(20, 10, 1240, 32), ink);
            Panel(new Rect(20, 10, 1240, 2), accent);
            string mapText = finalBattle ? "FINAL / DUEL" : "MAP " + (Run.stage + 1) + " / " + mapNames[Run.stage] + " (" + RoleName(Role) + (netMode == NetMode.None ? " SOLO" : " LINK") + ")";
            Text(32, 16, 420, 20, mapText, new GUIStyle(headingStyle) { fontSize = 13 });
            Text(460, 16, 160, 20, "CATCHES " + Run.catches + "/" + CampaignRun.RequiredCatches, smallStyle);
            Text(630, 16, 150, 20, "BALLS " + Run.balls + "/" + CampaignRun.BallsPerStage, smallStyle);
            int secs = Mathf.CeilToInt(Run.seconds);
            Text(800, 14, 150, 24, finalBattle ? "FINAL" : $"{secs/60:00}:{secs%60:00}", new GUIStyle(headingStyle) { fontSize = 15 });
            if (Button(new Rect(1150, 13, 95, 26), "PAUSE", true, false, true)) TogglePause();
        }
        private void DrawPartySidebar() { DrawRoleStatus(); }
        private void DrawWorldLabels()
        {
            if (view == null || World?.wild == null) return;
            foreach (WildCreature w in World.wild)
            {
                if (w == null || w.captured || w.root == null || !w.root.activeInHierarchy) continue;
                float dist = Vector3.Distance(view.transform.position, w.root.transform.position);
                if (dist > 35f) continue;
                Vector3 p = view.WorldToScreenPoint(w.root.transform.position + Vector3.up * (w.visualHeight + .25f));
                if (p.z <= 0) continue;
                float x = (p.x - uiOffset.x) / uiScale - 60, y = (Screen.height - p.y - uiOffset.y) / uiScale;
                if (x < 150 || x > (Role == PlayerRole.Corruptor ? 765 : 1120) || y < 45 || y > 660) continue;
                Panel(new Rect(x, y, 120, 20), new Color(.02f, .04f, .08f, .85f));
                Panel(new Rect(x, y, 3, 20), w.corrupted ? new Color(1f, .2f, .25f) : new Color(.3f, 1f, .6f));
                Text(x + 5, y + 1, 112, 18, w.creature.species.displayName + " Lv." + w.creature.level + (w.corrupted ? " [C]" : ""), new GUIStyle(smallStyle) { fontSize = 9 });
            }
        }
        private void DrawCorruptionPanel() { DrawWardenAbilities(); }
        private void DrawCapturePanel()
        {
            if (ScreenState == CampaignScreen.Capture)
            {
                Panel(new Rect(340, 570, 600, 75), ink);
                Text(360, 580, 560, 32, "CAPTURE IN PROGRESS", headingStyle);
                Bar(360, 620, 560, captureClock / 2.7f, encounter != null && encounter.corrupted ? violet : accent);
            }
            DrawFooter();
        }
        private void DrawFooter()
        {
            Panel(new Rect(20, 676, 1240, 36), ink);
            Panel(new Rect(20, 676, 1240, 2), Role == PlayerRole.Tamer ? new Color(.12f, .7f, 1f) : new Color(1f, .2f, .25f));
            if (Role == PlayerRole.Tamer)
            {
                bool canDash = dashCooldown <= 0 && ScreenState == CampaignScreen.Explore;
                string dashText = dashCooldown > 0 ? $"Q / QUICK DASH ({Mathf.CeilToInt(dashCooldown)}s)" : "Q / QUICK DASH";
                if (Button(new Rect(1055, 680, 195, 28), dashText, canDash, false, true)) UseAbility("dash");

                bool nearWild = ScreenState == CampaignScreen.Explore && selected != null && !selected.captured;
                float distance = (nearWild && World != null && World.tamer != null && selected.root != null)
                    ? Vector3.Distance(World.tamer.transform.position, selected.root.transform.position)
                    : 999f;
                bool canCatch = nearWild && distance <= 4.4f;

                if (canCatch)
                {
                    if (Button(new Rect(860, 680, 185, 28), "Z / CATCH", true, true, true)) BeginCapture(selected);
                    Text(32, 682, 820, 24, "▶ " + selected.creature.species.displayName.ToUpperInvariant() + " IN RANGE (" + distance.ToString("0.0") + "m): PRESS Z TO CATCH" + (selected.corrupted ? " (CORRUPTED BATTLE)" : "") + "  •  WASD move  •  Shift sprint", smallStyle);
                }
                else
                {
                    string footerInfo;
                    if (nearWild && distance <= 18f)
                        footerInfo = selected.creature.species.displayName + " (" + distance.ToString("0") + "m)  •  Press Z to catch (get within 4m)  •  WASD move  •  Shift sprint  •  R respawn  •  Esc pause";
                    else
                        footerInfo = "WASD move  •  Shift sprint  •  Right mouse orbit  •  Press Z to catch  •  R respawn  •  Esc shared pause";
                    Text(32, 682, 1015, 24, footerInfo, smallStyle);
                }
            }
            else
            {
                Text(32, 682, 1215, 24, "WASD move  •  Shift sprint  •  Right mouse orbit  •  Q corrupt  •  E trap  •  R lock gate  •  F scan  •  Esc shared pause", smallStyle);
            }
        }
        private void DrawPartnerSelection()
        {
            Panel(new Rect(280,190,650,342),ink); Text(305,212,600,40,Role==PlayerRole.Tamer?"CHOOSE YOUR BATTLER":"WAITING FOR TAMER",headingStyle);
            for(int i=0;i<Run.party.Count;i++) { OwnedCreature c=Run.party[i]; if(Button(new Rect(300+i%2*310,273+i/2*43,295,38),c.species.displayName+" Lv."+c.level+" / "+c.health+" HP",Role==PlayerRole.Tamer)) ChoosePartner(i); }
        }
        private void HealthCard(float x,OwnedCreature c,string side,Color color)
        {
            Panel(new Rect(x,114,390,115),ink); Text(x+18,126,355,35,c.species.displayName+" Lv."+c.level,new GUIStyle(headingStyle){fontSize=24});
            Text(x+18,166,355,28,side+" / "+string.Join(" / ",c.species.types)+(c.status!=""?" / "+c.status:""),smallStyle);
            Bar(x+18,199,280,(float)c.health/c.MaxHealth,color); Text(x+302,191,82,26,c.health+"/"+c.MaxHealth,smallStyle);
        }
        private void DrawBattle()
        {
            if (Run.party.Count == 0 || opponent == null) return;
            HealthCard(30, Run.party[partyIndex], "TAMER", accent);
            HealthCard(860, opponent, finalBattle ? "CORRUPTOR / " + bossTeam.Count + " LEFT" : "CORRUPTED WILD", violet);

            // Translucent glassmorphism floating cards instead of solid opaque screen-blocking panel
            Color cardBg = new Color(.02f, .04f, .09f, .75f);
            Color barAccent = HumanTurn ? accent : violet;

            // Compact left card: Turn status, battle message, and switch buttons (height 100px instead of 222px)
            Panel(new Rect(30, 608, 470, 100), cardBg);
            Panel(new Rect(30, 608, 470, 2), barAccent);
            Text(44, 613, 442, 24, turnDelay > 0 ? "RESOLVING TURN..." : HumanTurn ? "YOUR TURN" : "OPPONENT'S TURN", smallStyle);
            Text(44, 634, 442, 36, message, tinyStyle);

            var team = Role == PlayerRole.Tamer ? Run.party : bossTeam;
            if (Role == PlayerRole.Tamer || finalBattle)
            {
                Text(44, 674, 70, 24, "SWITCH:", tinyStyle);
                for (int i = 0; i < team.Count; i++)
                {
                    if (Button(new Rect(105 + i * 82, 672, 76, 26), team[i].species.displayName, HumanTurn && turnDelay <= 0, Role == PlayerRole.Tamer ? i == partyIndex : team[i] == opponent, true))
                        SwitchParty(i);
                }
            }

            // Compact right card: 50% smaller Moves panel (2x2 grid, each button 210x44 instead of 310x60, panel ~450x100 instead of 660x222)
            OwnedCreature actor = Role == PlayerRole.Tamer ? Run.party[partyIndex] : opponent;
            Panel(new Rect(520, 608, 450, 100), cardBg);
            Panel(new Rect(520, 608, 450, 2), barAccent);

            if (BattleRules.HasMoves(actor))
            {
                for (int i = 0; i < actor.species.moves.Length; i++)
                {
                    MoveDefinition m = actor.species.moves[i];
                    float bx = 530 + (i % 2) * 218;
                    float by = 615 + (i / 2) * 44;
                    string moveDesc = m.name + " (" + m.type + ")\n" + (m.power > 0 ? "Pwr " + m.power : m.effect.ToUpperInvariant()) + " • PP " + actor.pp[i] + "/" + m.pp;
                    if (Button(new Rect(bx, by, 210, 39), moveDesc, HumanTurn && turnDelay <= 0 && actor.pp[i] > 0, false, true))
                        PlayMove(i);
                }
            }
            else if (Button(new Rect(530, 622, 430, 60), "STRUGGLE / RECOIL", HumanTurn && turnDelay <= 0))
            {
                PlayMove(-1);
            }
        }
        private void DrawStageClear()
        {
            Panel(new Rect(220,120,840,500),ink);
            Text(255,145,780,70,"HABITAT CLEARED",titleStyle);
            Text(255,235,780,70,"3 new Pokémon caught in "+mapNames[Run.stage]+".\n"+Run.party.Count+" teammates remain.");
            Text(255,325,780,50,Run.stage<2?"Next habitat: team healed, ten balls and eight minutes.":"Final duel: defeat three random Corruptor Pokémon.",smallStyle);

            // Character swap (single-player only; LAN character is locked at lobby)
            bool canSwap = netMode == NetMode.None && catalog != null && catalog.characters != null && catalog.characters.Length > 0;
            if (canSwap)
            {
                Text(255,385,780,28,"CHOOSE YOUR CHARACTER FOR THE NEXT ROUND", smallStyle);
                int count = Mathf.Min(4, catalog.characters.Length);
                for (int i = 0; i < count; i++)
                    if (Button(new Rect(255+(i%2)*395, 420+(i/2)*52, 375, 46), catalog.characters[i].name, true, selectedCharacter==i))
                        selectedCharacter = i;
            }

            if(Button(new Rect(255,540,780,62),netMode==NetMode.Client?"WAITING FOR HOST":Run.stage<2?"ADVANCE TO LEVEL "+(Run.stage+2):"FACE THE CORRUPTOR",netMode!=NetMode.Client,true)) Advance();
        }
        private void DrawResult()
        {
            Panel(new Rect(0,0,1280,720),new Color(.015f,.025f,.06f,.96f));
            Logo(40,72);

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

            Panel(new Rect(140,150,1000,460), ink);
            Panel(new Rect(140,150,1000,6), outcomeTamer ? gold : violet);

            Text(170,170,940,70, outcomeTamer ? "VICTORY" : "MATCH FINISHED", titleStyle);
            Text(170,235,940,34, resultReason, smallStyle);

            // Stats grid (left column)
            float sx = 180, sy = 300; float lineH = 36;
            Text(sx, sy + 0*lineH, 240, 30, "Habitats cleared", smallStyle);
            Text(sx + 250, sy + 0*lineH, 100, 30, habitatsCleared + " / 3", headingStyle);
            Text(sx, sy + 1*lineH, 240, 30, "Final-stage catches", smallStyle);
            Text(sx + 250, sy + 1*lineH, 100, 30, catches.ToString(), headingStyle);
            Text(sx, sy + 2*lineH, 240, 30, "Balls remaining", smallStyle);
            Text(sx + 250, sy + 2*lineH, 100, 30, balls + " / 10", headingStyle);
            Text(sx, sy + 3*lineH, 240, 30, "Team members", smallStyle);
            Text(sx + 250, sy + 3*lineH, 100, 30, teamSize.ToString(), headingStyle);
            Text(sx, sy + 4*lineH, 240, 30, "Time remaining", smallStyle);
            Text(sx + 250, sy + 4*lineH, 160, 30, Mathf.FloorToInt(secondsLeft/60f) + ":" + (Mathf.FloorToInt(secondsLeft)%60).ToString("D2"), headingStyle);

            // Score breakdown (right column)
            float bx = 630, by = 300;
            Text(bx, by - 30, 400, 30, "SCORE BREAKDOWN", headingStyle);
            Text(bx, by + 0*lineH, 300, 30, "Catches x100", smallStyle);
            Text(bx + 300, by + 0*lineH, 160, 30, "+ " + catchPts, textStyle);
            Text(bx, by + 1*lineH, 300, 30, "Balls x25", smallStyle);
            Text(bx + 300, by + 1*lineH, 160, 30, "+ " + ballPts, textStyle);
            Text(bx, by + 2*lineH, 300, 30, "Time x2/s", smallStyle);
            Text(bx + 300, by + 2*lineH, 160, 30, "+ " + timeBonus, textStyle);
            Text(bx, by + 3*lineH, 300, 30, "Habitats x500", smallStyle);
            Text(bx + 300, by + 3*lineH, 160, 30, "+ " + habitatPts, textStyle);
            Text(bx, by + 4*lineH, 300, 30, "Team x150", smallStyle);
            Text(bx + 300, by + 4*lineH, 160, 30, "+ " + teamPts, textStyle);
            if (outcomeTamer) {
                Text(bx, by + 5*lineH, 300, 30, "Victory bonus", smallStyle);
                Text(bx + 300, by + 5*lineH, 160, 30, "+ " + winBonus, textStyle);
            }

            // Total line
            Panel(new Rect(170, 540, 940, 2), new Color(.3f,.35f,.5f));
            Text(180, 548, 400, 50, "TOTAL SCORE", headingStyle);
            GUIStyle totalStyle = new GUIStyle(titleStyle);
            totalStyle.normal.textColor = outcomeTamer ? gold : accent;
            totalStyle.alignment = TextAnchor.MiddleRight;
            Text(600, 548, 510, 55, totalPts.ToString("N0") + " pts", totalStyle);

            // Buttons
            if(Button(new Rect(140,635,480,55),"RETURN TO TITLE",true,true)) BackToTitle();
            if(Button(new Rect(660,635,480,55),"QUIT")) Quit();
        }
        private void DrawPause()
        {
            Panel(new Rect(0,0,1280,720),new Color(0,0,0,.65f)); Panel(new Rect(400,175,480,365),ink);
            Text(435,205,410,55,"MATCH PAUSED",headingStyle); Text(435,270,410,50,netMode==NetMode.None?"The game and timer are paused.":"Both laptops share this pause.");
            if(Button(new Rect(435,344,410,55),"RESUME",true,true)) TogglePause(); if(Button(new Rect(435,429,410,55),"LEAVE MATCH")) BackToTitle();
        }
        private void DrawSettings()
        {
            Panel(new Rect(330,180,620,420),ink); Text(365,212,550,60,"SETTINGS",headingStyle); Text(365,290,550,35,"Volume / "+Mathf.RoundToInt(volume*100)+"%");
            volume=GUI.HorizontalSlider(new Rect(365,340,545,25),volume,0,1); AudioListener.volume=volume;
            Text(365,390,550,70,"Level unlocks save on this laptop. LAN play uses UDP port 7788 by default.",smallStyle);
            if(Button(new Rect(365,501,545,55),"SAVE & BACK",true,true)) { PlayerPrefs.SetFloat("VALMON.Volume",volume); PlayerPrefs.Save(); settings=false; }
        }
        private void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying=false;
#else
            Application.Quit();
#endif
        }
    }
}


