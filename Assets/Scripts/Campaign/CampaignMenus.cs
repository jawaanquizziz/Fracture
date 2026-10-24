using System.Collections;
using System.Linq;
using UnityEngine;

namespace Valmon.Campaign
{
    public partial class CampaignController
    {
        private string helpPage = "";
        private readonly Texture2D[] arenaPreviews = new Texture2D[3], portraits = new Texture2D[4];
        private bool capturingArenaPreviews;

        private static string CleanPlayerName(string value)
        {
            string clean = new string((value ?? "").Where(c => !char.IsControl(c) && c != '<' && c != '>').Take(24).ToArray()).Trim();
            return clean.Length == 0 ? "Player" : clean;
        }

        private void EnterLobby()
        {
            playerName = CleanPlayerName(playerName);
            PlayerPrefs.SetString("VALMON.PlayerName", playerName);
            PlayerPrefs.Save();
            ScreenState = CampaignScreen.Lobby;
            World.ShowMapMenu(selectedLevel);
            StartCoroutine(PrepareArenaPreviews());
        }

        private IEnumerator PrepareArenaPreviews()
        {
            if (capturingArenaPreviews) yield break;
            capturingArenaPreviews = true;
            for (int i = 0; i < 3; i++)
            {
                if (arenaPreviews[i] == null)
                {
                    World.ShowMapMenu(i);
                    yield return null;
                    arenaPreviews[i] = World.CaptureMapPreview();
                }
            }
            for (int i = 0; i < 4; i++)
            {
                if (portraits[i] == null)
                {
                    portraits[i] = World.CapturePortrait(i);
                    yield return null;
                }
            }
            World.ShowMapMenu(selectedLevel);
            capturingArenaPreviews = false;
        }

        private void DrawHomeMenu()
        {
            // Sleek left-side glassmorphic navigation dock
            Rect dockRect = new Rect(40, 65, 420, 620);
            DrawGlassCard(dockRect, UITheme.GlassDark, UITheme.BorderMuted, UITheme.CyanPrimary, 10);

            // VALMON Brand title & tagline
            Text(65, 85, 370, 60, "VALMON", new GUIStyle(titleStyle) { fontSize = 56, fontStyle = FontStyle.Bold });
            DrawBadge(new Rect(65, 148, 250, 22), "TAME • BATTLE • PROTECT", UITheme.CyanPrimary, Color.white, 10);

            // Primary Play Online Button (pulsing prominent cyan)
            if (ModernButton(new Rect(65, 185, 370, 52), "▶  PLAY ONLINE", true, true)) EnterLobby();

            // Navigation stack with consistent sizing & spacing
            if (ModernButton(new Rect(65, 245, 370, 42), "SOLO CAMPAIGN")) menuStep = 1;
            if (ModernButton(new Rect(65, 295, 370, 42), "CREATURE ROSTER")) { collection = true; World.ShowCollection(collectionIndex); }
            if (ModernButton(new Rect(65, 345, 370, 42), "HOW TO PLAY")) helpPage = "HOW TO PLAY";
            if (ModernButton(new Rect(65, 395, 370, 42), "SETTINGS")) settings = true;
            if (ModernButton(new Rect(65, 445, 370, 42), "CREDITS")) helpPage = "CREDITS";
            if (ModernButton(new Rect(65, 495, 370, 42), "EXIT GAME")) Quit();

            // Player Name input card
            Rect nameCard = new Rect(65, 555, 370, 85);
            DrawGlassPanel(nameCard, UITheme.GlassInset, UITheme.BorderMuted, 6);
            Text(80, 565, 340, 22, "YOUR TRAINER NAME", smallStyle);
            playerName = GUI.TextField(new Rect(80, 590, 340, 36), playerName, 24, fieldStyle);

            // Subtle system indicator on bottom right
            GUIStyle verStyle = new GUIStyle(smallStyle) { alignment = TextAnchor.MiddleRight };
            verStyle.normal.textColor = UITheme.TextDim;
            Text(980, 685, 260, 22, "LAN UDP  •  UNITY 6.6 URP", verStyle);
        }

        private void DrawInfoPage()
        {
            Rect modalRect = new Rect(240, 110, 800, 530);
            DrawGlassCard(modalRect, UITheme.GlassDark, UITheme.CyanPrimary, UITheme.CyanPrimary, 10);

            Text(275, 135, 730, 45, helpPage, titleStyle);

            string content = helpPage == "CREDITS" ?
                "VALMON: THE RIFT CHAMPIONSHIP\n\nOriginal characters, creature models and stylized Blender arenas are sourced from the project's asset library.\n\nBuilt with Unity 6.6 URP, featuring real-time LAN UDP multiplayer synchronization, authoritative role switching, and dynamic procedural battle systems.\n\nAll gameplay mechanics, creature interactions, and championship rules are built for competitive LAN tournament play." :
                "TWO RIVALS  •  THREE HABITATS  •  AUTHORITATIVE ROLES\n\n" +
                "TAMER CONTROLS:\n" +
                "• WASD to Move  •  Shift to Sprint  •  Right Mouse to Orbit Camera\n" +
                "• Z to Catch wild creatures when within 4.0m\n" +
                "• Q for Quick Dash  •  R to Respawn  •  Esc to Pause\n" +
                "• Catch 5 creatures to clear the habitat. Capture 6 for a +1 Bonus Point!\n\n" +
                "WARDEN CONTROLS:\n" +
                "• Q: Corrupt nearest creature (25 NRG)  •  E: Place snare trap (30 NRG)\n" +
                "• R: Lock nearby gate (35 NRG)  •  F: Tactical radar scan (15 NRG)\n" +
                "• Corrupt 6 distinct creatures for a +1 Bonus Point!\n\n" +
                "TURN-BASED BATTLES:\n" +
                "Corrupted creatures initiate combat upon capture. Select moves or switch Pokémon when it is your turn.";

            Text(275, 195, 730, 340, content, new GUIStyle(textStyle) { fontSize = 14 });

            if (ModernButton(new Rect(275, 560, 730, 50), "RETURN TO MENU", true, true)) helpPage = "";
        }

        private void ArenaCard(Rect r, int index, bool canSelect)
        {
            Color theme = UITheme.GetArenaColor(index);
            bool isSelected = selectedLevel == index;
            DrawGlassCard(r, UITheme.GlassCard, isSelected ? UITheme.CyanPrimary : UITheme.BorderMuted, isSelected ? UITheme.CyanPrimary : theme, 8);

            if (arenaPreviews[index] != null)
            {
                Rect imgRect = new Rect(r.x + 3, r.y + 3, r.width - 6, r.height - 6);
                GUI.DrawTexture(imgRect, arenaPreviews[index], ScaleMode.ScaleAndCrop);
                GUI.DrawTexture(imgRect, UITextures.Solid(new Color(0.02f, 0.04f, 0.08f, 0.62f)));
            }

            // Top theme stripe
            GUI.DrawTexture(new Rect(r.x + 6, r.y + 1, r.width - 12, 2), UITextures.Solid(isSelected ? UITheme.CyanPrimary : theme));

            string badgeText = index == 0 ? "GRASS" : (index == 1 ? "FIRE" : "WATER");
            DrawBadge(new Rect(r.x + 12, r.y + 12, 65, 20), badgeText, theme, Color.white, 10);
            if (isSelected)
            {
                DrawBadge(new Rect(r.x + r.width - 85, r.y + 12, 75, 20), "SELECTED", UITheme.CyanPrimary, Color.white, 10);
            }

            Text(r.x + 14, r.y + 36, r.width - 28, 28, mapNames[index], new GUIStyle(headingStyle) { fontSize = 18 });
            Text(r.x + 14, r.y + 64, r.width - 28, 20, index == 0 ? "Levels 1–15 • Ancient Forest" : index == 1 ? "Levels 16–30 • Molten Fortress" : "Levels 31–50 • Abyssal Rift", smallStyle);

            if (canSelect && GUI.Button(r, "", GUIStyle.none))
            {
                selectedLevel = index;
                World.ShowMapMenu(index);
            }
        }

        private void PlayerCard(Rect r, string name, int character, string role, bool ready, bool local)
        {
            bool isWarden = role == "WARDEN";
            Color roleCol = isWarden ? UITheme.CrimsonWarden : UITheme.CyanPrimary;
            Color accentCol = ready ? roleCol : UITheme.BorderMuted;

            DrawGlassCard(r, UITheme.GlassCard, accentCol, ready ? roleCol : UITheme.BorderLight, 8);

            Rect portRect = new Rect(r.x + 16, r.y + 18, 95, 115);
            DrawGlassPanel(portRect, new Color(0.02f, 0.04f, 0.08f, 0.85f), ready ? roleCol : UITheme.BorderMuted, 6);
            if (character >= 0 && character < portraits.Length && portraits[character] != null)
            {
                GUI.DrawTexture(new Rect(portRect.x + 4, portRect.y + 4, portRect.width - 8, portRect.height - 8), portraits[character], ScaleMode.ScaleToFit);
            }
            else
            {
                GUIStyle iconSt = new GUIStyle(titleStyle) { alignment = TextAnchor.MiddleCenter, fontSize = 34 };
                iconSt.normal.textColor = ready ? roleCol : UITheme.TextDim;
                GUI.Label(portRect, ready ? "P" : "?", iconSt);
            }

            Text(r.x + 125, r.y + 20, r.width - 140, 30, name, new GUIStyle(headingStyle) { fontSize = 21 });

            if (!string.IsNullOrEmpty(role))
            {
                DrawBadge(new Rect(r.x + 125, r.y + 54, 110, 24), role, roleCol, Color.white, 11);
            }
            else if (ready)
            {
                DrawBadge(new Rect(r.x + 125, r.y + 54, 110, 24), "CONNECTED", UITheme.ArenaGrass, Color.white, 11);
            }
            else if (local)
            {
                DrawBadge(new Rect(r.x + 125, r.y + 54, 130, 24), "YOUR CHARACTER", UITheme.CyanPrimary, Color.white, 11);
            }
            else
            {
                string waitText = "WAITING FOR P2" + LoadingDots();
                DrawBadge(new Rect(r.x + 125, r.y + 54, 150, 24), waitText, new Color(0.25f, 0.18f, 0.05f, 0.8f), UITheme.AmberWarning, 11);
            }

            string descText = ready
                ? (local ? "Host / Local Trainer (Ready)" : "Peer Player Connected (Ready)")
                : (local ? "Host or join a room below" : "No second player connected on LAN");
            Text(r.x + 125, r.y + 88, r.width - 140, 34, descText, smallStyle);
        }

        private void DrawMatchLobby()
        {
            MenuChrome();
            Text(175, 75, 850, 45, "MULTIPLAYER LOBBY", titleStyle);

            string statusText = capturingArenaPreviews
                ? ("Preparing arena and character previews" + LoadingDots())
                : (netMode != NetMode.None && !netConnected ? (netStatus + LoadingDots()) : netStatus);
            Text(175, 122, 870, 26, statusText, smallStyle);

            // Player 1 & Player 2 cards
            PlayerCard(new Rect(175, 155, 455, 155), CleanPlayerName(playerName), selectedCharacter, "", netMode == NetMode.Host || netConnected, true);
            PlayerCard(new Rect(650, 155, 455, 155), netConnected ? remotePlayerName : "WAITING FOR PLAYER 2", netConnected ? netRemoteCharacter : -1, "", netConnected, false);

            bool editable = netMode == NetMode.None && !capturingArenaPreviews;

            // Character selector chips
            for (int i = 0; i < 4; i++)
            {
                bool isSel = selectedCharacter == i;
                if (ModernButton(new Rect(175 + i * 115, 320, 110, 28), catalog.characters[i].name, editable, isSel, true))
                    selectedCharacter = i;
            }

            // Arena selection cards
            for (int i = 0; i < 3; i++)
                ArenaCard(new Rect(175 + i * 315, 360, 300, 95), i, (netMode == NetMode.None || netMode == NetMode.Host) && !capturingArenaPreviews);

            // Network credentials card
            Rect netCard = new Rect(175, 470, 930, 195);
            DrawGlassCard(netCard, UITheme.GlassDark, UITheme.BorderMuted, UITheme.CyanPrimary, 8);

            Text(195, 482, 380, 22, "HOST IPv4 ADDRESS", smallStyle);
            Text(600, 482, 130, 22, "UDP PORT", smallStyle);
            Text(760, 482, 160, 22, "ROOM CODE", smallStyle);

            GUI.enabled = editable;
            lobbyJoinIp = GUI.TextField(new Rect(195, 508, 380, 38), lobbyJoinIp, fieldStyle);
            lobbyPort = GUI.TextField(new Rect(600, 508, 130, 38), lobbyPort, 5, fieldStyle);

            if (netMode != NetMode.Host)
                netRoomCode = GUI.TextField(new Rect(760, 508, 160, 38), netRoomCode, 6, fieldStyle);
            else
                Text(760, 510, 160, 36, netRoomCode, new GUIStyle(headingStyle) { fontSize = 20, alignment = TextAnchor.MiddleCenter });

            GUI.enabled = true;

            // Action buttons
            if (ModernButton(new Rect(195, 560, 290, 46), netMode == NetMode.Host ? "WAITING FOR PEER..." : "HOST ROOM", editable, true))
            {
                if (ReadPort()) NetHost();
            }

            if (ModernButton(new Rect(505, 560, 280, 46), netMode == NetMode.Client ? "CONNECTING..." : "JOIN ROOM", editable))
            {
                if (ReadPort() && netRoomCode.Trim().Length == 6) NetJoin(lobbyJoinIp);
                else netStatus = "Enter host IPv4 and 6-digit room code.";
            }

            if (ModernButton(new Rect(805, 560, 280, 46), "LEAVE LOBBY"))
            {
                NetDisconnect();
                netRoomCode = "";
                OpenMenu();
            }

            if (netMode == NetMode.Host)
            {
                Text(195, 625, 900, 25, "YOUR LOCAL IPv4: " + netLocalIp + "  •  Share this IP and room code with Player 2", smallStyle);
            }
            else if (netMode == NetMode.None)
            {
                Text(195, 625, 900, 25, "Connect both laptops to same Wi-Fi / hotspot. Host selects arena. Roles assign automatically.", smallStyle);
            }

            // Start Match button (top right when ready)
            if (netMode == NetMode.Host && !autoStartArmed)
            {
                if (ModernButton(new Rect(925, 75, 180, 48), "START MATCH ▶", netRemoteReady, true))
                    HostStartMatch();
            }
        }

        private void DrawCountdown()
        {
            GUI.DrawTexture(new Rect(0, 0, 1280, 720), UITextures.Solid(new Color(0.01f, 0.02f, 0.05f, 0.90f)));

            Text(360, 70, 560, 50, waitingForArena ? "PLAYERS READY" : "MATCH READY", new GUIStyle(titleStyle) { alignment = TextAnchor.MiddleCenter });
            Text(360, 125, 560, 30, "Authoritative roles assigned  •  Round " + (roundIndex + 1) + " of 3", new GUIStyle(smallStyle) { alignment = TextAnchor.MiddleCenter });

            PlayerRole hostRole = netMode == NetMode.Host ? Role : RemoteRole;
            int hostCharacter = netMode == NetMode.Host ? netLocalCharacter : netRemoteCharacter;
            int clientCharacter = netMode == NetMode.Client ? netLocalCharacter : netRemoteCharacter;

            // Host Card (Left)
            PlayerCard(new Rect(160, 185, 420, 160), hostPlayerName, hostCharacter, RoleName(hostRole), true, netMode == NetMode.Host);

            // VS Emblem & Countdown Center
            Rect vsRect = new Rect(600, 215, 80, 80);
            DrawGlassPanel(vsRect, UITheme.GlassDark, UITheme.CyanPrimary, 40);
            Text(600, 235, 80, 40, "VS", new GUIStyle(headingStyle) { alignment = TextAnchor.MiddleCenter });

            // Client Card (Right)
            PlayerCard(new Rect(700, 185, 420, 160), clientPlayerName, clientCharacter, RoleName(hostRole == PlayerRole.Tamer ? PlayerRole.Corruptor : PlayerRole.Tamer), true, netMode == NetMode.Client);

            // Selected Arena Card
            ArenaCard(new Rect(320, 385, 640, 95), Run.stage, false);

            // Countdown timer display
            Text(360, 510, 560, 35, waitingForArena ? "Synchronizing match simulation..." : "MATCH STARTS IN", new GUIStyle(headingStyle) { alignment = TextAnchor.MiddleCenter });

            if (!waitingForArena)
            {
                int countNum = Mathf.Max(1, Mathf.CeilToInt(countdownLeft));
                GUIStyle cdStyle = new GUIStyle(titleStyle) { fontSize = 64, alignment = TextAnchor.MiddleCenter };
                cdStyle.normal.textColor = UITheme.CyanGlow;
                Text(540, 555, 200, 80, countNum.ToString(), cdStyle);
            }
        }

        private void DrawRoundResults()
        {
            GUI.DrawTexture(new Rect(0, 0, 1280, 720), UITextures.Solid(new Color(0.015f, 0.025f, 0.06f, 0.94f)));
            bool final = ScreenState == CampaignScreen.MatchResult;

            Text(240, 60, 800, 55, final ? "FINAL MATCH RESULTS" : "ROUND " + (roundIndex + 1) + " COMPLETE", new GUIStyle(titleStyle) { alignment = TextAnchor.MiddleCenter });

            string winText = final
                ? "CHAMPION: " + (hostMatchWinner ? hostPlayerName : clientPlayerName).ToUpperInvariant()
                : (outcomeTamer ? "TAMER WINS THE ROUND" : "WARDEN WINS THE ROUND");
            Color winCol = (final ? UITheme.GoldChampion : (outcomeTamer ? UITheme.CyanPrimary : UITheme.CrimsonWarden));
            GUIStyle winStyle = new GUIStyle(headingStyle) { alignment = TextAnchor.MiddleCenter, fontSize = 22 };
            winStyle.normal.textColor = winCol;
            Text(240, 120, 800, 35, winText, winStyle);

            // Side-by-side performance cards
            Rect tamerCard = new Rect(165, 175, 455, 240);
            Rect wardenCard = new Rect(660, 175, 455, 240);

            DrawGlassCard(tamerCard, UITheme.GlassDark, UITheme.CyanPrimary, UITheme.CyanPrimary, 8);
            DrawGlassCard(wardenCard, UITheme.GlassDark, UITheme.CrimsonWarden, UITheme.CrimsonWarden, 8);

            DrawBadge(new Rect(185, 190, 90, 22), "TAMER", UITheme.CyanPrimary, Color.white, 10);
            DrawBadge(new Rect(680, 190, 90, 22), "WARDEN", UITheme.CrimsonWarden, Color.white, 10);

            // Stats breakdown
            string tamerDetails = $"Creatures captured: {Run.catches} / {CampaignRun.RequiredCatches}\n" +
                                 $"Time remaining: {Mathf.CeilToInt(Run.seconds)}s\n" +
                                 $"Round score: {roundTamerScore} PTS";
            Text(185, 230, 415, 120, tamerDetails, new GUIStyle(textStyle) { fontSize = 15 });

            string wardenDetails = $"Captures stopped: {roundStopped}\n" +
                                  $"Sabotage actions: {roundSabotages}\n" +
                                  $"Bonus point: +{WardenBonus}\n" +
                                  $"Round score: {roundWardenScore} PTS";
            Text(680, 230, 415, 120, wardenDetails, new GUIStyle(textStyle) { fontSize = 15 });

            // Aggregate Match Standings
            Rect standCard = new Rect(165, 435, 950, 75);
            DrawGlassPanel(standCard, UITheme.GlassCard, UITheme.BorderMuted, 8);

            Text(195, 448, 420, 48, hostPlayerName + " (Host)\n" + hostWins + " Rounds Won  •  " + hostScore + " Total Points", headingStyle);
            Text(680, 448, 420, 48, clientPlayerName + " (Client)\n" + clientWins + " Rounds Won  •  " + clientScore + " Total Points", headingStyle);

            string bottomHint = final
                ? "Match concluded. Championship winner decided by rounds won."
                : "Next round: roles swap automatically and both players load the next arena.";
            Text(165, 530, 950, 25, bottomHint, new GUIStyle(smallStyle) { alignment = TextAnchor.MiddleCenter });

            // Action Buttons
            if (ModernButton(new Rect(165, 580, 455, 55), final ? "PLAY AGAIN" : "NEXT ROUND ▶", true, true))
            {
                if (final) PlayAgain();
                else NextRound();
            }

            if (ModernButton(new Rect(660, 580, 455, 55), "RETURN TO LOBBY")) ReturnToLobby();
        }
    }
}
