# Hollow Vault — AI System
**GAME-10020 · Assignment 3: AI System Implementation**  
Student: Yatharth Chopra

---

## Game Concept

**Hollow Vault** is a top-down action game set inside an underground cursed treasury. The player — a relic thief — moves through vault rooms, avoids two AI guardians, and escapes through the exit. Two AI agents guard the vault, each with distinct sensory systems and emergent co-dependence.

---

## Gameplay Video

[![Hollow Vault — Gameplay](https://img.youtube.com/vi/bNwFLJFRnx8/0.jpg)](https://youtu.be/bNwFLJFRnx8)

> [https://youtu.be/bNwFLJFRnx8](https://youtu.be/bNwFLJFRnx8)

---

## AI Agents Overview

| Agent | Type | Sensor | Role |
|---|---|---|---|
| **Crypt Sentinel** | Undead armoured guardian | Vision cone 60° / 9 m, hearing 8 m | Slow, high-HP pursuer — punishes visibility mistakes |
| **The Shade** | Spectral stalker | Sound radius 8 m, light sensor 5 m | Attrition threat — punishes noise and rewards torch use |

---

## FSM Diagram — Crypt Sentinel

```
         +--------+
         |  PATROL |<--------------------------+
         +--------+                            |
              |                                |
  hears sound / sees player                    |
              |                                |
              v                                |
       +-------------+    no target found      |
       | INVESTIGATE |------------------------>|
       +-------------+               (returns to patrol)
              |
     confirms sighting
              |
              v
         +--------+   player out of range    +--------+
         | COMBAT |------------------------->| RETURN |----> PATROL
         +--------+                          +--------+
          |     |
  hit by  |     | HP low
  player  |     |
          v     v
     +----------+    +----------+
     | STAGGERED|    | RALLYING |
     +----------+    +----------+
          |               |
      1.5s timer      cry fired
          |               |
          +---> COMBAT <--+
```

### Crypt Sentinel — States

| State | Behaviour |
|---|---|
| **PATROL** | Walks between waypoints in a loop at 1.8 m/s. Vision cone active. Transitions to INVESTIGATE on sound or sight. |
| **INVESTIGATE** | Moves toward last known noise/sight position at patrol speed. Escalates to COMBAT on confirmed sighting; returns after 4 s timeout. |
| **COMBAT** | Charges player at 3.2 m/s and attacks at melee range (1.5 m). Maintains 4 s memory after losing sight. |
| **STAGGERED** | Brief 1.5 s stun after being hit. Agent stops moving. Gives player a tactical window. |
| **RALLYING** | Stops and broadcasts a rally cry. If The Shade is within 10 m, it enters RALLIED → STALK. Resumes COMBAT immediately after. |
| **RETURN** | Walks back to patrol origin at patrol speed. Transitions to PATROL on arrival. |

### Crypt Sentinel — Transitions

| From | To | Condition |
|---|---|---|
| PATROL | INVESTIGATE | Player enters view cone OR sound detected |
| INVESTIGATE | COMBAT | Player confirmed in view cone |
| INVESTIGATE | PATROL | Timeout (4 s, no sighting) |
| COMBAT | STAGGERED | Player hits Sentinel (`OnPlayerHitSentinel`) |
| COMBAT | RALLYING | HP drops below 35% (`OnSentinelLowHP`) |
| COMBAT | RETURN | Player out of sight for > 4 s |
| STAGGERED | COMBAT | 1.5 s recovery timer expires |
| RALLYING | COMBAT | Immediately after cry fires |
| RETURN | PATROL | Home waypoint reached |

---

## FSM Diagram — The Shade

```
        +-------+
        |  DRIFT |<------------------------------+
        +-------+                               |
             |                                  |
    sound detected                              |
             |                                  |
             v                                  |
        +-------+   sound fades (3s)            |
        | ALERT |------------------------------>|
        +-------+                       (returns to drift)
             |
    reaches sound source
             |
             v
        +-------+   player gone / arrived    +---------+
        | STALK |--------------------------->|  (DRIFT) |
        +-------+
             |                    torch lit nearby
    player within 3m       +----------------------------------+
             |              |                                  |
             v              |                                  |
        +------+       +---------+                            |
        | HUNT |------>| RETREAT |----> DRIFT (torch off / safe)
        +------+       +---------+
             |
    Rallied by Sentinel
             |
             v
       +---------+
       | RALLIED |----> STALK (immediately)
       +---------+
```

### The Shade — States

| State | Behaviour |
|---|---|
| **DRIFT** | Floats slowly (2.5 m/s) to random nearby NavMesh positions. Sound sensor active. |
| **ALERT** | Moves toward the sound source. 3 s fade timer. |
| **STALK** | Moves silently toward confirmed sound position. Shares position with Sentinel. Light sensor active — retreats on torch. |
| **HUNT** | Locks onto player at 4.5 m/s. Drains player stamina on contact each frame. |
| **RETREAT** | Flees directly away from player. Triggered by torch. Resumes DRIFT when safe or torch off. |
| **RALLIED** | One-frame passthrough state entered via Sentinel rally cry — immediately transitions to STALK. |

### The Shade — Transitions

| From | To | Condition |
|---|---|---|
| DRIFT | ALERT | Sound detected within `soundRadius × intensity` |
| ALERT | STALK | Reaches sound origin |
| ALERT | DRIFT | Fade timer (3 s) with no new sound |
| STALK | HUNT | Player within 3 m |
| STALK | RETREAT | Torch lit within 5 m |
| STALK | DRIFT | Arrived at position, player not found |
| HUNT | STALK | Player escapes beyond 5 m |
| HUNT | RETREAT | Torch lit within 5 m |
| RETREAT | DRIFT | Safe distance reached OR torch turned off |
| RALLIED | STALK | Immediately on Enter |

---

## Sensory Systems

### Sentinel — Vision Cone
Implemented in `VisionCone.cs`. Three-step check per frame:
1. Distance ≤ `viewRadius` (9 m)
2. Angle ≤ half `viewAngle` (30° either side of forward vector) using `Vector3.Angle()`
3. `Physics.Raycast` — blocked by obstacles

Visualised at runtime by `VisionConeMesh.cs` — a procedural fan mesh rendered using Standard shader in transparent mode. Appears **cyan** when searching, **red** when player is detected. Visible in the Game window during play.

### Shade — Hearing
Player emits footstep sounds every 0.4 s via `SoundEmitter.Emit(position, intensity)`, which fires `GameEvents.OnSoundEmitted`. Walking intensity: 0.6 · Sneaking intensity: 0.15. Detection range = `soundRadius × intensity` (8 m × 0.6 = 4.8 m walking).

### Shade — Light Sensor
Player toggles torch with `F`. `GameEvents.OnTorchToggled(bool)` fires. If player is within `lightRadius` (5 m), Shade enters RETREAT immediately from any state.

---

## Event System

All agent-to-agent and agent-to-player communication uses **C# Actions** in a central static `GameEvents` class. No `FindObjectOfType`, no direct cross-agent references.

| Event | Fired by | Handled by |
|---|---|---|
| `OnSoundEmitted(Vector3, float)` | `SoundEmitter.Emit()` on footsteps | Shade (Drift/Alert), Sentinel (Patrol) |
| `OnTorchToggled(bool)` | `PlayerController` on F key | `TheShade` — triggers Retreat if lit and close |
| `OnSentinelRallyingCry(Vector3)` | `SentinelRallyingState` | `TheShade.OnRallyingCry()` — enters Rallied if within 10 m |
| `OnShadeSharesPlayerPosition(Vector3)` | `ShadeStalkState` | `CryptSentinel.OnShadeSharedPosition()` — updates memory |
| `OnPlayerHitSentinel` | `PlayerAttack` on LMB hit | `CryptSentinel.OnHitByPlayer()` — triggers Staggered |
| `OnSentinelLowHP` | `CryptSentinel.TakeDamage()` | `CryptSentinel.OnLowHP()` — triggers Rallying |
| `OnShadeContactPlayer` | `ShadeHuntState` on contact | `PlayerController` — drains stamina |
| `OnPlayerCaught` | `SentinelCombatState.Attack()` | `GameManager` — shows game over screen |
| `OnPlayerEscaped` | `GoalZone.OnTriggerEnter()` | `GameManager` — shows win screen |

---

## Player-AI Interaction Scenario

### The Torch Trade-off
The most critical player decision is whether to use the torch:
- **Torch ON** — Shade immediately retreats. But the player is now visible to the Sentinel from further away.
- **Torch OFF** — Shade stalks freely, but Sentinel operates on vision alone.

### The Rallying Cry (Agent-Agent Interaction)
1. Player damages Sentinel — HP drops below 35%
2. Sentinel enters STAGGERED → RALLYING, stops and broadcasts `OnSentinelRallyingCry`
3. If Shade is within 10 m, it enters RALLIED → immediately transitions to STALK
4. Player now faces a recovering Sentinel and a Shade closing from a different direction

---

## Challenges and Solutions

### 1. State leaking between transitions
**Problem:** Timers set outside their own state carried stale values into new states.  
**Solution:** Used the polymorphic FSM pattern (`Enter()`, `Execute()`, `Exit()`) — every state resets its own timers in `Enter()`.

### 2. Event subscriptions causing double-triggers
**Problem:** Subscribing in `Awake` and never unsubscribing caused stacking listeners on scene reload.  
**Solution:** Subscriptions are done in `Enter()` and removed in `Exit()` for states that need them. `LevelManager` handles agent-level subscriptions in `OnEnable` / `OnDisable`.

### 3. NavMesh sampling for Shade's random drift
**Problem:** `Random.insideUnitSphere` sometimes picked positions off the NavMesh, freezing the agent.  
**Solution:** Added `NavMesh.SamplePosition()` to find the nearest valid NavMesh point before setting the destination.

### 4. Cross-agent coordination without tight coupling
**Problem:** Sentinel needed to notify the Shade without either holding a direct reference to the other.  
**Solution:** Static `GameEvents` class with C# Actions (observer pattern). `LevelManager` wires subscriptions — agents stay fully decoupled.

---

## Code Architecture

```
Assets/Scripts/
├── FSM/
│   └── State.cs                    ← Abstract base (Enter / Execute / Exit)
├── Events/
│   └── GameEvents.cs               ← Static C# Actions hub
├── Sensors/
│   ├── VisionCone.cs               ← 3-step view cone + raycast
│   ├── VisionConeMesh.cs           ← Runtime fan mesh (cyan/red)
│   └── SoundEmitter.cs             ← Static Emit() broadcaster
├── Agents/
│   ├── CryptSentinel.cs            ← Sentinel brain + 6 state instances
│   ├── SentinelPatrolState.cs
│   ├── SentinelInvestigateState.cs
│   ├── SentinelCombatState.cs
│   ├── SentinelStaggeredState.cs
│   ├── SentinelRallyingState.cs
│   ├── SentinelReturnState.cs
│   ├── TheShade.cs                 ← Shade brain + 6 state instances
│   ├── ShadeDriftState.cs
│   ├── ShadeAlertState.cs
│   ├── ShadeStalkState.cs
│   ├── ShadeHuntState.cs
│   ├── ShadeRetreatState.cs
│   └── ShadeRalliedState.cs
├── Player/
│   ├── PlayerController.cs         ← WASD, sneak, torch, footstep emission
│   ├── PlayerAttack.cs             ← LMB raycast, triggers Staggered
│   └── FollowCamera.cs
├── UI/
│   └── StaminaBar.cs
├── Level/
│   ├── LevelManager.cs             ← Wires all cross-agent events
│   ├── GoalZone.cs                 ← Fires OnPlayerEscaped on trigger enter
│   └── GameManager.cs             ← Win/lose screen, R to restart
└── Editor/
    └── SceneSetup.cs               ← Tools → Setup Hollow Vault Scene
```

---

## Controls

| Key | Action |
|---|---|
| WASD | Move |
| Left Shift | Sneak (quieter footsteps, slower movement) |
| F | Toggle Torch (repels Shade, boosts Sentinel vision) |
| Left Mouse | Attack Sentinel (triggers Stagger) |
| R | Restart (after win or lose) |

---

*GAME-10020 · Assignment 3 · Yatharth Chopra · Mohawk College*
