#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Valmon.Campaign
{
    public partial class CampaignController
    {
        private bool netSmokeEnabled;
        private string netSmokeRole, netSmokePhase="", netSmokeRemotePhase="", netSmokeFolder;
        private int netSmokeTarget, netSmokeRemoteTurns;
        private string netSmokeLastAck="";
        private readonly List<string> netSmokeResults=new List<string>();
        private void StartNetworkSmokeIfRequested()
        {
            string[] args=System.Environment.GetCommandLineArgs(); int i=Array.IndexOf(args,"-valomonNetSmoke");
            if(i<0||i+1>=args.Length) return;
            netSmokeEnabled=true; netSmokeRole=args[i+1]; int report=Array.IndexOf(args,"-valmonReport");
            netSmokeFolder=report>=0?args[report+1]:Path.Combine(Application.persistentDataPath,"NetworkSmoke");
            smokeFolder=netSmokeFolder; Directory.CreateDirectory(netSmokeFolder); Application.logMessageReceived+=SmokeLog;
            StartCoroutine(NetworkSmokeRunner());
        }
        private IEnumerator NetworkSmokeRunner()
        {
            var stack=new Stack<IEnumerator>(); stack.Push(NetworkSmokeFlow()); string error=null;
            while(stack.Count>0)
            {
                bool next=false; object value=null;
                try { next=stack.Peek().MoveNext(); if(next) value=stack.Peek().Current; }
                catch(Exception e) { error=e.ToString(); break; }
                if(!next) { stack.Pop(); continue; }
                if(value is IEnumerator routine) stack.Push(routine); else yield return value;
            }
            Application.logMessageReceived-=SmokeLog;
            if(smokeErrors.Count>0) error=(error??"")+string.Join("\n",smokeErrors);
            netSmokeResults.Add(error==null?"PASS UDP THREE-ROUND MATCH":"FAIL "+error);
            File.WriteAllLines(Path.Combine(netSmokeFolder,"udp-"+netSmokeRole+".txt"),netSmokeResults);
            Debug.Log("[VALMON NET TEST] "+netSmokeRole+" "+(error==null?"PASS":error));
            yield return null;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.Exit(error==null?0:1);
#else
            Application.Quit(error==null?0:1);
#endif
        }
        private void NetSmokeBattleTick()
        {
            if(ScreenState==CampaignScreen.ChoosePartner&&Role==PlayerRole.Tamer) ChoosePartner(0);
            if(ScreenState==CampaignScreen.Battle&&HumanTurn&&turnDelay<=0&&!paused)
            {
                OwnedCreature actor=tamerTurn?Run.party[partyIndex]:opponent;
                OwnedCreature target=tamerTurn?opponent:Run.party[partyIndex];
                PlayMove(BattleRules.BestMove(actor,target,new System.Random(17)));
            }
        }
        private IEnumerator NetWait(Func<bool> predicate,string description,float seconds=30)
        {
            float until=Time.realtimeSinceStartup+seconds;
            while(!predicate()&&Time.realtimeSinceStartup<until) { NetSmokeBattleTick(); yield return null; }
            if(!predicate()) throw new Exception(description+" / "+netStatus+" / "+ScreenState+" / "+netSmokePhase);
            netSmokeResults.Add("PASS "+description);
        }
        private void NetSmokeSignal(string phase,int id=0)
        {
            netSmokePhase=phase; netSmokeRemotePhase=""; NetSend(new Wire{type="test",action=phase,id=id});
        }
        private void NetSmokeReceive(Wire wire)
        {
            if(!netSmokeEnabled) return;
            if(netMode==NetMode.Client) { netSmokePhase=wire.action; netSmokeTarget=wire.id; }
            else netSmokeRemotePhase=wire.action;
        }
        private void NetSmokeClientTick()
        {
            if(Run==null)
            {
                if(ScreenState==CampaignScreen.Lobby&&netSmokePhase=="lobbycheck") { NetSend(new Wire{type="test",action="lobby-ready"}); netSmokePhase="waiting-done"; }
                return;
            }
            if(netSmokePhase=="restartcheck"&&ScreenState==CampaignScreen.Explore&&roundIndex==0) { NetSend(new Wire{type="test",action="restart-ready"}); netSmokePhase="restart-seen"; }
            if(netSmokePhase=="catch"&&Role==PlayerRole.Tamer&&ScreenState==CampaignScreen.Explore)
            { if(BeginCapture(World.wild[netSmokeTarget])) netSmokePhase="catch-sent"; }
            if(netSmokePhase=="corrupt"&&Role==PlayerRole.Corruptor&&(ScreenState==CampaignScreen.Explore||ScreenState==CampaignScreen.Capture))
            { if(TryCorrupt(World.wild[netSmokeTarget])) netSmokePhase="corrupt-sent"; }
            if(netSmokePhase.StartsWith("ability:")&&ScreenState==CampaignScreen.Explore)
            { if(UseAbility(netSmokePhase.Substring(8))) netSmokePhase="ability-sent"; }
            if(netSmokePhase=="next"&&ScreenState==CampaignScreen.RoundResult)
            { NextRound(); netSmokePhase="next-sent"; }
            NetSmokeBattleTick();
            string ack=ScreenState==CampaignScreen.RoundResult||ScreenState==CampaignScreen.MatchResult?
                "result-"+roundIndex+"-"+hostWins+"-"+clientWins+"-"+hostScore+"-"+clientScore:"";
            if(ack!=""&&ack!=netSmokeLastAck) { netSmokeLastAck=ack; NetSend(new Wire{type="test",action=ack}); }
        }
        private void SmokePlace(GameObject actor,CharacterController motor,Vector3 position)
        { motor.enabled=false; actor.transform.position=position; motor.enabled=true; Physics.SyncTransforms(); }
        private IEnumerator NetworkSmokeFlow()
        {
            string[] args=System.Environment.GetCommandLineArgs(); int p=Array.IndexOf(args,"-valomonTestPort"); netPort=p>=0?int.Parse(args[p+1]):17888;
            netRoomCode="424242"; OpenMenu(); yield return SmokeShot(netSmokeRole+"-menu");
            EnterLobby(); yield return NetWait(()=>!capturingArenaPreviews,"Actual arena previews and four character portraits prepared",60);
            yield return SmokeShot(netSmokeRole+"-lobby"); selectedCharacter=netSmokeRole=="host"?1:2; playerName=netSmokeRole=="host"?"Laptop One":"Laptop Two";
            if(netSmokeRole=="client")
            {
                NetJoin("127.0.0.1"); netTransport.TestDropEvery=11;
                yield return NetWait(()=>Run!=null&&netMatchStarted,"Real UDP handshake and arena loaded",60);
                int lastRound=-1; bool battle=false,finalSeen=false; float deadline=Time.realtimeSinceStartup+210;
                while(Time.realtimeSinceStartup<deadline&&netSmokePhase!="done")
                {
                    if(roundIndex!=lastRound)
                    {
                        if(lastRound>=0&&roundIndex==lastRound+1&&Role==(args.Contains("-valomonReverseRoles")?(lastRound%2==0?PlayerRole.Tamer:PlayerRole.Corruptor):(lastRound%2==0?PlayerRole.Corruptor:PlayerRole.Tamer))) throw new Exception("Roles did not swap");
                        lastRound=roundIndex; netSmokeResults.Add("PASS round "+(lastRound+1)+" role "+RoleName(Role)+" / arena "+(Run.stage+1));
                    }
                    if(ScreenState==CampaignScreen.Battle&&!battle) { battle=true; netSmokeResults.Add("PASS player-controlled battle, health and PP replicated"); }
                    if(ScreenState==CampaignScreen.MatchResult) finalSeen=true;
                    NetSmokeClientTick(); yield return null;
                }
                if(netSmokePhase!="done"||!battle||!finalSeen) throw new Exception("Client did not observe complete match: "+ScreenState+" / "+netSmokePhase);
                yield break;
            }
            NetHost(); netTransport.TestDropEvery=11;
            yield return NetWait(()=>netMatchStarted&&Run!=null,"Two players automatically start authoritative match",60);
            PlayerRole previous=Role;
            for(int round=0;round<3;round++)
            {
                yield return NetWait(()=>ScreenState==CampaignScreen.Countdown&&!waitingForArena,"Both arenas acknowledge loading before countdown");
                float started=Time.realtimeSinceStartup;
                yield return SmokeShot("host-roles-"+round);
                yield return NetWait(()=>ScreenState==CampaignScreen.Explore,"Synchronized three-second countdown completes");
                if(Time.realtimeSinceStartup-started<2.2f) throw new Exception("Countdown was shortened");
                if(round>0&&Role==previous) throw new Exception("Host roles did not swap"); previous=Role;
                if(Run.stage!=round||World.warden==null||!World.wardenController.enabled||World.gates.Count!=3) throw new Exception("Arena or physical Warden missing");
                netSmokeResults.Add("PASS map "+(round+1)+" with both 3D players, three gates, role "+RoleName(Role));
                yield return SmokeShot("host-gameplay-"+round);
                if(round==0)
                {
                    Vector3 gate=World.gates[0].transform.position;
                    SmokePlace(World.warden,World.wardenController,gate+Vector3.back*2);
                    yield return new WaitForSeconds(.4f);
                    if(Role==PlayerRole.Corruptor) UseAbility("gate"); else NetSmokeSignal("ability:gate");
                    yield return NetWait(()=>gateSeconds[0]>0,"Warden locks an actual synchronized gate");
                    if(Role==PlayerRole.Corruptor) UseAbility("scan"); else { yield return new WaitForSeconds(.2f); NetSmokeSignal("ability:scan"); }
                    yield return NetWait(()=>scanSeconds>0,"Warden scan spends energy and enters cooldown");
                    if(UseAbilityAs("scan",PlayerRole.Corruptor)) throw new Exception("Cooldown allowed scan spam");
                    corruptionEnergy=100;
                    if(Role==PlayerRole.Corruptor) UseAbility("trap"); else { yield return new WaitForSeconds(.2f); NetSmokeSignal("ability:trap"); }
                    yield return NetWait(()=>traps.Count>0,"Warden places a synchronized trap");
                    SmokePlace(World.tamer,World.controller,traps[0].point);
                    yield return NetWait(()=>tamerHealth==75&&slowSeconds>0,"Trap collision damages and slows authoritative Tamer");
                    yield return new WaitForSeconds(.4f); yield return SmokeShot("host-trap-feedback");
                }
                for(int capture=0;capture<CampaignRun.RequiredCatches;capture++)
                {
                    int target=World.wild.FindIndex(w=>!w.captured&&!w.corrupted); WildCreature wild=World.wild[target];
                    SmokePlace(World.tamer,World.controller,wild.root.transform.position+Vector3.back*2);
                    SmokePlace(World.warden,World.wardenController,wild.root.transform.position+Vector3.right*3);
                    yield return new WaitForSeconds(.4f);
                    if(Role==PlayerRole.Tamer) BeginCapture(wild); else NetSmokeSignal("catch",target);
                    yield return NetWait(()=>ScreenState==CampaignScreen.Capture,"Tamer spends one ball and starts catch");
                    if(capture==1)
                    {
                        corruptionEnergy=100; wardenCorruptCooldown=0;
                        if(Role==PlayerRole.Corruptor) TryCorrupt(wild); else NetSmokeSignal("corrupt",target);
                        yield return NetWait(()=>wild.corrupted,"Warden corruption interrupts active capture");
                        yield return NetWait(()=>ScreenState==CampaignScreen.Battle,"Partner selection leads to synchronized battle");
                        Run.party[partyIndex].level=50; Run.party[partyIndex].Heal(); opponent.health=1;
                        tamerTurn=Role!=PlayerRole.Tamer; turnDelay=0;
                    }
                    yield return NetWait(()=>Run.catches==capture+1,"Shared capture "+(capture+1)+" / round "+(round+1));
                    if(Run.balls!=9-capture) throw new Exception("Duplicate ball spending under packet loss");
                }
                FinishCaptureGoal();
                string expected="result-"+roundIndex+"-"+hostWins+"-"+clientWins+"-"+hostScore+"-"+clientScore;
                yield return NetWait(()=>netSmokeRemotePhase==expected,"Both clients agree on round winner and scores");
                yield return SmokeShot("host-result-"+round);
                if(round<2)
                {
                    if(round==0) NextRound(); else NetSmokeSignal("next");
                    yield return NetWait(()=>roundIndex==round+1,"Next round accepted from "+(round==0?"host":"client"));
                }
            }
            if(ScreenState!=CampaignScreen.MatchResult||hostWins+clientWins!=3||netSmokeRemoteTurns==0) throw new Exception("Match results or remote battle decisions missing");
            netSmokeResults.Add("PASS final match results, three rounds, role swap, remote battle decisions");
            NetSmokeSignal("restartcheck"); PlayAgain();
            yield return NetWait(()=>ScreenState==CampaignScreen.Explore&&netSmokeRemotePhase=="restart-ready","Play Again reloads both arenas and resets match scores");
            if(hostWins!=0||clientWins!=0||Run.catches!=0||Run.balls!=10) throw new Exception("Play Again retained previous scores or catches");
            NetSmokeSignal("lobbycheck"); ReturnToLobby();
            yield return NetWait(()=>ScreenState==CampaignScreen.Lobby&&netSmokeRemotePhase=="lobby-ready","Return to Lobby resets both clients while retaining UDP room");
            NetSmokeSignal("done"); yield return new WaitForSeconds(1);
        }
    }
}
#endif
