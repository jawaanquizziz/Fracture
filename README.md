# Valcom

### Catch the creature. Resist the corruption. Master the rift.

**An asymmetric multiplayer creature-catching game built with Unity.**

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

### Characters & Maps

The project has **25 character assets** and **3 maps** — all ready.

| Asset Type | Count |
|---|---|
| Pokémon creature models | **21** |
| Valorant agent models | **4** |
| Maps / arenas | **3** ✅ |

---

### 🐉 Creature & Agent Roster

![Character Roster Preview](Assets/Art/Previews/character_roster_v3.jpg)

All characters live in `Assets/Characters/`. `.glb` / `.gltf` files import directly into Unity via drag-and-drop.

#### Pokémon Creatures

| # | Creature | Type | Format |
|---|----------|------|--------|
| 1 | **Blastoise** | Water 💧 | `.zip` (3DS/FBX + textures) |
| 2 | **Bulbasaur** | Grass/Poison 🌿 | `.glb` |
| 3 | **Charizard** | Fire/Flying 🔥 | `.glb` |
| 4 | **Charmander** | Fire 🔥 | `.zip` |
| 5 | **Charmeleon** | Fire 🔥 | `.fbx` + textures |
| 6 | **Chikorita** | Grass 🌿 | `.zip` |
| 7 | **Cyndaquil** | Fire 🔥 | `.zip` |
| 8 | **Groudon** | Ground 🌋 | `.zip` |
| 9 | **Growlithe** | Fire 🔥 | `.zip` |
| 10 | **Ivysaur** | Grass/Poison 🌿 | `.glb` |
| 11 | **Lapras** | Water/Ice 💧 | `.zip` |
| 12 | **Lugia** | Psychic/Flying 🌀 | `.zip` (3DS/FBX + textures) |
| 13 | **Oddish** | Grass/Poison 🌿 | `.zip` |
| 14 | **Rayquaza** | Dragon/Flying ⚡ | `.gltf` + textures |
| 15 | **Snivy** | Grass 🌿 | `.zip` |
| 16 | **Squirtle** | Water 💧 | `.zip` (3DS/FBX + textures) |
| 17 | **Totodile** | Water 💧 | `.zip` |
| 18 | **Vaporeon** | Water 💧 | `.zip` |
| 19 | **Venusaur** | Grass/Poison 🌿 | `.glb` |
| 20 | **Vulpix** | Fire 🔥 | `.zip` |
| 21 | **Wartortle** | Water 💧 | `.zip` (3DS/FBX + textures) |

#### Valorant Agents

| # | Agent | Role | Format |
|---|-------|------|--------|
| 22 | **Clove** | Controller 💜 | `.glb` |
| 23 | **Gekko** | Initiator 🟢 | `.glb` |
| 24 | **Reyna** | Duelist 🔴 | `.glb` |
| 25 | **Tejo** | Initiator 🟡 | `.glb` |

> **Unity import:** `.glb` / `.gltf` → drag into Unity Project window. For `.zip` archives → extract, then import the inner `.fbx` file. Assign textures from the character's `/textures/` folder as the **Albedo** map on the material.

**Prop:**
- 🔵 **Poke Ball** — `poke_ball.glb` — ready to use as an in-game capture prop.

---

### 🗺️ Maps / Arenas

All 3 maps are ready as Blender scenes.

| Stage | Map | File | Format | Status |
|-------|-----|------|--------|--------|
| Stage 1 | **Map 1** | `map 1.blend` | Blender scene | ✅ Ready |
| Stage 2 | **Map 2** | `map2.blend` | Blender scene | ✅ Ready |
| Stage 3 | **Map 3** | `map3.blend` | Blender scene | ✅ Ready |

**To use in Unity:** Open each `.blend` in Blender → **File → Export → FBX** → save to `Game/Assets/Maps/` → import the `.fbx` into Unity and set up materials in the inspector.

## ⚔️ Battle Engine & Creature Moveset System

The project includes a complete, data-driven turn-based combat system located in [`Valmon/pokemon_moves/`](file:///Valmon/pokemon_moves/). Battles are resolved deterministically or probabilistically through lightweight JSON datasets and a headless Python battle engine.

### Folder Overview

- [`_template.json`](file:///Valmon/pokemon_moves/_template.json) — Schema template for creating new creatures. Copy, rename, and populate.
- [`type_chart.json`](file:///Valmon/pokemon_moves/type_chart.json) — Full 18-type attacker vs. defender damage multiplier matrix (Fire, Water, Grass, Electric, Dragon, Fairy, etc.).
- [`battle_engine.py`](file:///Valmon/pokemon_moves/battle_engine.py) — Headless turn-based combat resolver handling physical/special/status moves, STAB (Same Type Attack Bonus), weaknesses/resistances, critical hits, damage rolls, stat stages (attack, defense, accuracy, evasion), status conditions (burn, freeze, paralysis, poison, sleep, confusion, flinch), PP, priority, and bot strategies.
- **Creature Moveset Files** (`blastoise.json`, `charizard.json`, `venusaur.json`, `cyndaquil.json`, etc.) — Individual creature data definitions containing base stats, typing, and 4-move loadouts.

> **Hackathon Note:** The sample JSON files in `pokemon_moves/` provide baseline numeric balancing and move configurations. For final game submissions, update the creature `name` fields (and filenames) to match original creature designs while retaining the balanced numeric stat shapes.

---

### Creature File Schema

Every creature file follows a strict JSON structure:

```json
{
  "name": "Pyrocub",
  "types": ["Fire"],
  "stats": {
    "hp": 39,
    "atk": 52,
    "def": 43,
    "sp_atk": 60,
    "sp_def": 50,
    "speed": 65
  },
  "moves": [
    {
      "name": "Scratch",
      "type": "Normal",
      "cat": "Physical",
      "power": 40,
      "acc": 100,
      "pp": 35,
      "effect": null
    }
  ]
}
```

- **`types`**: 1 or 2 types from the standard 18-type chart (`Fire`, `Water`, `Grass`, `Normal`, `Flying`, `Dark`, `Dragon`, `Ghost`, `Poison`, `Ground`, `Psychic`, `Steel`, `Ice`, `Electric`, `Fighting`, `Bug`, `Rock`, `Fairy`).
- **`stats`**: Standard 6-stat block (`hp`: 30–120, offensive/defensive stats: 40–130).
- **`moves`**: Exactly 4 move slots, each containing:
  - `cat`: `"Physical"` | `"Special"` | `"Status"`
  - `power`: Base power (0 for status moves; 40–120 for damaging moves).
  - `acc`: Accuracy percentage (1–100).
  - `pp`: Power points (maximum uses per battle).
  - `effect`: Move effect trigger or `null`.

---

### Supported Move Effects

The battle engine implements 24+ unique move effects:

| Effect String | Mechanic / Description |
|---|---|
| `null` | Direct damage with no additional status or stage modifications. |
| `burn_10%` | 10% chance to inflict burn on hit (reduces physical attack, deals chip damage). |
| `paralyze_30%` | 30% chance to paralyze target on hit (reduces speed, chance to lose turn). |
| `freeze_10%` | 10% chance to freeze target on hit. |
| `poison_30%` | 30% chance to poison target on hit (deals recurring end-of-turn damage). |
| `flinch_30%` | 30% chance to cause the target to flinch if user attacks first. |
| `confuse_20%` | 20% chance to confuse target on hit. |
| `burn` | Guaranteed burn infliction (used on status moves). |
| `paralyze` | Guaranteed paralysis infliction. |
| `sleep` | Puts the target to sleep for 1–3 turns. |
| `confuse` | Guaranteed confusion infliction (target may hurt itself in confusion). |
| `lower_atk` | Lowers target's Attack by 1 stage. |
| `lower_def` | Lowers target's Defense by 1 stage. |
| `lower_accuracy` | Lowers target's Accuracy by 1 stage. |
| `lower_evasion` | Lowers target's Evasion by 1 stage. |
| `raise_def` | Raises user's Defense by 1 stage. |
| `sharp_raise_def` | Raises user's Defense by 2 stages. |
| `high_crit` | Increases critical strike rate ~3x (12.5% base). |
| `priority_+1` | Move executes first in round regardless of creature speed. |
| `recoil_25%` | User absorbs 25% of damage dealt as recoil damage. |
| `drain_50%` | User recovers HP equal to 50% of damage dealt. |
| `heal_50%` | Restores 50% of user's maximum HP. |
| `trap_4turns` | Traps target for 4 turns and inflicts persistent chip damage. |
| `fixed_dmg` | Inflicts damage exactly equal to move `power`, bypassing defense formulas. |
| `force_switch` | Forces target creature switch (for multi-creature rosters). |

---

### Python Battle Engine Simulation

The headless Python battle engine has zero graphical overhead and executes thousands of turns per second.

#### Running a Battle

```python
from battle_engine import BattleEngine

# Initialize with the dataset directory
engine = BattleEngine("./Valmon/pokemon_moves")

# Simulate a battle with strategy bots
log = engine.battle(
    "charizard",
    "blastoise",
    seed=42,
    strategy_a="greedy",
    strategy_b="random"
)

for line in log:
    print(line)
```

#### Bot Decision Strategies

- `"random"`: Selects any move with remaining PP at random.
- `"greedy"`: Calculates expected damage against the opponent (factoring type advantages and STAB) and executes the highest damage option.
- `"player:<MoveName>"`: Forces execution of a designated move (ideal for routing player UI inputs directly into the engine).

#### Applications

1. **Balance Matrix Tuning**: Execute 10,000 headless simulated matches across every creature matchup pair to build a win-rate matrix and eliminate over- or under-powered creatures.
2. **AI & RL Training**: Wrap the battle engine in a Gym/PettingZoo environment (observation: creature HPs, status conditions, remaining PP; action: move index 0–3) to train reinforcement learning policies for corrupted creature behavior.
3. **Dynamic Difficulty Scaling**: Tune the Warden's combat difficulty on the fly by swapping strategies (`random` for Easy, `greedy` for Medium, 1-step lookahead for Hard).

---

### Porting to Unity (C#)

The JSON creature and move definitions map directly to Unity `ScriptableObject` assets:

```csharp
[CreateAssetMenu(menuName = "Valcom/Creature Data")]
public class CreatureData : ScriptableObject {
    public string creatureName;
    public string[] types;
    public Stats stats;
    public MoveData[] moves; // Exactly 4 moves
}

[System.Serializable]
public class Stats {
    public int hp;
    public int atk;
    public int def;
    public int sp_atk;
    public int sp_def;
    public int speed;
}

[System.Serializable]
public class MoveData {
    public string moveName;
    public string type;
    public MoveCategory category; // Physical, Special, Status
    public int power;
    public int accuracy;
    public int pp;
    public MoveEffect effect;
}
```

The combat resolution logic can either be called via lightweight network/IPC calls to the Python engine during development, or implemented directly in C# using identical damage and effect formulas.

---

## Planned Features

-   **Asymmetric multiplayer:** Tamer and Warden have different roles
    and interfaces.
-   **Timing-based catching:** A timing challenge determines catch
    quality.
-   **Creature corruption:** The Warden can interrupt a catch and make a
    creature hostile.
-   **Turn-based battle engine:** Complete combat resolver with physical/special/status moves, STAB, 18-type effectiveness, stat stages, status ailments, priority, and AI strategies.
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
  Technology                          Planned / Active use
  ----------------------------------- -----------------------------------
  **Unity**                           Game engine, 3D arenas, and gameplay

  **C#**                              Gameplay logic, UI behavior, and
                                      ScriptableObject data models

  **Python**                          Headless battle engine, balance
                                      tuning, bot AI, and simulations
                                      (`Valmon/pokemon_moves/`)

  **JSON Datasets**                   Creature movesets, stats, and 18-type
                                      effectiveness matrix

  **Photon PUN 2**                    Multiplayer room management and event
                                      synchronization

  **Unity UI**                        Capture challenge, Warden dashboard,
                                      timer, energy meter, and battle UI
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

### 1. Unity Project Setup

1.  Open the Unity project through Unity Hub (targeting the repository root or `Valmon/`).
2.  Use the Unity Editor version configured for the project (Unity 6 / 2023+).
3.  Confirm character 3D models and arena assets import properly into the Asset pipeline.
4.  Configure Photon PUN 2 App ID in project settings when multiplayer networking is enabled.

### 2. Running Battle Engine Simulations

The headless Python battle engine can be tested directly from the terminal without opening Unity:

```bash
cd Valmon/pokemon_moves
python battle_engine.py
```

To run programmatic balance simulations or test custom creature pairings, import `BattleEngine` into your test script:

```python
from battle_engine import BattleEngine

engine = BattleEngine(".")
log = engine.battle("charmander", "squirtle", strategy_a="greedy", strategy_b="random")
for entry in log:
    print(entry)
```

## Project Status

**Active development & setup stage.**
- **3D Assets**: 14 character models (creatures & agents) and 3 arena maps ready.
- **Combat & Data Engine**: Turn-based battle resolver, 18-type chart, and 19+ creature moveset configurations functional in `Valmon/pokemon_moves/`.
- **Multiplayer & Arena Gameplay**: Asymmetric networked gameplay and capture challenges currently in development.

## Credits & License

Add credits for third-party character, map, and other assets before
publishing. No license was specified in the planning document; add a
`LICENSE` file before public distribution and verify third-party asset
licenses.

------------------------------------------------------------------------

::: {align="center"}
**Valcom** · *Every catch has a cost.*
:::
