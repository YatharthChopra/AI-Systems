# Hollow Vault — AI Design Document
**Assignment 3: AI System Implementation**  
**Course: Game Design — Autonomous Agents**

---

## Game Concept

Hollow Vault is a top-down 3D action game set in an underground cursed treasury. The player (a relic thief) navigates vault rooms, loots relics, and tries to escape alive. Two AI agents guard the vault: the **Crypt Sentinel** and **The Shade**. They interact with each other as well as the player, creating overlapping threat dynamics.

---

## State Machine Diagrams

### Crypt Sentinel

```
         ┌─────────────────────────────────────────────┐
         │                  PATROL                     │◄──── initial state
         │  Walks waypoints. Vision cone active.       │
         └───────────────┬─────────────────────────────┘
                         │ hears sound / sees player
                         ▼
         ┌─────────────────────────────────────────────┐
         │               INVESTIGATE                   │
         │  Moves to last known position.              │
         └───────────┬─────────────┬───────────────────┘
          no target  │             │ confirms sighting
          found (4s) │             ▼
                     │  ┌──────────────────────────────┐
                     │  │          COMBAT               │
                     │  │  Charges + attacks at range.  │
                     │  └──┬───────────────────┬────────┘
                     │     │ hit by player      │ target lost > 4s
                     │     ▼                   ▼
                     │  ┌──────────┐    ┌──────────────┐
                     │  │STAGGERED │    │    RETURN     │
                     │  │ 1.5s     │    │ Back to start │
                     │  └────┬─────┘    └──────┬────────┘
                     │       │ recover          │ waypoint reached
                     │       └──► COMBAT        └──► PATROL
                     │
                     └──► PATROL
```

**Sentinel — Transitions table:**

| From | To | Condition |
|------|----|-----------|
| Patrol | Investigate | Hears sound OR sees player in vision cone |
| Investigate | Combat | Player confirmed in vision cone |
| Investigate | Patrol | Arrives at location, memory timer (4s) expires |
| Combat | Staggered | Player lands a hit (`OnPlayerHitSentinel` event) |
| Combat | Rallying | HP drops below 35% (`OnSentinelLowHP` event) |
| Combat | Return | Sight lost for > 4 seconds |
| Staggered | Combat | Stagger duration (1.5s) expires |
| Rallying | Combat | Rallying Cry fired, Shade alerted |
| Return | Patrol | Reaches first waypoint |

---

### The Shade

```
         ┌─────────────────────────────────────────────┐
         │                  DRIFT                      │◄──── initial state
         │  Wanders perimeter. Sound sensor active.    │
         └───────────────┬─────────────────────────────┘
                         │ sound detected
                         ▼
         ┌─────────────────────────────────────────────┐
         │                  ALERT                      │
         │  Orients toward sound source.               │
         └───────────┬────────────────┬────────────────┘
         sound fades │                │ location confirmed
         (3s)        │                ▼
                     │  ┌──────────────────────────────┐
                     │  │           STALK              │
                     │  │  Silently approaches source. │◄──── entered via RALLIED
                     │  └──────┬──────────────┬────────┘
                     │         │ player within │ location lost
                     │         │ 3m            └──► DRIFT
                     │         ▼
                     │  ┌──────────────────────────────┐
                     │  │           HUNT               │
                     │  │  Locks on, drains stamina.   │
                     │  └──────┬──────────────┬────────┘
                     │         │ torch lit     │ player escapes > 5m
                     │         ▼               └──► STALK
                     │  ┌──────────────────────────────┐
                     │  │          RETREAT             │
                     │  │  Flees to shadow.            │
                     │  └──────┬──────────────────────┘
                     │         │ safe shadow / torch off
                     │         └──► DRIFT
                     │
                     └──► DRIFT

  * RALLIED entered via Sentinel RALLYING CRY (agent-agent interaction)
    Immediately transitions to STALK toward player position.
```

**Shade — Transitions table:**

| From | To | Condition |
|------|----|-----------|
| Drift | Alert | Sound detected within sound radius |
| Alert | Stalk | Shade arrives at sound origin |
| Alert | Drift | Sound fades after 3 seconds |
| Stalk | Hunt | Player within 3m |
| Stalk | Drift | Arrives at location, player not found |
| Hunt | Retreat | Torch lit within 5m |
| Hunt | Stalk | Player escapes beyond 5m |
| Retreat | Drift | Safe shadow reached OR torch extinguished |
| Rallied | Stalk | Immediately on entry (summoned by Sentinel) |

---

## State Behaviours

### Crypt Sentinel

- **Patrol** — Loops through scene waypoints at patrol speed (1.8 m/s). Vision cone (60°, 9m) is raycasted every frame. Subscribes to `OnSoundEmitted` to react to nearby noise.
- **Investigate** — Moves to last known position at patrol speed. If the player is spotted en route, escalates immediately to Combat. Memory timer counts down once arrived; if player is not found, returns to Patrol.
- **Combat** — Charges at chase speed (3.2 m/s) directly toward the player. Updates last known position every frame the player is visible. Performs a melee attack when within 1.5m. Starts a memory countdown when sight is lost.
- **Staggered** — Stops completely for 1.5 seconds after being hit. Gives the player an opening window to flee or reposition before the Sentinel recovers into Combat.
- **Rallying** — Fires `OnSentinelRallyingCry` event with its world position. The Shade listens and enters Rallied if within 10m. Sentinel immediately returns to Combat after the cry.
- **Return** — Walks back to waypoint 0 at patrol speed. Resets patrol index so the full route starts over on arrival.

### The Shade

- **Drift** — Wanders slowly around the room using random NavMesh sampling. Sound sensor (`OnSoundEmitted`) is active throughout.
- **Alert** — Moves toward the confirmed sound origin. Fade timer counts down simultaneously; transitions to Stalk on arrival or Drift if time runs out.
- **Stalk** — Moves silently toward the last sound position. Fires `OnShadeSharesPlayerPosition` on entry so the Sentinel can update its memory. Transitions to Hunt when player is within 3m.
- **Hunt** — Chases the player at full speed (4.5 m/s). Fires `OnShadeContactPlayer` every frame on contact, which triggers stamina drain on the player. Drops back to Stalk if player exceeds 5m.
- **Retreat** — Flees directly away from the player when exposed to torchlight. Listens for `OnTorchToggled` so it can cancel the retreat if the torch is extinguished early.
- **Rallied** — Entry-only state: sets last sound position to player's current location and immediately transitions to Stalk, giving the Shade a direct path to the player.

---

## Sensory Systems

### Sentinel — Vision Cone
- 60° field of view, 9m range
- Implemented in `VisionCone.cs`: distance check → angle check → `Physics.Raycast` to confirm no wall is blocking line of sight
- Active in Patrol and Combat states; Investigate also checks it every frame

### Shade — Sound Radius
- 8m sphere checked against `OnSoundEmitted` events
- Any game object can emit a sound by calling `SoundEmitter.Emit(position, intensity)`
- Intensity scales the effective hearing range (0.0 = silent, 1.0 = maximum range)
- Player footsteps fire automatically via `PlayerController` based on movement speed and sneak state

### Shade — Light Sensor
- 5m radius check when `OnTorchToggled` fires
- Immediate `Retreat` transition if the torch turns on and the Shade is within range
- Handled directly in `TheShade.OnTorchToggled()`

---

## Agent-Agent Interaction

The most important cross-agent mechanic is the **Rallying Cry**:

1. Sentinel HP drops below 35% → fires `OnSentinelLowHP`
2. Sentinel enters `Rallying` state → fires `OnSentinelRallyingCry` with its world position
3. Shade receives event in `TheShade.OnRallyingCry()` → checks distance (≤ 10m) → enters `Rallied`
4. Rallied immediately transitions to `Stalk` toward the player's current position

The reverse also applies: when Shade stalks and confirms a player position, it fires `OnShadeSharesPlayerPosition`, which updates the Sentinel's `lastKnownPlayerPos` and can pull a patrolling Sentinel into Investigate.

All cross-agent communication is done exclusively through `GameEvents` C# Actions. Neither agent holds a direct reference to the other — `LevelManager` is the only place where subscriptions are connected.

---

## Challenges and Solutions

**Challenge 1: Event unsubscription and ghost listeners**  
Early testing showed states were still responding to sound events after transitioning. The fix was ensuring every `Enter()` that subscribes also has a matching `Exit()` unsubscription. The pattern is consistent across all states that use events.

**Challenge 2: NavMesh agent carrying speed between states**  
The Sentinel's charge speed (3.2 m/s) was persisting into Staggered and Return states because speed was only set on entry, not reset on exit. Adding an `Exit()` reset to `SentinelCombatState` solved this.

**Challenge 3: Agent-agent decoupling**  
Initially the Sentinel held a direct reference to the Shade to call methods on it. This was replaced with the `GameEvents` system so neither agent needs to know about the other. `LevelManager` owns all subscriptions and cleans them up in `OnDisable()`.

**Challenge 4: Shade drifting off NavMesh**  
Random wandering in `ShadeDriftState` initially caused the agent to try pathing to invalid positions. Adding a `NavMesh.SamplePosition()` check before calling `SetDestination()` ensures the destination is always on a valid NavMesh surface.

---

## Gameplay Video

> Link to be added after recording — upload to YouTube as unlisted/public.

---

## Project Structure

```
Assets/Scripts/
├── FSM/
│   └── State.cs               — abstract base class all states inherit from
├── Events/
│   └── GameEvents.cs          — all C# Action events (decoupled pub/sub)
├── Sensors/
│   ├── VisionCone.cs          — raycasted vision cone for Sentinel
│   └── SoundEmitter.cs        — static helper to fire sound events
├── Agents/
│   ├── CryptSentinel.cs       — Sentinel MonoBehaviour, owns FSM
│   ├── SentinelPatrolState.cs
│   ├── SentinelInvestigateState.cs
│   ├── SentinelCombatState.cs
│   ├── SentinelStaggeredState.cs
│   ├── SentinelRallyingState.cs
│   ├── SentinelReturnState.cs
│   ├── TheShade.cs            — Shade MonoBehaviour, owns FSM
│   ├── ShadeDriftState.cs
│   ├── ShadeAlertState.cs
│   ├── ShadeStalkState.cs
│   ├── ShadeHuntState.cs
│   ├── ShadeRetreatState.cs
│   └── ShadeRalliedState.cs
├── Player/
│   └── PlayerController.cs    — movement, torch, stamina, footstep sounds
└── Level/
    └── LevelManager.cs        — connects all events on scene load
```
