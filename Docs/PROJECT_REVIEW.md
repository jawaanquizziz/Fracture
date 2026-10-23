# VALMON project review

## Folder inspection

- The root `Assets`, `Packages` and `ProjectSettings` form the active Unity 6000.6.7f1 project. Campaign work belongs here.
- `Valmon/` contains a separate Unity project with its own settings and Library. Opening that folder will not load the root campaign.
- `Game/Assets` holds source copies of models, textures and Blender maps. Root `Assets/Characters`, `Assets/Models` and `Assets/Resources` also contain duplicates.
- Root `Assets/Characters/ImportedPokemon` contains the extracted FBX creatures and conversions of the Blender/glTF-only creatures. Source archives remain intact.
- `Video/` holds the rendered opening and editable Blender scene. The campaign packages the MP4 in `Assets/StreamingAssets`.
- `Assets/Scripts/Gameplay`, `Environment`, `Warden`, `Player` and `Network` are an earlier prototype. The new campaign lives under `Assets/Scripts/Campaign`.

## Problems found and addressed

1. The old runtime bootstrap ran after every scene load. It could inject the prototype into a gallery, menu or campaign. It now runs only in SampleScene and MainGame.
2. The earlier rounds used a short timer and automatic role swapping, which did not match the requested three-level campaign. The campaign now owns its stage, quota, timer, party and final-duel state separately.
3. Many creature sources were hidden inside nested ZIPs or Blender files. The expanded roster now supplies 22 species and four human characters through serialized model references.
4. Existing map placement used fixed coordinates and colliders without explicit mesh assignment. The campaign normalizes each original map, explicitly assigns collision meshes and finds a connected surface before spawning creatures and the Tamer.
5. The root README claimed a Python battle engine and a `pokemon_moves` JSON directory, but those files were not found in the inspected project. The working campaign rules and move definitions are now in C#; the old description is preserved as historical documentation.
6. Legacy player scripts read Unity's old input API while the project uses the new Input System. The campaign uses the installed Input System for movement, camera controls, skipping and capture input.
7. A ball spent on a corrupted encounter must still be allowed to resolve when the supply reaches zero. The rule checks explicitly protect that pending encounter from an early no-balls loss.
8. Standalone testing found that map meshes were not readable outside the Editor, preventing runtime collision cooking. The builder now enables Read/Write on the three map imports and validates that their CPU mesh data is retained.
9. The map FBX files include cameras and lights that could override the gameplay view. Campaign instances now disable imported cameras, lights and audio listeners. Runtime validation checks that only the campaign camera renders each map.
10. Charmander's converted FBX did not resolve its supplied texture automatically. The catalog now explicitly references that texture. Character facing is configured per model, and Reyna's supplied glTF pose is baked into a separate FBX to avoid its inconsistent skin binding in Unity.

## Explicit design decisions

- The user confirmed one starter and a reset of ten balls and eight minutes at every stage, with AI opposing the selected local role.
- Clean catches are guaranteed after a 2.7-second channel. Corruption before the channel ends forces battle. One ball is consumed per attempt, including a lost battle.
- A defeated Tamer creature is permanently removed during that run. Losing a wild battle returns to exploration if teammates remain; the wild creature stays corrupted. Its health resets on the next attempt.
- Wild battle victories grant the winning teammate two levels, capped at the current stage maximum, plus a small health recovery. Level transitions heal and raise underlevelled teammates to the new baseline.
- Final battle has three distinct random Lv.50 enemies, no catches, and no catching timer. Surviving Tamer creatures heal and train to at least Lv.45.
- Both roles observe the same Tamer stage progression. If the Tamer clears a habitat while you play Corruptor, the confrontation continues at the next habitat; the Corruptor can still win by eliminating the team later.
- A simultaneous knockout that empties the Tamer's team awards the Corruptor the win.
- Combat is a compact original implementation with alternating turns, level-scaled stats, physical/special attacks, PP, type effectiveness, critical hits, healing, defense/attack/accuracy stages, burn, poison, sleep, drain and recoil. It is not a complete implementation of every Pokémon generation mechanic. Move effects and balance are simplified; evolution, abilities, items and full canonical priority rules are not implemented.

## Verification completed

- Unity 6000.6.7f1 compiled the campaign and generated its scene and catalog successfully.
- The catalog/rule validator passed 41 checks, including 120 seeded battle simulations, model references, standalone collision readability, habitat level bands, last-ball handling, timer expiry and empty-party losses. Results: `Tools/Reports/campaign-validation.txt`.
- A Unity Play Mode integration run passed the full three-stage campaign, clean and corrupted captures, teammate loss, final victory, timer/ball losses, Corruptor control and AI Tamer navigation. Results: `Tools/Reports/Runtime/runtime-smoke.txt`.
- The map sampler found 720, 750 and 1,619 reachable nodes in maps 1, 2 and 3 respectively. Each map spawned twelve creatures from its matching type and level band.
- Integration tests force low health for selected terminal-state checks; the separate seeded battles exercise normal damage calculations. These checks verify transitions and invariants, not human play balance.

## Remaining limitations

- Direct LAN UDP multiplayer is implemented with a real two-player lobby, authoritative roles/countdown, physical Warden movement and three role-swapping rounds. Internet matchmaking, relay hosting and mid-match reconnect are not included. A physical two-laptop router/firewall test remains necessary.
- The imported character/creature assets do not provide a consistent shared animation controller. Movement and battle lunges are procedural; many models retain their supplied pose.
- Some imported FBX materials may still need hand tuning, especially eye layering, transparency and external textures. Material conversion preserves textures that the importer resolved, but cannot recreate absent authored material assignments.
- The map navigation graph samples connected walking surfaces and supports the local AI. This is not a hand-authored NavMesh or a full dynamic pathfinding system. A foundation closes outer map gaps, and the Tamer has an R-key recovery to spawn.
- Unlocks and volume persist locally; active-run saves and party recovery after closing the app are not implemented.
- Final rosters and type matchups have large strength differences. Automated tests check correctness and termination, not competitive fairness. Human playtesting remains needed for balance and presentation.
- Source assets are duplicated across several project/source folders. Consolidation should follow the gameplay work, with GUID references checked before removing anything.

## Multiplayer implementation

The campaign uses `LanUdpTransport` and `CampaignNetwork`, not the older prototype TCP manager. The host validates movement, roles, range, energy, cooldowns and turn ownership. Reliable ordered commands use acknowledgements, retransmission and duplicate suppression; sequenced snapshots include both avatars, parties, PP/health, timer, corruption, traps, gates, roles and scores. Lobby control packets are serialized separately to stay within a single UDP datagram.

The new multiplayer format has three rounds, automatic first-role assignment, a three-second countdown after both maps load, role swapping, arena rotation, score breakdowns, final results, Play Again and Return to Lobby. Solo campaign progression and its final duel remain separate.

`Docs/Multiplayer-Setup.md` contains the Windows-to-Windows LAN steps. No Python server or Photon account is required. The setup guide records the verification scope; current machine test reports are under `Tools/Reports/Rounds-*`. Development fixtures force selected health/position values to test terminal transitions. They do not establish human gameplay balance.
