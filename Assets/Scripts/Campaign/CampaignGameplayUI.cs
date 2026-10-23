using System;
using System.Linq;
using UnityEngine;

namespace Valmon.Campaign
{
    public partial class CampaignController
    {
        private void DrawRoleStatus()
        {
            Color roleColor = Role == PlayerRole.Tamer ? new Color(.12f, .7f, 1f) : new Color(1f, .2f, .25f);
            Panel(new Rect(20, 48, 125, 175), ink);
            Panel(new Rect(20, 48, 125, 2), roleColor);
            Text(26, 54, 113, 20, RoleName(Role), new GUIStyle(headingStyle) { fontSize = 13 });
            Text(26, 73, 113, 14, IsRoundMatch ? "RND " + (roundIndex + 1) + "/3" : "SOLO", new GUIStyle(smallStyle) { fontSize = 10 });
            Bar(26, 90, 113, Role == PlayerRole.Tamer ? tamerHealth / 100f : corruptionEnergy / 100, roleColor);
            Text(26, 101, 113, 15, Role == PlayerRole.Tamer ? "HP " + tamerHealth + "/100" : "NRG " + Mathf.FloorToInt(corruptionEnergy) + "/100", new GUIStyle(smallStyle) { fontSize = 10 });
            Text(26, 118, 113, 15, "OBJECTIVE", new GUIStyle(textStyle) { fontSize = 10 });
            string objText = Role == PlayerRole.Tamer
                ? "Catch: " + Run.catches + "/3" + (slowSeconds > 0 ? "\nSlow: " + Mathf.CeilToInt(slowSeconds) + "s" : "\nParty: " + Run.party.Count)
                : "Sabotages: " + roundSabotages + "\nStopped: " + roundStopped;
            Text(26, 134, 113, 80, objText, new GUIStyle(smallStyle) { fontSize = 10 });
            DrawRadar();
        }
        private void DrawRadar()
        {
            Rect radar = new Rect(1130, 48, 130, 130);
            Panel(radar, new Color(.015f, .035f, .065f, .96f));
            for (int i = 1; i < 4; i++)
            {
                Panel(new Rect(radar.x + i * radar.width / 4, radar.y, 1, radar.height), new Color(.12f, .24f, .3f));
                Panel(new Rect(radar.x, radar.y + i * radar.height / 4, radar.width, 1), new Color(.12f, .24f, .3f));
            }
            Text(radar.x + radar.width / 2 - 6, radar.y + 2, 20, 15, "N", new GUIStyle(smallStyle) { fontSize = 10 });
            GameObject own = Role == PlayerRole.Tamer ? World.tamer : World.warden;
            foreach (WildCreature wild in World.wild)
                if (!wild.captured && (scanSeconds > 0 || Vector3.Distance(own.transform.position, wild.root.transform.position) < 24))
                    RadarDot(radar, wild.root.transform.position, wild.corrupted ? new Color(1, .2f, .25f) : new Color(.3f, 1, .6f), 3);
            if (World.warden != null && (Role == PlayerRole.Corruptor || Vector3.Distance(own.transform.position, World.warden.transform.position) < 15))
                RadarDot(radar, World.warden.transform.position, new Color(1, .2f, .25f), 6);
            if (Role == PlayerRole.Tamer || scanSeconds > 0 || Vector3.Distance(own.transform.position, World.tamer.transform.position) < 24)
                RadarDot(radar, World.tamer.transform.position, new Color(.1f, .7f, 1), 6);
            foreach (GameObject gate in World.gates)
                if (gate != null) RadarDot(radar, gate.transform.position, new Color(1, .7f, .1f), 2.5f);
            Text(radar.x, radar.y + radar.height + 2, radar.width, 16, scanSeconds > 0 ? "SCAN ACTIVE" : "RADAR", new GUIStyle(smallStyle) { fontSize = 10, alignment = TextAnchor.MiddleCenter });
        }
        private void RadarDot(Rect radar, Vector3 position, Color color, float size)
        {
            float x = radar.x + Mathf.Clamp01((position.x + 46) / 92) * radar.width;
            float y = radar.y + (1 - Mathf.Clamp01((position.z + 46) / 92)) * radar.height;
            Panel(new Rect(x - size / 2, y - size / 2, size, size), color);
        }
        private void DrawWardenAbilities()
        {
            string[] actions={"corruptNearest","trap","gate","scan"};
            string[] names={"Q / CORRUPT CREATURE","E / PLACE TRAP","R / LOCK GATE","F / SCAN"};
            int[] cost={25,30,35,15}; float[] cooldown={wardenCorruptCooldown,trapCooldown,gateCooldown,scanCooldown};
            for(int i=0;i<4;i++)
            {
                bool can=corruptionEnergy>=cost[i]&&cooldown[i]<=0&&(ScreenState==CampaignScreen.Explore||ScreenState==CampaignScreen.Capture);
                string text=names[i]+"\n"+(cooldown[i]>0?Mathf.CeilToInt(cooldown[i])+"s cooldown":cost[i]+" energy");
                if(Button(new Rect(960,334+i*53,285,48),text,can,false,true)) UseAbility(actions[i]);
            }
        }
    }
}
