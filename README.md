# 🏀 1v1 Basketball vs ML Agent

A **3D single-player basketball game** built with **Unity and C#**, featuring an opponent powered by **Unity ML-Agents**. Compete for possession, steal the ball, and score baskets in a race to **21 points**.

The project combines basketball gameplay systems with a reinforcement learning environment, exploring agent observations, action selection, reward shaping, and episode resets.

---

## 🎮 Gameplay

- **One-on-one basketball:** Play against an ML agent that can move, rotate, shoot, and attempt steals.
- **One- and two-point scoring:** Baskets are worth one point at close range or two points when released at least 7 world units from the hoop, measured horizontally.
- **Winner stays on offence:** The scorer keeps possession, with play restarting from a randomly selected possession start point.
- **12-second shot clock:** Running out of time transfers possession to the opponent.
- **Automatic pickup and dribbling:** Colliding with a loose ball collects it; a procedural bounce follows its owner while held.
- **Jump shots:** Shooting calculates a ballistic arc towards an assigned hoop target and adds a jump when grounded.
- **Stealing:** Success depends on distance and facing direction, with a cooldown between attempts.
- **Feedback and UI:** Score counters, possession status, shot clock, green/red backboard feedback, and a procedural net reaction.
- **Match flow:** Main menu, winner panel, restart, and return-to-menu actions.

---

## 🕹️ Controls

| Input | Action |
| --- | --- |
| W / A / S / D or arrow keys | Move |
| Mouse | Turn and look around |
| Left mouse button | Shoot while holding the ball |
| F | Attempt to steal |
| Escape | Toggle cursor lock and visibility |
| Touch a loose ball | Pick it up automatically |

Shots target the assigned hoop; this is not a free-aim or hold-to-charge shooting system. Escape toggles the cursor rather than pausing the match.

---

## 🧠 AI and Reinforcement Learning

The basketball agent uses a mixture of continuous and discrete actions:

| Action type | Purpose |
| --- | --- |
| 3 continuous actions | Forward/backward movement, strafing, and rotation |
| 1 discrete branch with 3 options | No additional action, shoot, or steal |

The agent collects **37 vector observations**, including its position and velocity, ball position and velocity, hoop and opponent positions, relative directions, possession state, distances, facing alignment, shooting-zone flags, and elapsed hold time.

**Action masking** disables shooting when the agent does not own the ball and disables stealing when the player does not own it.

### 🎯 Reward Shaping

Rewards encourage collecting the ball, approaching a loose ball, facing the hoop, occupying useful shooting positions, scoring, stealing, and staying near the player when defending.

Penalties discourage rushed or contested shots, poor shooting positions, turnovers, and holding possession too long. A separate hold timer forces the agent to shoot after its configured limit. This combines learned decisions with explicit gameplay rules.

The environment ends episodes and resets play after baskets, shot-clock violations, shot timeouts, loose-ball timeouts, episode timeouts, or the ball falling below the arena. Match scores persist across these resets until the match ends or is restarted.

### 📊 Training Configuration

[`Assets/MLAgent.yaml`](Assets/MLAgent.yaml) defines a **PPO (Proximal Policy Optimization)** trainer for the `MLAgent` behaviour:

| Setting | Value |
| --- | --- |
| Hidden layers | 2 |
| Hidden units per layer | 128 |
| Observation normalization | Enabled |
| Learning rate | 0.0003 |
| Batch size | 64 |
| Buffer size | 12,000 |
| Maximum steps | 1,000,000 |

These values describe the supplied configuration, not a verified training duration or performance result. The repository also contains exported ONNX models and previous training outputs.

---

## 📂 Code Overview

The basketball scripts are in [`Assets/Scripts`](Assets/Scripts).

| Script | Responsibility |
| --- | --- |
| `BasketballAgent.cs` | Observations, actions, movement, shooting, stealing, action masking, and rewards |
| `PlayerBasketballController.cs` | Player movement, mouse look, shooting, stealing, and cursor control |
| `BasketballBall.cs` | Ownership, procedural dribbling, physics release, shooter tracking, and ball resets |
| `BasketballEnvController.cs` | Scoring, possession, shot clock, episode resets, match state, and UI |
| `HoopScoreTrigger.cs` | Basket detection, downward-motion validation, and duplicate-score protection |
| `BackboardFeedback.cs` | Temporary score/miss colour changes |
| `MissTrigger.cs` | Miss-trigger feedback |
| `NetReaction.cs` | Procedural net wobble and squash after a basket |
| `MainMenuManager.cs` | Start-game and quit actions |

`MLAgent.cs` is a separate target-reaching example, not the basketball opponent. The repository also contains Penguin example content alongside the basketball project.

---

## 🚀 Opening the Project

1. Clone or download this repository.
2. Add its root folder in Unity Hub.
3. Open it with **Unity 6000.0.32f1**, the version recorded in `ProjectSettings/ProjectVersion.txt`.
4. Allow Unity to import assets and resolve the included package manifest.
5. Open `Assets/Scenes/MainMenu.unity` and enter Play mode, or open `Assets/Scenes/MLBasketballArena.unity` to inspect the arena directly.

The package manifest specifies **Unity ML-Agents 3.0.0** and **Universal Render Pipeline 17.0.3**. The basketball agent prefab references an included ONNX model in `Assets/AgentONX`.

For standalone builds, the project lists `MainMenu` followed by `MLBasketballArena` as enabled scenes.

The repository includes third-party art and example content; these should be distinguished from the basketball gameplay implementation. Agent performance depends on the selected model and scene configuration.
