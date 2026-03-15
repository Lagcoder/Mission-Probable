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

### Inspector Wiring Reference

Use these tables as a checklist. For each component, select the listed GameObject in the Hierarchy, then drag the target object into the matching slot in the Inspector.

---

#### Systems GameObject – `GameManager` component

| Inspector Header | Field | Drag this in |
|-----------------|-------|--------------|
| Core References | Spawner | `TaskSpawner` component on **Systems** |
| Core References | Cooking Minigame | `CookingTowerMinigame` component on **Systems** |
| Menu UI | Menu Panel | **MenuPanel** GameObject |
| Menu UI | Todo Input | `InputField` (**TodoInput**) inside MenuPanel |
| Menu UI | Mode Dropdown | `Dropdown` (**ModeDropdown**) inside MenuPanel |
| Menu UI | Budget Dropdown | `Dropdown` (**BudgetDropdown**) inside MenuPanel |
| Menu UI | Start Button | `Button` (**StartButton**) inside MenuPanel |
| HUD UI | Hud Panel | **HUDPanel** GameObject |
| HUD UI | Status Text | `Text` (**StatusText**) inside HUDPanel |
| HUD UI | Health Text | `Text` (**HealthText**) inside HUDPanel |
| HUD UI | Timer Text | `Text` (**TimerText**) inside HUDPanel |
| HUD UI | Mode Label | `Text` (**ModeLabel**) inside HUDPanel |
| HUD UI | Trap Button | `Button` (**TrapButton**) inside HUDPanel |
| HUD UI | Trap Cooldown Text | `Text` (**TrapCooldownText**) inside HUDPanel |
| HUD UI | Back To Menu Button | `Button` (**BackToMenuButton**) inside HUDPanel |
| Battle HUD | Damage Boost Text | `Text` (**DamageBoostText**) inside HUDPanel |
| Battle HUD | Loot Log Text | `Text` (**LootLogText**) inside HUDPanel |

> **Dropdown option order matters** – the script reads `value` as an index.
> - **ModeDropdown** options must be (index 0 → 2): `Trap Race`, `Battle`, `Cooking Tower`
> - **BudgetDropdown** options must be (index 0 → 3): `Short (10m)`, `Medium (30m)`, `Long (60m)`, `Very Long`

---

#### Systems GameObject – `TaskSpawner` component

| Inspector Header | Field | Drag this in |
|-----------------|-------|--------------|
| Prefab & Parent | Task Prefab | **TaskItem** prefab asset (from the Project window) |
| Prefab & Parent | Task Parent | `RectTransform` (**TaskParent**) inside HUDPanel |

---

#### Systems GameObject – `MenuController` component

| Inspector Header | Field | Drag this in |
|-----------------|-------|--------------|
| Panels | Main Menu Panel | **MenuPanel** GameObject |
| Panels | Credits Panel | **CreditsPanel** GameObject |
| Panels | How To Play Panel | **HowToPlayPanel** GameObject |
| Main Menu Buttons | Credits Button | `Button` (**CreditsButton**) inside MenuPanel |
| Main Menu Buttons | How To Play Button | `Button` (**HowToPlayButton**) inside MenuPanel |
| Credits Panel | Close Credits Button | `Button` (**CloseCreditsButton**) inside CreditsPanel |
| How-to-Play Panel | Close How To Play Button | `Button` (**CloseHowToPlayButton**) inside HowToPlayPanel |

---

#### Systems GameObject – `CookingTowerMinigame` component

| Inspector Header | Field | Drag this in |
|-----------------|-------|--------------|
| UI References | Tower Parent | `RectTransform` (**TowerParent**) inside CookingTowerPanel |
| UI References | Falling Block | `RectTransform` (**FallingBlock**) inside CookingTowerPanel |
| UI References | Falling Label | `Text` (**FallingLabel**) — child of FallingBlock |
| UI References | Result Text | `Text` (**ResultText**) inside CookingTowerPanel |
| UI References | Bonus Time Text | `Text` (**BonusTimeText**) inside CookingTowerPanel |
| UI References | Tower Height Text | `Text` (**TowerHeightText**) inside CookingTowerPanel |
| UI References | Drop Button | `Button` (**DropButton**) inside CookingTowerPanel |
| UI References | Cooking Panel | **CookingTowerPanel** GameObject (child of HUDPanel) |

---

#### TaskItem Prefab – `TaskItem` component

Open the prefab in the Project window, then wire these fields on its `TaskItem` component:

| Field | Drag this in |
|-------|--------------|
| Label Text | `Text` (**LabelText**) — child of the prefab root |
| Health Text | `Text` (**HealthText**) — child of the prefab root |
| Timer Text | `Text` (**TimerText**) — child of the prefab root |
| Background Image | `Image` (**Background**) — child of the prefab root |
| Health Bar Fill | `Image` (**HealthBarFill**) — child of the prefab root |
| Progress Bar Fill | `Image` (**ProgressBarFill**) — child of the prefab root |
| Click Button | `Button` on the **root** of the prefab |

---

### Quick-start
1. Build the scene hierarchy described above.
2. Wire every Inspector field using the tables in **Inspector Wiring Reference**.
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

