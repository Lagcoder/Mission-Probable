# Mission-Probable
A to do list, but fun.

## Overview
Mission-Probable turns your real-world to-do list into a game. Type your tasks, pick a mode, and survive!

## Game Modes

### Mode 1 – Trap Race
All tasks race toward a finish line. The **first** task to cross is the one you **must** complete.
- Use the **Trap** button (or click a task) to freeze and damage the leading task.
- Every `~20 seconds` unpunished tasks **rank up**: more health, faster speed, and a new colour.
- Rank colours: Light Blue → Green → Yellow → Orange → Red → Purple → Black.

### Mode 2 – Battle
Click tasks to attack them in real-time.
- **Critical hits** – 20% chance for 2.2× damage.
- **Burn DoT** – loot perk that adds damage-over-time to every attack.
- **Slow Aura** – loot perk that reduces all task speeds.
- **Block Chance** – tasks can block hits; upgradeable via loot.
- **Roguelike Loot** – killing a task may drop: Damage Boost, Burn DoT, Slow Aura, Block Chance, Heal on Kill, Trap Upgrade, or Spawn Slowdown.
- **Time Budgets** – Short (10 min), Medium (30 min), Long (60 min), Very Long (unlimited).
  - Killing a task grants **+5 bonus minutes**.
  - If a task reaches you and you haven't killed it, your HP drains up to **20 HP per task** while it lingers.
- You start with **50 HP**.

### Mode 3 – Cooking Tower
Each task is a coloured ingredient block that swings back and forth and falls.
- **Tap / click Drop** at the right moment to land it cleanly on the tower.
- A **clean layer** banks **+2 bonus minutes** and grows the tower.
- A **miss** forces you to complete that task immediately (no bonus).
- The landing platform **narrows** with each clean layer — stay sharp!

## Scripts

| File | Purpose |
|------|---------|
| `Assets/Scripts/TaskItem.cs` | Task entity: health, rank tiers, timer, damage, burn, freeze, overtime |
| `Assets/Scripts/TaskSpawner.cs` | Instantiates, positions, and cleans up `TaskItem` objects |
| `Assets/Scripts/GameManager.cs` | Orchestrates all three modes, loot, HUD, win/lose conditions |
| `Assets/Scripts/MenuController.cs` | Credits and How-to-Play panel navigation |
| `Assets/Scripts/CookingTowerMinigame.cs` | Block-stacking / pendulum minigame for Mode 3 |

## Unity Setup Guide

### Requirements
- Unity 2021 LTS or later.
- Use the **Legacy UI** (Canvas / UnityEngine.UI) — no TextMeshPro required.

### Scene layout

#### Canvas (Screen Space – Overlay)
Set **Canvas Scaler** to *Scale with Screen Size*, reference 1920×1080.

#### MenuPanel
Enable by default. Contains:
- `InputField` – **TodoInput** (multi-line, placeholder "Enter tasks separated by comma or new line")
- `Dropdown` – **ModeDropdown** (options: `Trap Race`, `Battle`, `Cooking Tower`)
- `Dropdown` – **BudgetDropdown** (options: `Short (10m)`, `Medium (30m)`, `Long (60m)`, `Very Long`)
- `Button` – **StartButton**
- `Button` – **CreditsButton**
- `Button` – **HowToPlayButton**

#### CreditsPanel / HowToPlayPanel
Disabled by default. Each needs a **Close** button.

#### HUDPanel
Disabled by default. Contains:
- `Text` – **StatusText**
- `Text` – **HealthText** (Mode 2)
- `Text` – **TimerText** (Mode 2 global countdown)
- `Text` – **ModeLabel**
- `Text` – **DamageBoostText** (Mode 2 loot display)
- `Text` – **LootLogText** (Mode 2 loot drop log)
- `Button` – **TrapButton** (Mode 1; hidden in other modes)
- `Text` – **TrapCooldownText**
- `Button` – **BackToMenuButton**
- `RectTransform` **TaskParent** – `VerticalLayoutGroup` container for spawned tasks.

#### TaskItem Prefab
Create a UI Panel prefab with:
- Root `Button` component (so clicking it fires `OnClicked`).
- Child `Image` (name it `Background`) – set as `Image Type = Simple`.
- Child `Image` (name it `HealthBarFill`) – `Image Type = Filled`, Fill Method = Horizontal.
- Child `Image` (name it `ProgressBarFill`) – `Image Type = Filled`, Fill Method = Horizontal (for Mode 1).
- Child `Text` – **LabelText**.
- Child `Text` – **HealthText**.
- Child `Text` – **TimerText**.
- Attach `TaskItem` component; wire all `[SerializeField]` fields.

#### CookingTowerPanel (for Mode 3)
Inside HUDPanel:
- `RectTransform` **TowerParent** – anchor bottom-centre; this holds placed blocks.
- `RectTransform` **FallingBlock** – the actively-dropping block (Image + Text child).
- `Text` – **FallingLabel** (child of FallingBlock).
- `Text` – **ResultText**.
- `Text` – **BonusTimeText**.
- `Text` – **TowerHeightText**.
- `Button` – **DropButton**.
- Attach `CookingTowerMinigame` component; wire all fields.

#### Systems GameObject
Empty GameObject with:
- `GameManager` – wire **all** serialised fields.
- `TaskSpawner` – assign `taskPrefab` and `taskParent` (TaskParent RectTransform).
- `MenuController` – wire panel and button references.

### Quick-start
1. Create the scene layout above.
2. Assign all serialised references in the Inspector.
3. Hit **Play**, type a few tasks (e.g. `Do laundry, Send email, Call mum`), choose a mode and budget, then click **Start**.

## Balancing Knobs (Inspector)

| Field | Default | Effect |
|-------|---------|--------|
| `trapCooldown` | 8 s | Seconds between trap uses (Mode 1) |
| `trapDamage` | 15 | Damage dealt by each trap |
| `rankUpInterval` | 20 s | How often unchallenged tasks rank up |
| `playerBaseDamage` | 20 | Base click damage (Mode 2) |
| `critChance` | 0.20 | 20 % crit chance |
| `critMultiplier` | 2.2 | Crit damage multiplier |
| `bonusTimePerKill` | 300 s | +5 min reward per kill |
| `spawnIntervalBattle` | 4 s | Time between new task spawns (Mode 2) |
| `bonusPerLayer` | 120 s | +2 min per clean cooking layer |

