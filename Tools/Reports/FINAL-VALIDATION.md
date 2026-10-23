# VALMON validation — 24 October 2026

## Milestones delivered in the existing Unity project

1. **Compilation:** Unity 6000.6.7f1 compiles the campaign. Generated scene and serialized catalog are valid; 41 asset/rule checks and 120 seeded battle simulations pass. No manual Editor wiring is needed.
2. **Map menu** (`CampaignMenus.cs`, `CampaignMapMenu.cs`, `CampaignLobby.cs`): actual supplied map and character models back the title menu. Play Online, Settings, How to Play, Credits, Exit, 22-creature roster and solo campaign are wired to actions. Four imported character models are selectable. Play the first enabled scene, `Assets/Scenes/VALMON_Campaign.unity`.
3. **Real lobby** (`CampaignNetwork.cs`, `LanUdpTransport.cs`): two Windows player processes establish an IPv4 UDP connection, with room code and character/name exchange. No fake player entries are used. `LanUdpTransport` also passes ordered delivery, retransmission, duplicate suppression and 11KB fragment reassembly with simulated packet loss. Join/control packet size has a regression check.
4. **Roles/countdown** (`CampaignRounds.cs`, `CampaignMenus.cs`, `CampaignNetwork.cs`): host assigns complementary roles, waits for both arenas to load, and starts a three-second countdown. Both initial host roles are tested. See `Rounds-Rendered` and `Rounds-Reversed` reports.
5. **Gameplay** (`CampaignController.cs`, `CampaignAgents.cs`, `CampaignGameplayUI.cs`): all three arenas, corrupted catches, player-controlled battles, ball counts, Warden gate/scan/trap effects, trap damage/slow and host/client Next Round commands pass actual two-process UDP tests under simulated 1-in-11 packet loss. Scores and winners agree across both clients.
6. **Lifecycle/presentation** (`CampaignRounds.cs`, `CampaignPresentation.cs`, `CampaignBuilder.cs`, `Docs/Multiplayer-Setup.md`): Play Again reloads both maps and clears scores/catches; Return to Lobby resets both peers while retaining the UDP room. Solo campaign, final duel, losses and AI navigation pass. The 12-second, 24fps MP4 decodes and prepares in the Windows player. Real rendered screenshots are saved in the report folders.

## Primary evidence

- `campaign-validation.txt`: 41 data/rule checks, including 120 seeded battles.
- `udp-transport-validation.txt`: ordered/reliable UDP and fragmented snapshots under loss.
- `Rounds-Rendered/udp-host.txt`, `udp-client.txt`: complete match with graphics enabled; accompanying PNGs are game screenshots.
- `Rounds-Reversed/udp-host.txt`, `udp-client.txt`: host initially Warden.
- `FinalRoundLifecycle/udp-host.txt`, `udp-client.txt`: three rounds, Play Again, Return to Lobby.
- `FinalSolo/runtime-smoke.txt`: packaged video, all maps, catches, final duel, timeout/ball/team loss and AI Tamer.
- `OpeningVideo`: decoded MP4 sample frames; video metadata reports 288 frames at 24fps.

Earlier report folders contain intermediate failures and are retained for debugging. The named final reports above supersede those intermediate runs.

## Test limits / remaining work

A physical two-laptop test on your network is still required. Two processes on this PC verify actual UDP sockets and game synchronization, but cannot verify another laptop's firewall, Wi-Fi device isolation or router settings. Follow the packaged Multiplayer-Setup guide.

Development integration fixtures reposition avatars and force low health for selected terminal-state checks. Normal battle calculations are covered by separate seeded simulations. Human playtesting is still needed for balance, camera comfort and presentation. Existing imported models mostly use procedural motion and their supplied poses. Internet matchmaking/relay, authenticated accounts, reconnection and mid-match saves are outside this LAN release.

