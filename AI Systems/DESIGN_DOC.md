# Hollow Vault — AI System Design Document
**Yatharth Chopra | GAME 10020 | Assignment 3**

---

## Game Concept

Hollow Vault is a top-down action game set in an underground cursed treasury. The player is a relic thief moving through vault rooms. Two AI agents guard the vault: the **Crypt Sentinel** (a heavy armoured undead guard) and **The Shade** (a ghostly stalker that reacts to sound and retreats from torchlight).

The two agents work together — the Sentinel uses vision, the Shade uses hearing — and can alert each other through a Rallying Cry mechanic.

---

## Gameplay Video

> **Link:** [https://youtu.be/bNwFLJFRnx8](https://youtu.be/bNwFLJFRnx8)

---

## State Machine Diagrams

### Crypt Sentinel FSM

```
          +--------+
          |  PATROL |<--------------------------+
          +--------+                            |
               |                                |
   hears sound / sees player                    |
               |                                |
               v                                |
        +-------------+    no target found      |
        | INVESTIGATE |------------------------->|
        +-------------+                  (returns to patrol)
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

### The Shade FSM

```
         +-------+
         | DRIFT |<------------------------------+
         +-------+                               |
              |                                  |
     sound detected                              |
              |                                  |
              v                                  |
         +-------+   sound fades (3s)            |
         | ALERT |------------------------------>|
         +-------+                          (returns to drift)
              |
     reaches sound source
              |
              v
         +-------+   player gone / arrived    +----------+
         | STALK |--------------------------->|  (DRIFT)  |
         +-------+
              |                         torch lit anywhere
     player within 3m           +----------------------------------+
              |                  |                                  |
              v                  |                                  |
         +------+          +---------+                             |
         | HUNT |--------->| RETREAT |----> DRIFT (torch off / safe)|
         +------+          +---------+
              |
     Rallied by Sentinel
              |
              v
        +---------+
        | RALLIED |----> STALK (immediately)
        +---------+
```

---

## State Behaviours and Transitions

### Crypt Sentinel

| State | Behaviour | Transitions Out |
|---|---|---|
| **PATROL** | Walks waypoint loop at 1.8 m/s. Vision cone (60°, 9m) active. Listens for sounds. | → INVESTIGATE: sees player OR hears nearby sound |
| **INVESTIGATE** | Moves to last known noise or sight position at patrol speed. 4-second memory timer. | → COMBAT: confirms player in vision cone; → PATROL: timer runs out with no sighting |
| **COMBAT** | Charges player at 3.2 m/s, attacks at melee range (1.5m) with 1.5s attack cooldown. Updates last known position every frame. | → STAGGERED: player hits Sentinel (event); → RALLYING: HP drops below 35% (event); → RETURN: loses sight for 4+ seconds |
| **STAGGERED** | Stops moving. 1.5-second stun timer. Gives player a damage window. | → COMBAT: timer expires |
| **RALLYING** | Stops and fires the `OnSentinelRallyingCry` event. Shade responds if within 10m. | → COMBAT: immediately after cry fires |
| **RETURN** | Walks back to waypoint index 0 at patrol speed. | → PATROL: reaches waypoint |

### The Shade

| State | Behaviour | Transitions Out |
|---|---|---|
| **DRIFT** | Floats slowly (2.5 m/s) to random nearby NavMesh positions. Listens for sounds. | → ALERT: sound detected within soundRadius × intensity |
| **ALERT** | Moves toward the sound source. 3-second fade timer. | → STALK: reaches the sound origin; → DRIFT: fade timer runs out |
| **STALK** | Moves silently toward confirmed sound position. Shares position with Sentinel via event. | → HUNT: player within 3m; → RETREAT: torch lit nearby; → DRIFT: arrived at position, player not found |
| **HUNT** | Locks onto player, closes at 4.5 m/s. Fires `OnShadeContactPlayer` event each frame on contact to drain stamina. | → STALK: player escapes beyond 5m; → RETREAT: torch lit nearby |
| **RETREAT** | Flees directly away from player. Speed matches hunt speed. Listens for torch-off event. | → DRIFT: reaches safe distance OR torch turned off |
| **RALLIED** | Entry state — sets last sound position to player location and immediately transitions to Stalk. | → STALK: on enter |

---

## Event System

All agent-to-agent and agent-to-player communication uses **C# Actions** defined in a central `GameEvents` static class. This avoids direct object references between agents (no `FindObjectOfType`, no hard coupling). Each action is subscribed to in `Enter()` / `OnEnable()` and unsubscribed in `Exit()` / `OnDisable()` to prevent memory leaks.

| Event (C# Action) | Fired by | Handled by |
|---|---|---|
| `OnSoundEmitted(Vector3, float)` | `SoundEmitter.Emit()` on player footsteps | Shade (Drift/Alert states) and Sentinel (Patrol state) |
| `OnTorchToggled(bool)` | `PlayerController` on F key | `TheShade` — triggers Retreat if lit and close |
| `OnSentinelRallyingCry(Vector3)` | `SentinelRallyingState` | `TheShade.OnRallyingCry()` — enters Rallied if within 10m |
| `OnShadeSharesPlayerPosition(Vector3)` | `ShadeStalkState` | `CryptSentinel.OnShadeSharedPosition()` — updates memory |
| `OnPlayerHitSentinel` | `PlayerAttack` on LMB hit | `CryptSentinel.OnHitByPlayer()` — triggers Staggered |
| `OnSentinelLowHP` | `CryptSentinel.TakeDamage()` | `CryptSentinel.OnLowHP()` — triggers Rallying |
| `OnShadeContactPlayer` | `ShadeHuntState` on contact | `PlayerController` — drains stamina |
| `OnPlayerCaught` | `SentinelCombatState.Attack()` | `GameManager` — shows game over screen |
| `OnPlayerEscaped` | `GoalZone.OnTriggerEnter()` | `GameManager` — shows win screen |

---

## Cross-Agent Interactions

- **Sentinel → Shade:** `RALLYING` state fires `OnSentinelRallyingCry`. Shade checks if within 10m and enters `RALLIED` → `STALK`.
- **Shade → Sentinel:** `STALK` state fires `OnShadeSharesPlayerPosition`. Sentinel updates its last known player position, potentially triggering `INVESTIGATE` if currently patrolling.
- **Player → Sentinel:** Entering the 60° vision cone triggers `INVESTIGATE` → `COMBAT`. Attacking the Sentinel fires `OnPlayerHitSentinel` → `STAGGERED`.
- **Player → Shade:** Footsteps every 0.4s fire `OnSoundEmitted`. Torch toggle fires `OnTorchToggled`. Shade goes to `RETREAT` if torch is on and player is within 5m.

---

## Sensory Systems

### Vision Cone (Crypt Sentinel)
- **FOV:** 60°, range 9m
- Three-step check each frame:
  1. Distance check — is the player within 9m?
  2. Angle check — is the player inside the 60° cone using `Vector3.Angle()`?
  3. Raycast — is line of sight clear? (`Physics.Raycast` — returns false if an obstacle blocks the path)
- Visualized at runtime by `VisionConeMesh.cs` — a procedural fan mesh rendered as a cyan arc (searching) or red arc (player detected), visible in the Game window during play

### Hearing (The Shade)
- Player emits footstep sounds every 0.4s while moving (`SoundEmitter.Emit`)
- Walking intensity: 0.6 — sneaking intensity: 0.15
- Detection range = `soundRadius × intensity` (8m × 0.6 = 4.8m for walking)
- Shade subscribes to `GameEvents.OnSoundEmitted` while in DRIFT or ALERT states

### Torch / Light Sensor (The Shade)
- Player toggles torch with the `F` key
- `GameEvents.OnTorchToggled` fires with a bool
- `TheShade.OnTorchToggled()` checks distance — if player is within `lightRadius` (5m), enters RETREAT immediately

---

## Player Interaction Scenario

**Scenario: The Chest Ambush**

1. Player opens a chest — `SoundEmitter.Emit` fires with intensity 0.8
2. Shade (in DRIFT) detects the sound, enters ALERT and moves toward the chest
3. Shade arrives at the sound source, enters STALK — shares position with Sentinel
4. Sentinel (patrolling) receives position, enters INVESTIGATE
5. Player walks away unaware — Shade detects movement within 3m, enters HUNT
6. Shade drains stamina on contact — slower player makes louder footsteps
7. Louder footsteps cross the Sentinel's hearing threshold — Sentinel confirms sight, enters COMBAT
8. Player hits Sentinel — STAGGERED for 1.5s (escape window)
9. Player lights torch — Shade enters RETREAT
10. Player uses the opening to exit — Sentinel eventually enters RETURN → PATROL; Shade DRIFTS again

**Player decisions driven by this scenario:**
- Torch is a double-edged tool: repels the Shade but makes the player visible to the Sentinel
- Sneaking reduces sound radius but also makes the player slower
- Staggering the Sentinel is risky if the Shade has been Rallied and is already hunting

---

## Challenges and Solutions

### 1. State leaking between transitions
**Problem:** Early versions had variables like timers being set inside the wrong state, so entering a state partway through a loop would carry stale values.  
**Solution:** Used the polymorphic FSM pattern (`Enter()`, `Execute()`, `Exit()`) — every state resets its own timers in `Enter()`, not in the calling state.

### 2. Event subscriptions causing double-triggers
**Problem:** The Shade and Sentinel were subscribing to `OnSoundEmitted` in `Awake` and never unsubscribing, so switching scenes or re-enabling objects caused multiple event listeners stacking.  
**Solution:** Subscriptions are now done exclusively in `Enter()` and removed in `Exit()` for states that need them. `LevelManager` handles agent-to-agent subscriptions and unsubscribes in `OnDisable()`.

### 3. NavMesh sampling for Shade's random drift
**Problem:** The Shade picking random points using `Random.insideUnitSphere` would sometimes pick positions off the NavMesh, causing the agent to freeze.  
**Solution:** Added `NavMesh.SamplePosition()` to find the nearest valid NavMesh point within a search radius before setting the destination.

### 4. Cross-agent coordination without tight coupling
**Problem:** Making the Sentinel notify the Shade (and vice versa) without them holding direct references to each other, to keep the code modular.  
**Solution:** Used a static `GameEvents` class with C# Actions (observer pattern from Module 2). Agents broadcast events; `LevelManager` wires the subscriptions so agents stay decoupled.

---

## Controls

| Key | Action |
|---|---|
| WASD | Move |
| Left Shift | Sneak (quieter footsteps) |
| F | Toggle Torch |
| Left Mouse | Attack Sentinel (triggers Stagger event) |

---

*Yatharth Chopra — GAME 10020 — Mohawk College*
