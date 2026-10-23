using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Valmon.Campaign
{
    public partial class CampaignController
    {
        public enum NetMode { None, Host, Client }
        public NetMode netMode = NetMode.None;
        public bool netConnected;
        public int netPort = 7788;
        public string netAddress = "", netStatus = "Connect both laptops to the same network.", netLocalIp = "", netRoomCode = "";
        private LanUdpTransport netTransport;
        private string netSession = "", netCatalog, netBattleVisualKey = "";
        private float netLastSeen, netSendClock, netHelloClock, netStartedAt, netMoveSeen, netMoveYaw;
        private bool netMatchStarted, netRemoteReady, netSprint;
        private int netMatchSeed, netStageSeed, netRemoteCharacter, netCorruptorCharacter, netBattleSerial, netAppliedStage = -1, netAppliedSeed;
        private int netClientCommand, netLastCommand;
        private int netLocalCharacter;
        private int netTurnSerial;
        private float netNextCommandAt;
        private int netSentBattleTurn = -1;
        private long netRevision, netAppliedRevision;
        private Vector2 netMove;
        private PlayerRole netHostRole = PlayerRole.Tamer;
        private PlayerRole RemoteRole => Role == PlayerRole.Tamer ? PlayerRole.Corruptor : PlayerRole.Tamer;

        [Serializable] private sealed class CreatureState
        {
            public string species, status;
            public int level, health, attack, defense, accuracy, sleep;
            public int[] pp;
            public CreatureState() { }
            public CreatureState(OwnedCreature c) { species = c.species.id; level = c.level; health = c.health; pp = c.pp; status = c.status; attack = c.attackStage; defense = c.defenseStage; accuracy = c.accuracyStage; sleep = c.sleepTurns; }
            public OwnedCreature Read(CampaignCatalog data)
            {
                SpeciesDefinition definition = data.Find(species);
                if (definition == null) throw new ArgumentException("Snapshot contains unknown creature: " + species);
                var c = new OwnedCreature(definition, level);
                c.health = health; c.pp = pp; c.status = status ?? ""; c.attackStage = attack; c.defenseStage = defense; c.accuracyStage = accuracy; c.sleepTurns = sleep; return c;
            }
        }
        [Serializable] private sealed class WildState
        {
            public CreatureState creature;
            public Vector3 position;
            public bool corrupted, captured;
        }
        [Serializable] private sealed class MatchState
        {
            public long revision;
            public int stage, stageSeed, screen, balls, catches, partyIndex, encounterId, battleSerial, turnSerial, character, corruptorCharacter;
            public float seconds, energy, captureClock, turnDelay, animationClock;
            public bool paused, final, tamerTurn, outcomeTamer, attackByTamer;
            public Vector3 position;
            public float rotation;
            public string message, reason;
            public CreatureState[] party, bosses;
            public CreatureState opponent;
            public WildState[] wild;
            public int round, hostRole, hostWins, clientWins, hostScore, clientScore, tamerHealth, sabotages, stopped, tamerScore, wardenScore, opponentBoss;
            public Vector3 wardenPosition;
            public float wardenRotation, countdown, corruptCooldown, trapCooldown, gateCooldown, scanCooldown, scanSeconds, dashCooldown, dashSeconds, slowSeconds;
            public bool waitingForArena, hostWinner;
            public float[] gates;
            public TrapStateData[] traps;
            public string hostName, clientName;
        }
        [Serializable] private sealed class Wire
        {
            public string type, session, code, catalog, action, reason, name;
            public int character, role, seed, id, command, expectedScreen, expectedBattle, expectedTurn, expectedStage;
            public float x, z, yaw;
            public bool sprint;
            public Vector3 point;
            public MatchState state;
        }
        // JsonUtility expands a null nested serializable class into an empty object.
        // Keep HELLO and other control packets small enough for one UDP datagram.
        [Serializable] private sealed class ControlWire
        {
            public string type, session, code, catalog, action, reason, name;
            public int character, role, seed, id, command, expectedScreen, expectedBattle, expectedTurn, expectedStage;
            public float x,z,yaw;
            public bool sprint;
            public Vector3 point;
            public ControlWire(Wire w)
            {
                type=w.type; session=w.session; code=w.code; catalog=w.catalog; action=w.action; reason=w.reason; name=w.name;
                character=w.character; role=w.role; seed=w.seed; id=w.id; command=w.command; expectedScreen=w.expectedScreen;
                expectedBattle=w.expectedBattle; expectedTurn=w.expectedTurn; expectedStage=w.expectedStage;
                x=w.x; z=w.z; yaw=w.yaw; sprint=w.sprint; point=w.point;
            }
        }
        private string CatalogSignature()
        {
            if (netCatalog != null) return netCatalog;
            var text = new StringBuilder("VALMON-LAN-4-WALKING|");
            foreach (SpeciesDefinition s in catalog.species)
            {
                text.Append(s.id).Append(':').Append(s.hp).Append(':').Append(s.attack).Append(':').Append(s.defense).Append(':').Append(s.specialAttack).Append(':').Append(s.specialDefense).Append(':').Append(s.speed).Append(string.Join(",", s.types));
                foreach (MoveDefinition m in s.moves) text.Append(m.name).Append(m.type).Append(m.power).Append(m.accuracy).Append(m.pp).Append(m.effect);
            }
            text.Append(CampaignRun.RequiredCatches).Append(CampaignRun.BallsPerStage).Append(CampaignRun.SecondsPerStage);
            using (SHA256 hash = SHA256.Create()) netCatalog = BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(text.ToString()))).Replace("-", "");
            return netCatalog;
        }
        public void NetHost()
        {
            NetDisconnect();
            netLocalCharacter=selectedCharacter; autoStartArmed=true;
            try
            {
                netTransport = new LanUdpTransport(netPort); netMode = NetMode.Host;
                netSession = Guid.NewGuid().ToString("N");
                if (string.IsNullOrEmpty(netRoomCode)) netRoomCode = UnityEngine.Random.Range(100000, 999999).ToString();
                netLocalIp = string.Join("  /  ", GetLocalIps());
                netStatus = "Room open. Share the IP, port and room code with laptop 2.";
                netStartedAt = Time.unscaledTime;
            }
            catch (Exception e) { NetDisconnect(); netStatus = "Could not host: " + e.Message; }
        }
        public void NetJoin(string ip)
        {
            NetDisconnect(); netAddress = ip.Trim();
            netLocalCharacter=selectedCharacter;
            if (!IPAddress.TryParse(netAddress, out IPAddress address) || address.AddressFamily != AddressFamily.InterNetwork)
            { netStatus = "Enter the host's IPv4 address, for example 192.168.1.10."; return; }
            try
            {
                netTransport = new LanUdpTransport(0, new IPEndPoint(address, netPort)); netMode = NetMode.Client;
                netStatus = "Contacting host over UDP..."; netLastSeen = netStartedAt = Time.unscaledTime; netHelloClock = 1;
            }
            catch (Exception e) { NetDisconnect(); netStatus = "Could not join: " + e.Message; }
        }
        public void NetDisconnect()
        {
            netTransport?.Dispose(); netTransport = null;
            netConnected = netMatchStarted = netRemoteReady = false; netMode = NetMode.None;
            netAppliedStage = -1; netAppliedRevision = netRevision = 0; netClientCommand = netLastCommand = 0;
            netBattleVisualKey = ""; netMove = Vector2.zero;
            netSentBattleTurn = -1; netNextCommandAt = 0;
        }
        private static string[] GetLocalIps()
        {
            var result = new List<string>();
            foreach (NetworkInterface adapter in NetworkInterface.GetAllNetworkInterfaces().OrderByDescending(n => n.NetworkInterfaceType == NetworkInterfaceType.Wireless80211))
            {
                if (adapter.OperationalStatus != OperationalStatus.Up || adapter.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;
                foreach (UnicastIPAddressInformation a in adapter.GetIPProperties().UnicastAddresses)
                    if (a.Address.AddressFamily == AddressFamily.InterNetwork && !a.Address.ToString().StartsWith("169.254")) result.Add(a.Address.ToString());
            }
            return result.Count > 0 ? result.Distinct().ToArray() : new[] { "No LAN IPv4 found" };
        }
        private void NetSend(Wire wire, bool reliable = true, IPEndPoint endpoint = null)
        {
            wire.session = netSession;
            string json=wire.state==null?JsonUtility.ToJson(new ControlWire(wire)):JsonUtility.ToJson(wire);
            netTransport?.Send(json, reliable, endpoint);
        }
#if UNITY_EDITOR
        public static void ValidateControlPacketBudget()
        {
            var hello=new Wire{type="hello",session=new string('s',32),catalog=new string('c',64),code="424242",name=new string('n',24),character=3};
            string json=JsonUtility.ToJson(new ControlWire(hello));
            if(Encoding.UTF8.GetByteCount(json)>1100||JsonUtility.FromJson<Wire>(json).code!="424242") throw new InvalidOperationException("UDP lobby control packet exceeded its single-datagram budget.");
            Debug.Log("[VALMON UDP] Compact lobby control packet budget passed.");
        }
#endif
        private bool NetCommand(string action, int id = 0, Vector3 point = default)
        {
            if (!netConnected || !netMatchStarted) return false;
            if (Time.unscaledTime < netNextCommandAt) return false;
            if (action == "move" || action == "switch")
            {
                if (netSentBattleTurn == netTurnSerial) return false;
                netSentBattleTurn = netTurnSerial;
            }
            netNextCommandAt = Time.unscaledTime + .14f;
            NetSend(new Wire { type = "command", action = action, id = id, point = point, command = ++netClientCommand, expectedScreen = (int)ScreenState, expectedBattle = netBattleSerial, expectedTurn = netTurnSerial, expectedStage = Run.stage }); return true;
        }
        private void NetTick()
        {
            if (netTransport == null) return;
            netTransport.Tick();
            for (int count = 0; count < 256 && netTransport.TryReceive(out LanUdpTransport.Message packet); count++)
            {
                try { NetProcess(JsonUtility.FromJson<Wire>(packet.text), packet.from); }
                catch (Exception e) { Debug.LogWarning("[VALOMON UDP] Ignored invalid packet: " + e); }
            }
            float now = Time.unscaledTime;
            if(netMode==NetMode.Host&&netRemoteReady&&!netMatchStarted&&autoStartArmed) HostStartMatch();
            RoundNetworkTick();
            if (netMode == NetMode.Client && !netConnected)
            {
                netHelloClock += Time.unscaledDeltaTime;
                if (netHelloClock > .5f) { netHelloClock = 0; NetSend(new Wire { type = "hello", code = netRoomCode.Trim(), catalog = CatalogSignature(), character = selectedCharacter, name=playerName }, false); }
                if (now - netStartedAt > 20) { NetDisconnect(); netStatus = "No response. Check the host IP, room code, UDP port and Windows Firewall."; }
                return;
            }
            if (netConnected && (now - netLastSeen > 15 || netTransport.Error != null))
            {
                bool playing = netMatchStarted; string reason = netTransport.Error ?? "Connection to the other laptop was lost.";
                NetDisconnect(); netStatus = reason;
                if (playing) { ScreenState = CampaignScreen.Result; resultReason = reason + " Return to the lobby to start a new match."; paused = false; }
                return;
            }
            netSendClock += Time.unscaledDeltaTime;
            if (!netConnected || netSendClock < (netMatchStarted ? .05f : .5f)) return;
            netSendClock = 0;
            if (netMode == NetMode.Host)
            {
                if (netMatchStarted) NetSend(new Wire { type = "state", state = NetSnapshot() }, false);
                else NetSend(new Wire { type = "ping", id=selectedLevel, character=netLocalCharacter, name=playerName }, false);
            }
            else NetSend(new Wire { type = "input", x = netMove.x, z = netMove.y, yaw = Role==PlayerRole.Tamer?yaw:corruptorYaw, sprint = netSprint }, false);
        }
        private void NetProcess(Wire p, IPEndPoint from)
        {
            if (p == null || p.type == null) return;
            if (p.type == "hello" && netMode == NetMode.Host && !netConnected && !netMatchStarted)
            {
                if (p.code != netRoomCode || p.catalog != CatalogSignature() || p.character < 0 || p.character >= catalog.characters.Length) return;
                netTransport.Accept(from); netRemoteCharacter = p.character; remotePlayerName=CleanPlayerName(p.name); netConnected = true; netLastSeen = Time.unscaledTime;
                NetSend(new Wire { type = "welcome", catalog = CatalogSignature(), role = (int)netHostRole, name=playerName, character=netLocalCharacter, id=selectedLevel });
                netStatus = "Laptop 2 connected. Press START MATCH when ready."; return;
            }
            if (p.type == "welcome" && netMode == NetMode.Client && !netConnected)
            {
                if (p.catalog != CatalogSignature() || string.IsNullOrEmpty(p.session)) return;
                netSession = p.session; netConnected = true; netLastSeen = Time.unscaledTime;
                remotePlayerName=CleanPlayerName(p.name); netRemoteCharacter=p.character; selectedLevel=p.id;
                Role = (PlayerRole)p.role == PlayerRole.Tamer ? PlayerRole.Corruptor : PlayerRole.Tamer;
                NetSend(new Wire { type = "ready", character = selectedCharacter });
                netStatus = "Connected as " + Role.ToString().ToUpperInvariant() + ". Waiting for the host."; return;
            }
            if (!netConnected || p.session != netSession) return;
            netLastSeen = Time.unscaledTime;
            switch (p.type)
            {
                case "ready": if (netMode == NetMode.Host) netRemoteReady = true; break;
                case "ping": if (netMode == NetMode.Client) { selectedLevel=p.id; remotePlayerName=CleanPlayerName(p.name); netRemoteCharacter=p.character; NetSend(new Wire { type = "pong" }, false); } break;
                case "start":
                    if (netMode != NetMode.Client || netMatchStarted) break;
                    netMatchSeed = p.seed; netMatchStarted = true; ScreenState = CampaignScreen.Lobby; netStatus = "Loading shared arena..."; break;
                case "input":
                    if (netMode != NetMode.Host || !netMatchStarted) break;
                    if (float.IsNaN(p.x) || float.IsInfinity(p.x) || float.IsNaN(p.z) || float.IsInfinity(p.z) || float.IsNaN(p.yaw) || float.IsInfinity(p.yaw)) break;
                    netMove = Vector2.ClampMagnitude(new Vector2(p.x, p.z), 1); netMoveYaw = p.yaw % 360; netSprint = p.sprint; netMoveSeen = Time.unscaledTime; break;
                case "command":
                    if(netMode==NetMode.Host&&netMatchStarted) { int before=netTurnSerial; ApplyRemoteCommand(p); if(p.action=="move"||p.action=="switch") NetSend(new Wire{type="decision",id=p.expectedTurn,sprint=netTurnSerial!=before}); } break;
                case "decision": if(netMode==NetMode.Client&&!p.sprint&&netSentBattleTurn==p.id) netSentBattleTurn=-1; break;
                case "loaded": if(netMode==NetMode.Host&&waitingForArena&&p.id==roundIndex) { waitingForArena=false; countdownDeadline=Time.unscaledTime+3; } break;
                case "lobbyReset": if(netMode==NetMode.Client) ResetToLobby(); break;
                case "state": if (netMode == NetMode.Client && netMatchStarted && p.state != null) NetApplySnapshot(p.state); break;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                case "test": NetSmokeReceive(p); break;
#endif
            }
        }
        private void HostStartMatch()
        {
            if (netMode != NetMode.Host || !netConnected || !netRemoteReady || netMatchStarted) return;
            netMatchSeed = UnityEngine.Random.Range(1, int.MaxValue); netMatchStarted = true; netBattleSerial = 0;
            NetSend(new Wire { type = "start", seed = netMatchSeed });
            random = new System.Random(netMatchSeed); BeginMultiplayerMatch(); netSendClock = 1;
        }
        private void ApplyRemoteCommand(Wire p)
        {
            if (p.command <= netLastCommand) return; netLastCommand = p.command;
            bool intervention=p.action=="corrupt"||p.action=="burst"||p.action=="trap"||p.action=="gate"||p.action=="scan"||p.action=="corruptNearest";
            bool catchingBoundary=intervention&&(ScreenState==CampaignScreen.Explore||ScreenState==CampaignScreen.Capture)&&
                (p.expectedScreen==(int)CampaignScreen.Explore||p.expectedScreen==(int)CampaignScreen.Capture);
            if ((!catchingBoundary&&p.expectedScreen != (int)ScreenState) || p.expectedBattle != netBattleSerial) return;
            if (p.expectedStage != Run.stage || ((p.action == "move" || p.action == "switch") && p.expectedTurn != netTurnSerial)) return;
            PlayerRole remote = RemoteRole;
            if (p.action == "pause" && Active) { paused = !paused; return; }
            if (paused) return;
            switch (p.action)
            {
                case "next": NextRound(); break;
                case "again": PlayAgain(); break;
                case "lobby": ReturnToLobby(); break;
                case "dash": case "trap": case "gate": case "scan": case "corruptNearest": UseAbilityAs(p.action,remote); break;
                case "catch": if (remote == PlayerRole.Tamer && p.id >= 0 && p.id < World.wild.Count) BeginCapture(World.wild[p.id]); break;
                case "corrupt": if (remote == PlayerRole.Corruptor && p.id >= 0 && p.id < World.wild.Count) TryCorrupt(World.wild[p.id]); break;
                case "burst": if (remote == PlayerRole.Corruptor && IsFinite(p.point) && Mathf.Abs(p.point.x) <= 60 && Mathf.Abs(p.point.z) <= 60) TryRiftBurst(p.point); break;
                case "partner": if (remote == PlayerRole.Tamer) ChoosePartner(p.id); break;
                case "move":
                    if (ScreenState != CampaignScreen.Battle || turnDelay > 0 || tamerTurn != (remote == PlayerRole.Tamer)) break;
                    OwnedCreature actor = tamerTurn ? Run.party[partyIndex] : opponent;
                    if (BattleRules.HasMoves(actor) && (p.id < 0 || p.id >= actor.pp.Length || actor.pp[p.id] <= 0)) break;
                    ResolveTurn(p.id);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    if (netSmokeEnabled) netSmokeRemoteTurns++;
#endif
                    break;
                case "switch":
                    if (ScreenState == CampaignScreen.Battle && turnDelay <= 0 && tamerTurn == (remote == PlayerRole.Tamer)) SwitchPartyAs(p.id, remote); break;
                case "reset": if (remote == PlayerRole.Tamer && ScreenState == CampaignScreen.Explore) World.ResetPlayer(); break;
            }
        }
        private static bool IsFinite(Vector3 p) => !(float.IsNaN(p.x) || float.IsNaN(p.y) || float.IsNaN(p.z) || float.IsInfinity(p.x) || float.IsInfinity(p.y) || float.IsInfinity(p.z));
        private MatchState NetSnapshot() => new MatchState
        {
            revision = ++netRevision, stage = Run.stage, stageSeed = netStageSeed, screen = (int)ScreenState,
            balls = Run.balls, catches = Run.catches, seconds = Run.seconds, energy = corruptionEnergy,
            partyIndex = partyIndex, encounterId = encounter == null ? -1 : World.wild.IndexOf(encounter), battleSerial = netBattleSerial, turnSerial = netTurnSerial,
            character = selectedCharacter, corruptorCharacter = netCorruptorCharacter, paused = paused, final = finalBattle, tamerTurn = tamerTurn,
            outcomeTamer = outcomeTamer, captureClock = captureClock, turnDelay = turnDelay, animationClock = animationClock, attackByTamer = attackByTamer,
            position = World.tamer.transform.position, rotation = World.tamer.transform.eulerAngles.y, message = message, reason = resultReason,
            party = Run.party.Select(c => new CreatureState(c)).ToArray(), bosses = bossTeam.Select(c => new CreatureState(c)).ToArray(), opponent = opponent == null ? null : new CreatureState(opponent),
            wild = World.wild.Select(w => new WildState { creature = new CreatureState(w.creature), position = w.root.transform.position, corrupted = w.corrupted, captured = w.captured }).ToArray(),
            round=roundIndex, hostRole=(int)Role, hostWins=hostWins, clientWins=clientWins, hostScore=hostScore, clientScore=clientScore,
            tamerHealth=tamerHealth, sabotages=roundSabotages, stopped=roundStopped, tamerScore=roundTamerScore, wardenScore=roundWardenScore,
            opponentBoss=bossTeam.IndexOf(opponent), wardenPosition=World.warden.transform.position, wardenRotation=World.warden.transform.eulerAngles.y,
            countdown=countdownLeft, waitingForArena=waitingForArena, hostWinner=hostMatchWinner,
            corruptCooldown=wardenCorruptCooldown, trapCooldown=trapCooldown, gateCooldown=gateCooldown, scanCooldown=scanCooldown, scanSeconds=scanSeconds,
            dashCooldown=dashCooldown, dashSeconds=dashSeconds, slowSeconds=slowSeconds, gates=gateSeconds, traps=traps.ToArray(), hostName=hostPlayerName, clientName=clientPlayerName
        };
        private void NetApplySnapshot(MatchState s)
        {
            if (s.revision <= netAppliedRevision) return; netAppliedRevision = s.revision;
            bool load = s.stage != netAppliedStage || s.stageSeed != netAppliedSeed || Run == null;
            Role=(PlayerRole)s.hostRole==PlayerRole.Tamer?PlayerRole.Corruptor:PlayerRole.Tamer;
            if (load)
            {
                netAppliedStage = s.stage; netAppliedSeed = s.stageSeed;
                Run = new CampaignRun(); Run.party.Add(s.party.Length > 0 ? s.party[0].Read(catalog) : new OwnedCreature(catalog.species[0], 10)); Run.StartStage(s.stage);
                World.LoadStage(Run, s.character, new System.Random(s.stageSeed)); World.controller.enabled = false; World.ShowCorruptor(s.corruptorCharacter);
                World.wardenController.enabled=false;
                yaw = World.StartingYaw; pitch = 27; corruptorPosition = World.towerPosition; corruptorYaw = 0; corruptorPitch = 27;
                netBattleVisualKey = ""; UpdateCamera(true);
            }
            CampaignScreen previous = ScreenState;
            Run.stage = s.stage; Run.balls = s.balls; Run.catches = s.catches; Run.seconds = s.seconds;
            Run.party.Clear(); Run.party.AddRange(s.party.Select(c => c.Read(catalog)));
            bossTeam.Clear(); bossTeam.AddRange(s.bosses.Select(c => c.Read(catalog)));
            partyIndex = s.partyIndex; opponent = s.opponent == null || string.IsNullOrEmpty(s.opponent.species) ? null : s.opponent.Read(catalog); corruptionEnergy = s.energy;
            if(s.final&&s.opponentBoss>=0&&s.opponentBoss<bossTeam.Count) opponent=bossTeam[s.opponentBoss];
            if (paused != s.paused) netSentBattleTurn = -1;
            paused = s.paused; finalBattle = s.final; tamerTurn = s.tamerTurn; outcomeTamer = s.outcomeTamer;
            message = s.message; resultReason = s.reason; captureClock = s.captureClock; turnDelay = s.turnDelay;
            netBattleSerial = s.battleSerial; netTurnSerial = s.turnSerial; animationClock = s.animationClock; attackByTamer = s.attackByTamer;
            roundIndex=s.round; hostWins=s.hostWins; clientWins=s.clientWins; hostScore=s.hostScore; clientScore=s.clientScore; tamerHealth=s.tamerHealth;
            roundSabotages=s.sabotages; roundStopped=s.stopped; roundTamerScore=s.tamerScore; roundWardenScore=s.wardenScore;
            countdownLeft=s.countdown; waitingForArena=s.waitingForArena; hostMatchWinner=s.hostWinner; hostPlayerName=s.hostName; clientPlayerName=s.clientName;
            wardenCorruptCooldown=s.corruptCooldown; trapCooldown=s.trapCooldown; gateCooldown=s.gateCooldown; scanCooldown=s.scanCooldown; scanSeconds=s.scanSeconds; dashCooldown=s.dashCooldown; dashSeconds=s.dashSeconds; slowSeconds=s.slowSeconds;
            for(int i=0;i<3;i++) { gateSeconds[i]=s.gates[i]; World.GateState(i,gateSeconds[i]>0); }
            foreach(TrapStateData old in traps) if(!s.traps.Any(t=>t.id==old.id&&t.active)) World.TrapState(old.id,old.point,false);
            traps.Clear(); traps.AddRange(s.traps);
            foreach(TrapStateData trap in traps) World.TrapState(trap.id,trap.point,trap.active);
            for (int i = 0; i < s.wild.Length && i < World.wild.Count; i++)
            {
                WildCreature w = World.wild[i]; WildState state = s.wild[i];
                if (state.corrupted && !w.corrupted) World.Corrupt(w);
                w.creature = state.creature.Read(catalog); w.captured = state.captured;
                w.root.SetActive(!w.captured); w.root.transform.position = state.position;
            }
            encounter = s.encounterId < 0 ? null : World.wild[s.encounterId];
            World.tamer.transform.SetPositionAndRotation(s.position, Quaternion.Euler(0, s.rotation, 0)); ScreenState = (CampaignScreen)s.screen;
            World.warden.transform.SetPositionAndRotation(s.wardenPosition,Quaternion.Euler(0,s.wardenRotation,0));
            if (ScreenState == CampaignScreen.Battle && Run.party.Count > 0 && opponent != null)
            {
                string key = s.battleSerial + ":" + partyIndex + ":" + Run.party[partyIndex].species.id + ":" + opponent.species.id;
                if (key != netBattleVisualKey) { World.ShowBattle(Run.party[partyIndex], opponent); netBattleVisualKey = key; }
            }
            else if (previous == CampaignScreen.Battle || load) { World.ResumeMap(); netBattleVisualKey = ""; UpdateCamera(true); }
            if (ScreenState == CampaignScreen.Capture && encounter != null)
            {
                if (captureVisual == null) captureVisual = World.Model(catalog.captureBall, null, s.position + Vector3.up, .35f);
                captureVisual.transform.position = Vector3.Lerp(s.position + Vector3.up, encounter.root.transform.position + Vector3.up, Mathf.Clamp01(captureClock / 1.3f));
            }
            else RemoveCaptureVisual();
            if (ScreenState == CampaignScreen.StageClear) { unlocked = Mathf.Max(unlocked, Mathf.Min(2, Run.stage + 1)); PlayerPrefs.SetInt("VALMON.UnlockedStage", unlocked); }
            if(load) NetSend(new Wire{type="loaded",id=roundIndex});
        }
        private void NetClientFrame()
        {
            if(!netMatchStarted||Run==null||World.tamer==null) return;
            if(!paused&&(ScreenState==CampaignScreen.Explore||ScreenState==CampaignScreen.Capture||ScreenState==CampaignScreen.ChoosePartner))
            {
                netMove=ScreenState==CampaignScreen.Explore||(ScreenState==CampaignScreen.Capture&&Role==PlayerRole.Corruptor)?ReadMoveInput():Vector2.zero;
                netSprint=Keyboard.current!=null&&Keyboard.current.leftShiftKey.isPressed;
                if(Mouse.current!=null&&Mouse.current.rightButton.isPressed)
                {
                    Vector2 d=Mouse.current.delta.ReadValue();
                    if(Role==PlayerRole.Tamer) { yaw+=d.x*.16f; pitch=Mathf.Clamp(pitch-d.y*.12f,10,65); }
                    else { corruptorYaw+=d.x*.16f; corruptorPitch=Mathf.Clamp(corruptorPitch-d.y*.12f,10,65); }
                }
                if(Role==PlayerRole.Tamer)
                {
                    selected=NearestWild();
                    if(ScreenState==CampaignScreen.Explore&&Keyboard.current!=null)
                    {
                        if((Keyboard.current.zKey.wasPressedThisFrame || Keyboard.current.eKey.wasPressedThisFrame)&&selected!=null) BeginCapture(selected);
                        if(Keyboard.current.rKey.wasPressedThisFrame) NetCommand("reset");
                    }
                }
                ReadAbilityKeys(); World.TickAmbient(Time.deltaTime); UpdateCamera();
            }
            else netMove=Vector2.zero;
            if(ScreenState==CampaignScreen.Battle&&!paused) AnimateAttack();
            if(Keyboard.current!=null&&Keyboard.current.escapeKey.wasPressedThisFrame&&Active) NetCommand("pause");
            if(Role==PlayerRole.Corruptor&&!paused) ReadCorruptorClick();
        }        private static Vector2 ReadMoveInput()
        {
            Vector2 input = Vector2.zero;
            if (Keyboard.current != null) input = new Vector2((Keyboard.current.dKey.isPressed ? 1 : 0) - (Keyboard.current.aKey.isPressed ? 1 : 0), (Keyboard.current.wKey.isPressed ? 1 : 0) - (Keyboard.current.sKey.isPressed ? 1 : 0));
            if (Gamepad.current != null) input += Gamepad.current.leftStick.ReadValue(); return Vector2.ClampMagnitude(input, 1);
        }
        private void MoveRemoteTamer()
        {
            Vector2 input = Time.unscaledTime - netMoveSeen < .4f ? netMove : Vector2.zero;
            Vector3 direction = Quaternion.Euler(0, netMoveYaw, 0) * new Vector3(input.x, 0, input.y);
            if (World.controller.isGrounded && verticalSpeed < 0) verticalSpeed = -2;
            verticalSpeed -= 20 * Time.deltaTime;
            World.controller.Move((direction * TamerSpeed(netSprint) + Vector3.up * verticalSpeed) * Time.deltaTime);
            if (direction.sqrMagnitude > .01f) World.tamer.transform.rotation = Quaternion.LookRotation(direction);
            if (World.tamer.transform.position.y < -8) { World.ResetPlayer(); verticalSpeed = 0; }
        }
        private void NetSendCorrupt(int id) => NetCommand("corrupt", id);
        private void MoveRemoteWarden()
        {
            if(World.wardenController==null) return;
            Vector2 input=Time.unscaledTime-netMoveSeen<.4f?netMove:Vector2.zero;
            Vector3 direction=Quaternion.Euler(0,netMoveYaw,0)*new Vector3(input.x,0,input.y);
            if(World.wardenController.isGrounded&&wardenVertical<0) wardenVertical=-2;
            wardenVertical-=20*Time.deltaTime;
            World.wardenController.Move((direction*(netSprint?7:5)+Vector3.up*wardenVertical)*Time.deltaTime);
            if(direction.sqrMagnitude>.01f) World.warden.transform.rotation=Quaternion.LookRotation(direction);
            if(World.warden.transform.position.y < -8) { World.ResetWarden(); wardenVertical=0; }
        }
        private void NetSendBurst(Vector3 at) => NetCommand("burst", 0, at);
    }
}

