::: {align="center"}
# Valcom

### Catch the creature. Resist the corruption. Master the rift.

**An asymmetric multiplayer creature-catching game built with Unity.**
:::

------------------------------------------------------------------------

## About the Game

**Valcom** is a two-player game where each player has a different
role. The **Tamer** explores arenas and attempts to catch creatures,
while the **Warden** protects them by spending energy to corrupt
creatures or trigger environmental hazards.

A clean catch earns a higher score. But if the Warden corrupts a
creature at the right moment, the capture attempt is interrupted and a
short battle begins.

> **Current project status:** Early setup and asset organization.
> Gameplay code has not been implemented yet.

## Gameplay

### The Tamer

-   Explore the arena and approach a creature.
-   Attempt a timing-based catch.
-   Catch the creature cleanly for a higher score.
-   Fight back if the Warden corrupts the creature during a capture
    attempt.

### The Warden

-   Use a separate control interface to monitor the match.
-   Spend a recharging energy pool on corruption and hazards.
-   Disrupt captures by turning creatures hostile.
-   Use abilities strategically because each action has an energy cost.

### Round Flow

1.  Two players join the same match.
2.  The Tamer searches for a creature and starts a catch attempt.
3.  The Warden may use corruption or an environmental hazard.
4.  Corruption can trigger a short turn-based battle.
5.  The round is scored based on catch quality and remaining time.
6.  Players switch roles for the next round.

## Characters & Maps

The project has **6 creature assets** and **1 map** (2 more planned).

| Asset Type | Count |
|---|---|
| Creature 3D models | **6** |
| Maps / arenas | **1** ready · 2 planned |

---

### 🐉 Creature Roster

![Character Roster Preview](Game/Assets/Art/Previews/character_roster_preview.jpg)

All six creatures have full 3D models with texture sets stored under `Game/Assets/Characters/`.

| # | Creature | Type | 3D Format | Body Texture |
|---|----------|------|-----------|--------------|
| 1 | **Blastoise** | Water 💧 | `.zip` (3DS source) | ![](Game/Assets/Characters/blastoise/textures/pm0009_00_BodyA1.png) |
| 2 | **Charmeleon** | Fire 🔥 | `.fbx` | ![](Game/Assets/Characters/charmeleon-005-sv/textures/pm0005_00_00_body_a_alb.png) |
| 3 | **Lugia** | Psychic/Flying 🌀 | `.zip` (3DS source) | ![](Game/Assets/Characters/lugia/textures/pm0249_00_Body1.png) |
| 4 | **Rayquaza** | Dragon/Flying ⚡ | `.gltf` | ![](Game/Assets/Characters/rayquaza-remastered/textures/gltf_embedded_0.png) |
| 5 | **Squirtle** | Water 💧 | `.zip` (3DS source) | ![](Game/Assets/Characters/squirtle/textures/pm0007_00_Body1.png) |
| 6 | **Wartortle** | Water 💧 | `.zip` (3DS source) | ![](Game/Assets/Characters/wartortle/textures/pm0008_00_Body1.png) |

> **Unity import note:** `.fbx` and `.gltf` import directly into Unity. For `.zip` archives, extract first and import the inner 3DS/FBX file. Apply each character's `/textures/` folder images as the **Albedo** map on the Unity material.

---

### 🗺️ Maps / Arenas

| Stage | Map | File | Format | Status |
|-------|-----|------|--------|--------|
| Stage 1 | **Map 1** | `map 1.blend` | Blender scene | ✅ Asset ready |
| Stage 2 | Map 2 | — | TBD | 🔲 Planned |
| Stage 3 | Map 3 | — | TBD | 🔲 Planned |

**Map 1** is located at `Game/Assets/Maps/map 1.blend`.  
To use in Unity: open in Blender → **File → Export → FBX** → save to `Game/Assets/Maps/` → import the `.fbx` into Unity.

## Planned Features

-   **Asymmetric multiplayer:** Tamer and Warden have different roles
    and interfaces.
-   **Timing-based catching:** A timing challenge determines catch
    quality.
-   **Creature corruption:** The Warden can interrupt a catch and make a
    creature hostile.
-   **Short turn-based battles:** Planned three-move battle system:
    Quick, Dodge, and Pacify.
-   **Environmental hazards:** Locked gate, patrol turret, and
    collapsing bridge.
-   **Recharging energy:** Warden abilities share a limited energy pool.
-   **Three maps:** Difficulty increases across the three stages.
-   **Role switching:** Players swap roles between rounds.
-   **Timer and scoring:** Clean catches, battled catches, and remaining
    time contribute to the score.
-   **Results screen:** Planned summary of score and catch outcomes.

## Technology Stack

  -----------------------------------------------------------------------
  Technology                          Planned use
  ----------------------------------- -----------------------------------
  **Unity**                           Game engine, scenes, arenas, and
                                      gameplay

  **C#**                              Gameplay logic and UI behavior

  **Photon PUN 2**                    Multiplayer rooms and event
                                      communication

  **Unity UI**                        Capture challenge, Warden controls,
                                      timer, energy meter, and results
  -----------------------------------------------------------------------

## Planned Multiplayer Design

The game plan proposes two players joining the same Photon room,
typically on separate computers.

-   **Tamer client:** Displays the 3D arena and Tamer gameplay.
-   **Warden client:** Displays a separate control panel.
-   **Room code:** Allows both players to join the same match.
-   **Network events:** Planned communication for corruption, hazards,
    catch results, battle transitions, and role changes.
-   **Position display:** A low-frequency position update may help the
    Warden see the Tamer's approximate location.

Multiplayer is part of the design plan and has not been verified as
implemented.

## Getting Started

The project is currently being organized. Once the Unity project is
created or confirmed:

1.  Open the Unity project through Unity Hub.
2.  Use the Unity Editor version intended for the project.
3.  Confirm that the character and map assets import correctly.
4.  Add the Photon PUN 2 package and configure an App ID when
    multiplayer development begins.
5.  Test networking with two clients before building gameplay systems
    that depend on it.

Do not commit private credentials or secrets to version control.

## Project Status

**Early development / setup stage.** The five character assets and three
maps are available for organization. Gameplay systems described above
are planned and should not be considered implemented until they have
been built and tested.

## Credits & License

Add credits for third-party character, map, and other assets before
publishing. No license was specified in the planning document; add a
`LICENSE` file before public distribution and verify third-party asset
licenses.

------------------------------------------------------------------------

::: {align="center"}
**Valcom** · *Every catch has a cost.*
:::
