# VALMON — two Windows laptops over UDP

## Copy and launch

1. Copy the entire `Builds/VALMON` folder to the other Windows laptop. Keep `VALMON.exe`, `VALMON_Data`, `UnityPlayer.dll` and all other files together. Unity and Python are not required on either laptop.
2. Connect both laptops to the same trusted Wi-Fi, Ethernet network, or Windows mobile hotspot. This release uses direct LAN UDP; it does not include Internet matchmaking or a relay.
3. Run `VALMON.exe` on both laptops. Click or press a key to skip the opening. Enter different player names, then select **Play Online**.
4. Choose a character on each laptop. On laptop 1, choose the first arena and click **Host Room**. Share the displayed IPv4 address, UDP port (default **7788**), and six-digit room code.
5. On laptop 2, enter those values and select **Join Room**. Both copies must be from the same build.
6. Once both players connect and load the arena, the host automatically assigns one Tamer and one Warden. The match starts after a three-second countdown.

## Firewall and connection problems

Allow VALMON on **Private networks** in the Windows Firewall prompt. If Windows did not show a prompt, an administrator can run the included helper from PowerShell in the game folder:

```powershell
powershell -ExecutionPolicy Bypass -File .\Allow-VALMON-LAN.ps1
```

The helper creates one inbound UDP rule for this executable, port 7788, Private networks and the local subnet. For a different port append `-Port 17888` and use that port on both laptops. The helper does not change your network profile. Use a trusted Private network.

Use the Wi-Fi/Ethernet IPv4, not a VPN, virtual adapter, or 127.0.0.1 address. Guest Wi-Fi often blocks communication between devices: use a mobile hotspot or another network if needed. A six-digit code prevents accidental joins; this is a local match, not an authenticated Internet service. The game reports a failed join after 20 seconds and a lost peer after 15 seconds. Use Leave Lobby and host/join again to reconnect.

## Controls and rules

- Both roles: WASD move, Shift sprint, hold right mouse to orbit. Esc pauses both players.
- Tamer: **Z** catches within range, **Q** dashes, **R** returns to spawn. Start each round with one starter, 10 balls and 8 minutes. Catch at least 5 wild creatures to qualify. Then choose Finish Round or keep catching until the timer expires. At 6 catches, earn one additional score point. The starter is excluded from the quota. The Corruptor earns one additional score point after corrupting 6 distinct wild creatures; this does not end the round. Captured creatures retain their corruption credit. Both counters reset each round.
- Warden: **Q** corrupts the nearest clean creature within 12m (25 energy); **E** places a trap at your feet (30); **R** locks a gate within 8m for 8 seconds (35); **F** reveals targets on the radar for 6 seconds (15). Buttons show cooldowns. Left-click a creature to target corruption; Shift-click terrain within 15m for an area burst (55 energy).
- A trap causes 25 damage and a four-second slow; it also interrupts an active catch. Four trap hits defeat the Tamer.
- Corrupting a creature during a catch triggers partner selection and a turn-based battle. Each player chooses their side's moves. Winning completes the catch. Losing removes the selected teammate. Losing the entire team, exhausting the quota's possible balls, or running out of time awards the round to the Warden.
- **Next Round** swaps roles and loads the next arena. Arena order rotates from the host's selection: Grass → Fire → Water. Every round resets the starter, balls and timer. Grass creatures are levels 1–15; Fire 16–30; Water 31–50.
- After three rounds the player with most round wins takes the match. Results show captures, stopped captures, sabotage actions, round points and total points. Choose Play Again or Return to Lobby. In the lobby, the host can start another match.
- The separate Solo Campaign retains progression across all three maps and the final Corruptor battle.

## Unity preview and customization

Open the project in Unity 6000.6.7f1 and play `Assets/Scenes/VALMON_Campaign.unity` (first enabled build scene). No manual scene wiring is required. Use **Tools > VALMON > Build Campaign** to regenerate the catalog/scene and validate the roster, maps, rules and UDP transport. Use **Build Windows Release** to rebuild the portable release.

- `Assets/Scripts/Campaign/CampaignMenus.cs`: menu, lobby, previews, countdown, result layout.
- `CampaignRounds.cs`: automatic roles, three rounds, scoring, energy, cooldowns, traps and gates.
- `CampaignGameplayUI.cs`: role HUD and radar.
- `CampaignNetwork.cs` / `LanUdpTransport.cs`: authoritative match state / UDP reliability.
- `CampaignMapMenu.cs` / `CampaignPresentation.cs`: original map menu and opening stage.
- `Assets/Campaign/CampaignCatalog.asset`: supplied character/creature/map references and music hooks.
- `CampaignRules.cs`: balls, timer, catch quota and battle logic.

Existing map and model source files are preserved. The packaged 12-second stadium MP4 is `Assets/StreamingAssets/VALMON_Championship_Opening.mp4`; its source export is under `Video/`. The original rift video is also preserved. When the MP4 is absent, Unity uses the live 3D opening. The catalog exposes menu music, capture, corruption and victory clips; missing effect clips use synthesized cues.

## Verification scope

See the accompanying validation report for the exact tests run. Two local Windows game processes exercise real UDP sockets, packet loss, automatic roles, all three arenas, corruption/battles, sabotage and score synchronization. A physical two-laptop Wi-Fi test must still be performed with the steps above; local testing cannot verify your router or the other laptop's firewall.

