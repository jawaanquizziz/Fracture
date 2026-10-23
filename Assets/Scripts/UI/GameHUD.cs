using UnityEngine;
using Valmon.Gameplay;

namespace Valmon.UI
{
    public class GameHUD : MonoBehaviour
    {
        private GUIStyle headerStyle;
        private GUIStyle cardStyle;
        private GUIStyle labelStyle;
        private GUIStyle titleStyle;
        private GUIStyle buttonStyle;
        private GUIStyle energyFillStyle;
        private bool stylesInitialized = false;

        void OnGUI()
        {
            InitStyles();

            GameManager gm = GameManager.Instance;
            if (gm == null) return;

            // 1. Top Match Status Bar
            DrawTopBar(gm);

            // 2. Tamer HUD (when Tamer active or in spectator)
            DrawTamerPanel(gm);

            // 3. Warden HUD
            DrawWardenPanel(gm);

            // 4. Stage Select Quick Bar
            DrawStageSelectBar(gm);

            // 5. Match Finished Overlay
            if (gm.state == MatchState.MatchFinished)
            {
                DrawResultsOverlay(gm);
            }
            else if (gm.state == MatchState.RoleSwitching)
            {
                DrawRoleSwitchOverlay();
            }
        }

        private void InitStyles()
        {
            if (stylesInitialized) return;

            headerStyle = new GUIStyle(GUI.skin.box);
            headerStyle.fontSize = 16;
            headerStyle.fontStyle = FontStyle.Bold;
            headerStyle.normal.textColor = Color.white;
            headerStyle.alignment = TextAnchor.MiddleCenter;

            cardStyle = new GUIStyle(GUI.skin.box);
            cardStyle.padding = new RectOffset(12, 12, 12, 12);

            labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.fontSize = 13;
            labelStyle.normal.textColor = Color.white;

            titleStyle = new GUIStyle(GUI.skin.label);
            titleStyle.fontSize = 15;
            titleStyle.fontStyle = FontStyle.Bold;
            titleStyle.normal.textColor = new Color(0.35f, 0.85f, 1f);

            buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.fontSize = 13;
            buttonStyle.fontStyle = FontStyle.Bold;

            stylesInitialized = true;
        }

        private void DrawTopBar(GameManager gm)
        {
            float width = 440;
            float height = 50;
            float x = (Screen.width - width) / 2f;
            float y = 15;

            GUILayout.BeginArea(new Rect(x, y, width, height), headerStyle);
            int minutes = Mathf.FloorToInt(gm.timeRemaining / 60f);
            int seconds = Mathf.FloorToInt(gm.timeRemaining % 60f);
            string timeStr = $"{minutes:00}:{seconds:00}";

            string mapName = (gm.currentMapIndex == 0) ? "Map 1: Forest Ruins" :
                             (gm.currentMapIndex == 1) ? "Map 2: Tower Fortress" : "Map 3: Coastal Shore";

            GUILayout.Label($"<b>ROUND {gm.currentRound} / {gm.maxRounds}</b>   |   ⏱️ <b>{timeStr}</b>   |   📍 <b>{mapName}</b>", labelStyle);
            GUILayout.EndArea();
        }

        private void DrawTamerPanel(GameManager gm)
        {
            bool isTamer = (gm.localRole == GameRole.Tamer);
            float width = 280;
            float height = 180;
            float x = 20;
            float y = 20;

            GUILayout.BeginArea(new Rect(x, y, width, height), cardStyle);
            GUILayout.Label(isTamer ? "🟢 <b>ACTIVE: TAMER ROLE</b>" : "👤 <b>TAMER STATS</b>", titleStyle);
            GUILayout.Space(4);
            GUILayout.Label($"Score: <b>{gm.tamerScore} pts</b>", labelStyle);
            GUILayout.Label("• <b>WASD</b>: Move character", labelStyle);
            GUILayout.Label("• <b>Shift</b>: Sprint  |  <b>Space</b>: Jump", labelStyle);
            GUILayout.Label("• <b>[E] / Right-Click</b>: Throw Poké Ball", labelStyle);
            GUILayout.Label("• <b>[Tab]</b>: Switch to Warden Drone View", labelStyle);
            GUILayout.EndArea();
        }

        private void DrawWardenPanel(GameManager gm)
        {
            bool isWarden = (gm.localRole == GameRole.Warden);
            WardenController warden = WardenController.Instance;
            float width = 310;
            float height = 210;
            float x = Screen.width - width - 20;
            float y = 20;

            GUILayout.BeginArea(new Rect(x, y, width, height), cardStyle);
            GUILayout.Label(isWarden ? "🔴 <b>ACTIVE: WARDEN ROLE</b>" : "👁️ <b>WARDEN STATS</b>", titleStyle);
            GUILayout.Space(4);
            GUILayout.Label($"Score: <b>{gm.wardenScore} pts</b>", labelStyle);

            if (warden != null)
            {
                float energyPercent = warden.currentEnergy / warden.maxEnergy;
                GUILayout.Label($"Rift Energy: <b>{Mathf.FloorToInt(warden.currentEnergy)} / {warden.maxEnergy}</b>", labelStyle);

                // Energy Bar
                Rect barRect = GUILayoutUtility.GetRect(width - 24, 14);
                GUI.Box(barRect, "");
                Rect fillRect = new Rect(barRect.x, barRect.y, barRect.width * energyPercent, barRect.height);
                GUI.color = new Color(0.9f, 0.2f, 0.4f);
                GUI.Box(fillRect, "");
                GUI.color = Color.white;
            }

            GUILayout.Space(4);
            GUILayout.Label("• <b>[1] Corrupt Creature</b> (Cost 30)", labelStyle);
            GUILayout.Label("• <b>[2] Deploy Rift Trap</b> (Cost 25)", labelStyle);
            GUILayout.Label("• <b>[3] Unleash Rift Surge</b> (Cost 45)", labelStyle);
            GUILayout.Label("• <b>[Tab]</b>: Switch to Tamer View", labelStyle);
            GUILayout.EndArea();
        }

        private void DrawStageSelectBar(GameManager gm)
        {
            float width = 480;
            float height = 45;
            float x = (Screen.width - width) / 2f;
            float y = Screen.height - height - 15;

            GUILayout.BeginArea(new Rect(x, y, width, height), cardStyle);
            GUILayout.BeginHorizontal();

            GUILayout.Label("<b>Arenas:</b>", labelStyle, GUILayout.Width(60));

            if (GUILayout.Button("[F1] Map 1 (Forest)", buttonStyle)) gm.SwitchMap(0);
            if (GUILayout.Button("[F2] Map 2 (Fortress)", buttonStyle)) gm.SwitchMap(1);
            if (GUILayout.Button("[F3] Map 3 (Coastal)", buttonStyle)) gm.SwitchMap(2);

            Valmon.Network.ValmonNetworkManager net = Valmon.Network.ValmonNetworkManager.Instance;
            if (net != null)
            {
                if (!net.isConnected)
                {
                    if (GUILayout.Button("🌐 Host LAN", buttonStyle)) net.StartHost();
                    if (GUILayout.Button("🔗 Join LAN", buttonStyle)) net.ConnectToHost("127.0.0.1");
                }
                else
                {
                    GUILayout.Label($"<b>🟢 {net.currentRole}</b>", labelStyle);
                }
            }

            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private void DrawRoleSwitchOverlay()
        {
            float w = 460;
            float h = 140;
            float x = (Screen.width - w) / 2f;
            float y = (Screen.height - h) / 2f;

            GUI.Box(new Rect(x, y, w, h), "");
            GUILayout.BeginArea(new Rect(x + 20, y + 20, w - 40, h - 40));
            GUILayout.Label("<size=20><b>🔄 ROUND 1 COMPLETE!</b></size>", headerStyle);
            GUILayout.Space(10);
            GUILayout.Label("<b>Switching Roles:</b> Tamer ⇄ Warden", labelStyle);
            GUILayout.Label("Get ready for Round 2...", labelStyle);
            GUILayout.EndArea();
        }

        private void DrawResultsOverlay(GameManager gm)
        {
            float w = 520;
            float h = 260;
            float x = (Screen.width - w) / 2f;
            float y = (Screen.height - h) / 2f;

            GUI.Box(new Rect(x, y, w, h), "");
            GUILayout.BeginArea(new Rect(x + 20, y + 20, w - 40, h - 40));

            GUILayout.Label("<size=22><b>🏆 MATCH FINISHED!</b></size>", headerStyle);
            GUILayout.Space(12);

            string winner = (gm.tamerScore > gm.wardenScore) ? "TAMER WINS!" :
                            (gm.wardenScore > gm.tamerScore) ? "WARDEN WINS!" : "IT'S A TIE!";

            GUILayout.Label($"<size=18><b>{winner}</b></size>", titleStyle);
            GUILayout.Space(8);

            GUILayout.Label($"• Final Tamer Score: <b>{gm.tamerScore} pts</b>", labelStyle);
            GUILayout.Label($"• Final Warden Score: <b>{gm.wardenScore} pts</b>", labelStyle);
            GUILayout.Space(12);

            if (GUILayout.Button("🔄 Play Again / Restart Match", buttonStyle, GUILayout.Height(36)))
            {
                gm.RestartMatch();
            }

            GUILayout.EndArea();
        }
    }
}
