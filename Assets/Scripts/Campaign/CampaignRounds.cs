using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Valmon.Campaign
{
    public partial class CampaignController
    {
        private const int MatchRounds=3;
        private int roundIndex, firstArena, hostWins, clientWins, hostScore, clientScore;
        private int roundSabotages, roundStopped, roundTamerScore, roundWardenScore, nextTrapId;
        private bool roundScored, waitingForArena, autoStartArmed=true, hostMatchWinner;
        private float countdownDeadline, countdownLeft, wardenVertical, wardenCorruptCooldown, trapCooldown, gateCooldown, scanCooldown, scanSeconds, dashCooldown, dashSeconds, slowSeconds;
        private int tamerHealth=100;
        private readonly float[] gateSeconds=new float[3];
        private readonly List<TrapStateData> traps=new List<TrapStateData>();
        private string playerName="Player", remotePlayerName="", hostPlayerName="", clientPlayerName="";
        [Serializable] private sealed class TrapStateData { public int id; public Vector3 point; public float seconds; public bool active=true; }
        private bool IsRoundMatch=>netMode!=NetMode.None&&netMatchStarted;
        private static string RoleName(PlayerRole role)=>role==PlayerRole.Tamer?"TAMER":"WARDEN";
        private void BeginMultiplayerMatch()
        {
            netMatchSeed=UnityEngine.Random.Range(1,int.MaxValue);
            roundIndex=0; hostWins=clientWins=hostScore=clientScore=0; firstArena=selectedLevel;
            netHostRole=UnityEngine.Random.Range(0,2)==0?PlayerRole.Tamer:PlayerRole.Corruptor;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if(netSmokeEnabled) netHostRole=System.Environment.GetCommandLineArgs().Contains("-valomonReverseRoles")?PlayerRole.Corruptor:PlayerRole.Tamer;
#endif
            hostPlayerName=playerName; clientPlayerName=remotePlayerName;
            SetupMultiplayerRound();
        }
        private void SetupMultiplayerRound()
        {
            Role=netHostRole; roundScored=false; roundSabotages=roundStopped=roundTamerScore=roundWardenScore=0;
            int localChoice=netLocalCharacter;
            int tamerCharacter=Role==PlayerRole.Tamer?localChoice:netRemoteCharacter;
            netCorruptorCharacter=Role==PlayerRole.Corruptor?localChoice:netRemoteCharacter;
            tamerHealth=100; wardenVertical=0; wardenCorruptCooldown=trapCooldown=gateCooldown=scanCooldown=scanSeconds=dashCooldown=dashSeconds=slowSeconds=0;
            traps.Clear(); Array.Clear(gateSeconds,0,gateSeconds.Length);
            StartCampaign((firstArena+roundIndex)%3,tamerCharacter,Role,starterIds[selectedStarter]);
            ScreenState=CampaignScreen.Countdown; waitingForArena=true; countdownDeadline=0; countdownLeft=3;
            netStatus="Loading arena on both laptops..."; netSendClock=1;
        }
        private void RoundNetworkTick()
        {
            if(netMode!=NetMode.Host||!netMatchStarted) return;
            if(ScreenState==CampaignScreen.Countdown&&!waitingForArena)
            {
                countdownLeft=Mathf.Max(0,countdownDeadline-Time.unscaledTime);
                if(countdownLeft<=0) { ScreenState=CampaignScreen.Explore; netStatus="Round "+(roundIndex+1)+" live / UDP linked"; }
            }
        }
        private void FinishRound(bool tamerWon,string reason)
        {
            if(roundScored||netMode!=NetMode.Host) return;
            roundScored=true; outcomeTamer=tamerWon; resultReason=reason; RemoveCaptureVisual(); paused=false;
            roundTamerScore=Run.catches*100+(tamerWon?200+Mathf.FloorToInt(Run.seconds/10):0);
            roundWardenScore=roundSabotages*50+roundStopped*100+(!tamerWon?200:0);
            bool hostWon=tamerWon==(Role==PlayerRole.Tamer);
            if(hostWon) hostWins++; else clientWins++;
            hostScore+=Role==PlayerRole.Tamer?roundTamerScore:roundWardenScore;
            clientScore+=Role==PlayerRole.Tamer?roundWardenScore:roundTamerScore;
            hostMatchWinner=hostWins>clientWins;
            ScreenState=roundIndex==MatchRounds-1?CampaignScreen.MatchResult:CampaignScreen.RoundResult;
            Cue(tamerWon?1.6f:.6f);
        }
        private void NextRound()
        {
            if(netMode==NetMode.Client) { NetCommand("next"); return; }
            if(ScreenState!=CampaignScreen.RoundResult) return;
            roundIndex++; netHostRole=netHostRole==PlayerRole.Tamer?PlayerRole.Corruptor:PlayerRole.Tamer;
            SetupMultiplayerRound();
        }
        private void PlayAgain()
        {
            if(netMode==NetMode.Client) { NetCommand("again"); return; }
            if(ScreenState==CampaignScreen.MatchResult) BeginMultiplayerMatch();
        }
        private void ReturnToLobby()
        {
            if(netMode==NetMode.Client) { NetCommand("lobby"); return; }
            NetSend(new Wire{type="lobbyReset"}); ResetToLobby();
        }
        private void ResetToLobby()
        {
            netMatchStarted=false; netAppliedStage=-1; netAppliedRevision=0; netRevision=0; autoStartArmed=false;
            Run=null; paused=false; ScreenState=CampaignScreen.Lobby; World.ShowMapMenu(selectedLevel);
            netStatus="Both players connected. Start another match when ready.";
        }
        private void TickAbilities()
        {
            float dt=Time.deltaTime;
            wardenCorruptCooldown=Mathf.Max(0,wardenCorruptCooldown-dt); trapCooldown=Mathf.Max(0,trapCooldown-dt); gateCooldown=Mathf.Max(0,gateCooldown-dt);
            scanCooldown=Mathf.Max(0,scanCooldown-dt); scanSeconds=Mathf.Max(0,scanSeconds-dt); dashCooldown=Mathf.Max(0,dashCooldown-dt); dashSeconds=Mathf.Max(0,dashSeconds-dt); slowSeconds=Mathf.Max(0,slowSeconds-dt);
            for(int i=0;i<gateSeconds.Length;i++) { gateSeconds[i]=Mathf.Max(0,gateSeconds[i]-dt); World.GateState(i,gateSeconds[i]>0); }
            foreach(TrapStateData trap in traps)
            {
                if(!trap.active) continue;
                trap.seconds-=dt;
                if(trap.seconds<=0) { trap.active=false; World.TrapState(trap.id,trap.point,false); continue; }
                if((ScreenState==CampaignScreen.Explore||ScreenState==CampaignScreen.Capture)&&Vector3.Distance(World.tamer.transform.position,trap.point)<1.4f)
                {
                    trap.active=false; World.TrapState(trap.id,trap.point,false); slowSeconds=4; tamerHealth=Mathf.Max(0,tamerHealth-25); roundSabotages++;
                    message="Warden trap! Slowed for four seconds; 25 health lost.";
                    if(ScreenState==CampaignScreen.Capture) { RemoveCaptureVisual(); encounter=null; ScreenState=CampaignScreen.Explore; roundStopped++; message="Capture interrupted by a Warden trap! The ball was spent."; }
                    World.SpawnRiftBurst(trap.point,2);
                    if(tamerHealth==0&&IsRoundMatch) FinishRound(false,"The Tamer was defeated by Warden hazards.");
                }
            }
            if(netMode!=NetMode.Client) traps.RemoveAll(t=>!t.active);
        }
        private bool UseAbility(string action)
        {
            if(netMode==NetMode.Client) return NetCommand(action);
            return UseAbilityAs(action,Role);
        }
        private bool UseAbilityAs(string action,PlayerRole actor)
        {
            if(paused||(ScreenState!=CampaignScreen.Explore&&ScreenState!=CampaignScreen.Capture)) return false;
            if(actor==PlayerRole.Tamer)
            {
                if(action!="dash"||dashCooldown>0||ScreenState!=CampaignScreen.Explore) return false;
                dashCooldown=7; dashSeconds=1; message="Rift dash!"; return true;
            }
            if(World.warden==null) return false;
            Vector3 at=World.warden.transform.position;
            if(action=="trap"&&trapCooldown<=0&&corruptionEnergy>=30)
            {
                if(traps.Count>=5) { message="Five traps are already active."; return false; }
                corruptionEnergy-=30; trapCooldown=10;
                var trap=new TrapStateData{id=++nextTrapId,point=at,seconds=25}; traps.Add(trap); World.TrapState(trap.id,trap.point,true);
                message="Trap armed for 25 seconds."; return true;
            }
            if(action=="gate"&&gateCooldown<=0&&corruptionEnergy>=35)
            {
                int closest=-1; float distance=8;
                for(int i=0;i<World.gates.Count;i++) { float d=Vector3.Distance(at,World.gates[i].transform.position); if(d<distance) { closest=i; distance=d; } }
                if(closest<0) { message="Move within 8m of a rift gate."; return false; }
                corruptionEnergy-=35; gateCooldown=12; gateSeconds[closest]=8; World.GateState(closest,true); roundSabotages++; message="Gate locked for eight seconds."; return true;
            }
            if(action=="scan"&&scanCooldown<=0&&corruptionEnergy>=15) { corruptionEnergy-=15; scanCooldown=15; scanSeconds=6; message="Scan active. Tamer and creatures marked on your radar."; return true; }
            if(action=="corruptNearest")
            {
                WildCreature nearest=World.wild.Where(w=>!w.captured&&!w.corrupted).OrderBy(w=>Vector3.Distance(at,w.root.transform.position)).FirstOrDefault();
                return nearest!=null&&TryCorrupt(nearest);
            }
            return false;
        }
        private void ReadAbilityKeys()
        {
            if(Keyboard.current==null||paused) return;
            if(Role==PlayerRole.Tamer) { if(Keyboard.current.qKey.wasPressedThisFrame) UseAbility("dash"); return; }
            if(Keyboard.current.qKey.wasPressedThisFrame) UseAbility("corruptNearest");
            if(Keyboard.current.eKey.wasPressedThisFrame) UseAbility("trap");
            if(Keyboard.current.rKey.wasPressedThisFrame) UseAbility("gate");
            if(Keyboard.current.fKey.wasPressedThisFrame) UseAbility("scan");
        }
        private float TamerSpeed(bool sprint)=>(dashSeconds>0?13:sprint?7.5f:5)*(slowSeconds>0?.45f:1);
    }
}
