# 🏀 1v1 Basketball vs ML Agent

**1v1 Basketball vs ML Agent** is a 3D basketball game built in **Unity and C#**, featuring an opponent powered by **Unity ML-Agents**.

Compete for possession, steal the ball, and score baskets in a race to **21 points**. The project combines basketball gameplay with a reinforcement learning environment, exploring how rewards and observations influence an agent's decisions.

> Built with Unity 6, C#, and Unity ML-Agents.

---

## 🎮 Gameplay

Play a one-on-one match against an AI opponent that can move, rotate, shoot, and attempt steals.

- 🏆 **First to 21** — Reach the target score before your opponent.
- 🎯 **One- and two-point baskets** — Score one point at close range or two when shooting from at least 7 world units away, measured horizontally.
- 🔄 **Scorer keeps possession** — After a basket, the scorer restarts with the ball at a randomly selected possession start point.
- ⏱️ **12-second shot clock** — Take a shot before time runs out or lose possession.
- 🏀 **Automatic pickup and dribbling** — Collect a loose ball by touching it and dribble while holding possession.
- 🦘 **Jump shots** — Launch the ball along a calculated arc towards the hoop, with a jump when grounded.
- 🤏 **Stealing** — Challenge your opponent for the ball, with success influenced by distance and facing direction.

---

## ✨ Feedback and Match Flow

- **Live score counters** for the player and agent.
- **Possession status** showing who currently holds the ball.
- **Shot-clock display** to keep track of remaining possession time.
- **Green and red backboard feedback** for baskets and miss-trigger events.
- **Procedural net movement** when a basket is scored.
- **Main menu and winner panel**, with restart and return-to-menu actions.

---

## 🕹️ Controls

| Input | Action |
| --- | --- |
| **W / A / S / D or arrow keys** | Move |
| **Mouse** | Turn and look around |
| **Left mouse button** | Shoot while holding the ball |
| **F** | Attempt to steal |
| **Escape** | Toggle cursor lock and visibility |
| **Touch a loose ball** | Pick it up automatically |

Shots use a calculated trajectory towards the assigned hoop target. Escape releases or locks the cursor; it does not pause the match.

---

## 🧠 Machine Learning

The basketball opponent uses **reinforcement learning**, supported by observations, actions, and rewards defined in the gameplay code.

### 👀 Agent Observations

The agent receives **37 vector observations**, including:

- Its position and velocity.
- The ball's position and velocity.
- Hoop and opponent positions.
- Directions and distances to relevant objects.
- Player and agent possession states.
- Alignment with the hoop and shooting-zone information.
- Time spent holding the ball.

### ⚙️ Agent Actions

| Action type | Purpose |
| --- | --- |
| **3 continuous actions** | Forward/backward movement, strafing, and rotation |
| **1 discrete branch** | Choose between no additional action, shooting, or stealing |

**Action masking** prevents the agent from selecting a shot without possession or a steal when the player does not hold the ball.

### 🎯 Rewards and Penalties

Rewards encourage the agent to:

- Collect and approach a loose ball.
- Face the hoop and occupy useful shooting positions.
- Score baskets and successfully steal possession.
- Stay near the player while defending.

Penalties discourage rushed or contested shots, poor shot selection, turnovers, and holding the ball too long. A separate possession timer forces a shot after the agent's configured hold limit.

The opponent therefore combines **learned decisions with explicit gameplay rules**.

### 🔄 Episode Management

The environment resets play after baskets, shot-clock violations, shot timeouts, loose-ball timeouts, episode timeouts, or the ball falling below the arena.

Match scores carry over between these resets until the match ends or is restarted.

---

## 📊 Training Configuration

[`Assets/MLAgent.yaml`](Assets/MLAgent.yaml) contains a **PPO — Proximal Policy Optimization** configuration for the `MLAgent` behaviour.

| Setting | Value |
| --- | --- |
| **Hidden layers** | 2 |
| **Hidden units per layer** | 128 |
| **Observation normalization** | Enabled |
| **Learning rate** | 0.0003 |
| **Batch size** | 64 |
| **Buffer size** | 12,000 |
| **Maximum steps** | 1,000,000 |

These are the supplied configuration values, rather than a verified training duration or performance result. The repository also includes exported **ONNX models** and previous training outputs.

---

## 🛠️ Technical Highlights

- **C# gameplay systems** for movement, possession, shooting, and scoring.
- **Rigidbody interactions** and calculated ballistic shot trajectories.
- **Procedural dribbling** and net feedback.
- **Probabilistic stealing** based on distance and facing direction.
- **ML-Agents observations, action masking, and reward shaping**.
- **Environment resets** that support repeated training episodes within a match.

### 📂 Main Scripts

| Script | Purpose |
| --- | --- |
| `BasketballAgent.cs` | Agent observations, actions, rewards, movement, shooting, and stealing |
| `PlayerBasketballController.cs` | Player movement, camera controls, shooting, and stealing |
| `BasketballBall.cs` | Ownership, dribbling, ball release, shooter tracking, and resets |
| `BasketballEnvController.cs` | Scores, possession, shot clock, episode resets, and match UI |
| `HoopScoreTrigger.cs` | Basket detection and duplicate-score protection |
| `BackboardFeedback.cs` | Backboard colour feedback |
| `MissTrigger.cs` | Miss-trigger feedback |
| `NetReaction.cs` | Net wobble and squash after a basket |
| `MainMenuManager.cs` | Start-game and quit actions |

The basketball scripts are in [`Assets/Scripts`](Assets/Scripts). The separate `MLAgent.cs` script and Penguin example content are learning examples rather than the basketball opponent.

---

## 🚀 Running the Project

1. **Clone or download** the repository.
2. Add the project folder in **Unity Hub**.
3. Open it with **Unity 6000.0.32f1**.
4. Allow Unity to import assets and resolve packages.
5. Open **`Assets/Scenes/MainMenu.unity`**.
6. Press **Play** and start the game.

You can also open **`Assets/Scenes/MLBasketballArena.unity`** directly to inspect the arena.

The project uses **Unity ML-Agents 3.0.0** and **Universal Render Pipeline 17.0.3**. The basketball agent prefab references an included ONNX model in `Assets/AgentONX`.

For standalone builds, the enabled scenes are **MainMenu**, followed by **MLBasketballArena**.

---

## 💡 Project Focus

This project explores the connection between **gameplay programming and reinforcement learning**: building interactive mechanics, defining useful observations and rewards, and coordinating AI episodes with an ongoing basketball match.

It is a portfolio prototype, with agent behaviour dependent on the selected model and scene configuration. The repository also contains third-party art and example content alongside the basketball gameplay implementation.
