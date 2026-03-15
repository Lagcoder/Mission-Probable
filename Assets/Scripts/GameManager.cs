using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Core game orchestrator.
///
/// Mode 1 – Trap Race:
///   All tasks race toward a finish line. The first to finish is the task you
///   MUST do. Player places traps (buttons) to freeze/damage tasks they don't
///   want to win. Unfinished tasks increase in rank (speed/health) over time.
///
/// Mode 2 – Battle:
///   Click tasks to attack them. Combat has crits, burn DoT, slow aura, block
///   chance, and roguelike loot drops. Time budgets track how long you have;
///   overtime drains your HP. You start at 50 HP.
///
/// Mode 3 – Cooking / Stacking:
///   Delegates to CookingTowerMinigame component.
/// </summary>
public class GameManager : MonoBehaviour
{
    // ── Mode enum ─────────────────────────────────────────────────────────────
    public enum GameMode { TrapRace = 0, Battle = 1, Cooking = 2 }

    // ── Inspector references ──────────────────────────────────────────────────
    [Header("Core References")]
    [SerializeField] private TaskSpawner           spawner;
    [SerializeField] private CookingTowerMinigame  cookingMinigame;

    [Header("Menu UI")]
    [SerializeField] private GameObject  menuPanel;
    [SerializeField] private InputField  todoInput;
    [SerializeField] private Dropdown    modeDropdown;
    [SerializeField] private Dropdown    budgetDropdown;   // Short/Med/Long/VeryLong
    [SerializeField] private Button      startButton;

    [Header("HUD UI")]
    [SerializeField] private GameObject  hudPanel;
    [SerializeField] private Text        statusText;
    [SerializeField] private Text        healthText;       // Mode 2
    [SerializeField] private Text        timerText;        // Mode 2 global countdown
    [SerializeField] private Text        modeLabel;
    [SerializeField] private Button      trapButton;       // Mode 1 trap activation
    [SerializeField] private Text        trapCooldownText; // Mode 1 trap cooldown
    [SerializeField] private Button      backToMenuButton;

    [Header("Battle HUD")]
    [SerializeField] private Text        damageBoostText;
    [SerializeField] private Text        lootLogText;

    [Header("Mode 1 Config")]
    [SerializeField] private float trapCooldown  = 8f;    // seconds between traps
    [SerializeField] private float trapDamage    = 15f;   // damage dealt by trap
    [SerializeField] private float trapFreezeSec = 2.5f;  // freeze duration
    [SerializeField] private float rankUpInterval = 20f;  // seconds before unpunished tasks rank up

    [Header("Mode 2 Config")]
    [SerializeField] private float playerBaseDamage = 20f;
    [SerializeField] private float critChance        = 0.20f;
    [SerializeField] private float critMultiplier    = 2.2f;
    [SerializeField] private float bonusTimePerKill  = 300f;  // 5 minutes in seconds

    [Header("Mode 2/3 Task Spawn")]
    [SerializeField] private float spawnIntervalBattle   = 4f;
    [SerializeField] private float spawnIntervalCooking  = 5f;

    // ── Private state ─────────────────────────────────────────────────────────
    private GameMode  _mode;
    private bool      _running;
    private List<string> _tasks = new List<string>();

    // Mode 1
    private float _trapTimer;
    private bool  _trapReady = true;
    private float _rankUpTimer;

    // Mode 2
    private float _playerHP       = 50f;
    private float _playerMaxHP    = 50f;
    private float _globalTimeLeft;  // overall budget for the current battle session
    private bool  _timerRunning;
    private float _playerDamageBoost  = 1f;
    private float _playerBurnDPS      = 0f;
    private float _playerSlowMult     = 1f;
    private float _playerBlockChance  = 0f;
    private float _playerHealOnKill   = 0f;
    private float _spawnTimer;
    private int   _killCount;
    private TaskItem.TimeBudget _selectedBudget = TaskItem.TimeBudget.Medium;

    // ── Unity lifecycle ───────────────────────────────────────────────────────
    private void Start()
    {
        startButton.onClick.AddListener(StartGame);
        if (backToMenuButton != null)
            backToMenuButton.onClick.AddListener(ShowMenu);
        if (trapButton != null)
            trapButton.onClick.AddListener(ActivateTrap);

        spawner.OnTaskFinished += OnTaskFinished;
        spawner.OnTaskClicked  += OnTaskClicked;

        ShowMenu();
    }

    private void Update()
    {
        if (!_running) return;

        switch (_mode)
        {
            case GameMode.TrapRace:  UpdateTrapRace();  break;
            case GameMode.Battle:    UpdateBattle();    break;
            // Cooking is handled by CookingTowerMinigame
        }
    }

    // ── Start / Stop ─────────────────────────────────────────────────────────

    private void StartGame()
    {
        ParseTasks();
        if (_tasks.Count == 0)
        {
            if (statusText != null) statusText.text = "Please enter at least one task!";
            return;
        }

        _mode = (GameMode)modeDropdown.value;

        // Budget selection
        int budgetIdx = budgetDropdown != null ? budgetDropdown.value : 1;
        TaskItem.TimeBudget[] budgets =
        {
            TaskItem.TimeBudget.Short,
            TaskItem.TimeBudget.Medium,
            TaskItem.TimeBudget.Long,
            TaskItem.TimeBudget.VeryLong,
        };
        _selectedBudget = budgets[Mathf.Clamp(budgetIdx, 0, budgets.Length - 1)];

        spawner.CleanupAll();
        menuPanel.SetActive(false);
        hudPanel.SetActive(true);
        _running = true;

        switch (_mode)
        {
            case GameMode.TrapRace:  InitTrapRace();  break;
            case GameMode.Battle:    InitBattle();    break;
            case GameMode.Cooking:   InitCooking();   break;
        }

        UpdateHUD();
    }

    private void ShowMenu()
    {
        _running = false;
        spawner.CleanupAll();
        if (cookingMinigame != null) cookingMinigame.EndGame();
        menuPanel.SetActive(true);
        hudPanel.SetActive(false);
    }

    // ── Mode 1: Trap Race ─────────────────────────────────────────────────────

    private void InitTrapRace()
    {
        _trapTimer  = 0f;
        _trapReady  = true;
        _rankUpTimer = 0f;

        if (modeLabel != null) modeLabel.text = "Mode: Trap Race";
        if (trapButton != null) trapButton.gameObject.SetActive(true);

        foreach (var task in _tasks)
            spawner.Spawn(task, 0, RandomSpeed(), _selectedBudget, progressMode: true);

        UpdateTrapUI();
    }

    private void UpdateTrapRace()
    {
        // Trap cooldown
        if (!_trapReady)
        {
            _trapTimer += Time.deltaTime;
            if (_trapTimer >= trapCooldown)
            {
                _trapReady = true;
                _trapTimer = 0f;
            }
        }

        // Rank-up interval: unpunished tasks get stronger
        _rankUpTimer += Time.deltaTime;
        if (_rankUpTimer >= rankUpInterval)
        {
            _rankUpTimer = 0f;
            foreach (var t in new List<TaskItem>(spawner.ActiveTasks))
                t.IncreaseRank();
        }

        UpdateTrapUI();
        UpdateHUD();
    }

    /// <summary>Trap button: freeze + damage a random leading task.</summary>
    private void ActivateTrap()
    {
        if (!_trapReady || spawner.ActiveTasks.Count == 0) return;

        // Pick the task furthest ahead
        TaskItem leader = null;
        float maxProgress = -1f;
        foreach (var t in spawner.ActiveTasks)
        {
            if (t.Progress > maxProgress) { maxProgress = t.Progress; leader = t; }
        }

        if (leader == null) return;

        leader.TakeDamage(trapDamage, false);
        leader.Freeze(trapFreezeSec);

        _trapReady = false;
        _trapTimer = 0f;
        UpdateTrapUI();
    }

    private void UpdateTrapUI()
    {
        if (trapButton        != null) trapButton.interactable = _trapReady;
        if (trapCooldownText  != null)
            trapCooldownText.text = _trapReady
                ? "Trap Ready!"
                : $"Trap: {trapCooldown - _trapTimer:0.0}s";
    }

    // ── Mode 2: Battle ────────────────────────────────────────────────────────

    private void InitBattle()
    {
        _playerHP         = 50f;
        _playerMaxHP      = 50f;
        _playerDamageBoost = 1f;
        _playerBurnDPS    = 0f;
        _playerSlowMult   = 1f;
        _playerBlockChance = 0f;
        _playerHealOnKill = 0f;
        _killCount        = 0;
        _spawnTimer       = 0f;
        _timerRunning     = true;

        _globalTimeLeft = (_selectedBudget == TaskItem.TimeBudget.VeryLong)
            ? float.MaxValue
            : (int)_selectedBudget * 60f;

        if (modeLabel != null) modeLabel.text = "Mode: Battle";
        if (trapButton != null) trapButton.gameObject.SetActive(false);
        if (lootLogText  != null) lootLogText.text = "";

        // Spawn first wave
        SpawnBattleTask();
    }

    private void UpdateBattle()
    {
        // Global countdown
        if (_timerRunning && _selectedBudget != TaskItem.TimeBudget.VeryLong)
        {
            _globalTimeLeft -= Time.deltaTime;
            if (_globalTimeLeft <= 0f)
            {
                _globalTimeLeft = 0f;
                EndGame("Time's up! You ran out of time.");
                return;
            }
        }

        // Auto-spawn
        _spawnTimer += Time.deltaTime;
        if (_spawnTimer >= spawnIntervalBattle)
        {
            _spawnTimer = 0f;
            if (spawner.ActiveTasks.Count < _tasks.Count)
                SpawnBattleTask();
        }

        // Apply player slow aura to all tasks
        foreach (var t in spawner.ActiveTasks)
            t.SlowMultiplier = _playerSlowMult;

        UpdateHUD();
    }

    private void SpawnBattleTask()
    {
        if (_tasks.Count == 0) return;
        string name  = _tasks[Random.Range(0, _tasks.Count)];
        var    task  = spawner.Spawn(name, 0, RandomSpeed(), _selectedBudget, false);
        if (task == null) return;

        // Tasks in battle mode move toward the player (track via overtime)
        StartCoroutine(BattleTaskApproach(task));
    }

    /// <summary>
    /// Simulate the task "approaching": after a delay it reaches the player
    /// and starts overtime drain unless already killed.
    /// </summary>
    private IEnumerator BattleTaskApproach(TaskItem task)
    {
        float approachTime = Mathf.Lerp(8f, 20f, 1f - (task.BaseSpeed - 0.4f) / 0.7f);
        yield return new WaitForSeconds(approachTime);

        if (!task.IsActive) yield break;

        // Task reached the player – start overtime HP drain
        task.StartOvertime(drain => DamagePlayer(drain));
    }

    private void DamagePlayer(float amount)
    {
        _playerHP = Mathf.Max(0f, _playerHP - amount);
        UpdateHUD();
        if (_playerHP <= 0f)
            EndGame("You have been defeated!");
    }

    // ── Mode 3: Cooking ───────────────────────────────────────────────────────

    private void InitCooking()
    {
        if (cookingMinigame == null)
        {
            Debug.LogWarning("[GameManager] CookingTowerMinigame is not assigned.");
            return;
        }

        if (modeLabel != null) modeLabel.text = "Mode: Cooking Tower";
        if (trapButton != null) trapButton.gameObject.SetActive(false);

        cookingMinigame.StartGame(_tasks, OnCookingEnded);
    }

    private void OnCookingEnded(string result)
    {
        EndGame(result);
    }

    // ── Task event callbacks ──────────────────────────────────────────────────

    private void OnTaskFinished(TaskItem t)
    {
        switch (_mode)
        {
            case GameMode.TrapRace:
                // First task to cross the finish line must be done
                EndGame($"Task '{t.TaskName}' won the race! You must complete it.");
                spawner.CleanupAll();
                return;

            case GameMode.Battle:
                _killCount++;
                // Add 5 bonus minutes to global timer
                if (_selectedBudget != TaskItem.TimeBudget.VeryLong)
                    _globalTimeLeft += bonusTimePerKill;
                // Heal on kill
                _playerHP = Mathf.Min(_playerMaxHP, _playerHP + _playerHealOnKill);
                // Apply loot
                GrantBattleLoot(t);
                break;
        }

        spawner.Cleanup(t);
        UpdateHUD();

        if (_mode == GameMode.Battle && spawner.ActiveTasks.Count == 0)
            SpawnBattleTask();
    }

    private void OnTaskClicked(TaskItem t)
    {
        if (!_running) return;

        switch (_mode)
        {
            case GameMode.TrapRace:
                // In trap race, clicking a task applies the trap effect directly
                if (_trapReady)
                {
                    t.TakeDamage(trapDamage, false);
                    t.Freeze(trapFreezeSec);
                    _trapReady = false;
                    _trapTimer = 0f;
                    UpdateTrapUI();
                }
                break;

            case GameMode.Battle:
                PerformAttack(t);
                break;
        }
    }

    // ── Battle combat ─────────────────────────────────────────────────────────

    private void PerformAttack(TaskItem target)
    {
        float dmg = playerBaseDamage * _playerDamageBoost;

        // Critical hit
        bool isCrit = Random.value < critChance;
        if (isCrit) dmg *= critMultiplier;

        target.TakeDamage(dmg);

        // Apply player burn DoT to target
        if (_playerBurnDPS > 0f)
            target.ApplyBurn(_playerBurnDPS);

        string msg = isCrit ? $"CRIT! -{dmg:0} dmg" : $"-{dmg:0} dmg";
        if (statusText != null) statusText.text = msg;
    }

    // ── Roguelike loot ────────────────────────────────────────────────────────

    private readonly string[] LootPool =
    {
        "Damage Boost",
        "Burn DoT",
        "Slow Aura",
        "Block Chance",
        "Heal on Kill",
        "Trap Upgrade",
        "Spawn Slowdown",
    };

    private void GrantBattleLoot(TaskItem defeatedTask)
    {
        // Higher rank = higher loot chance
        float lootChance = 0.40f + defeatedTask.Rank * 0.08f;
        if (Random.value > lootChance) return;

        string loot = LootPool[Random.Range(0, LootPool.Length)];
        ApplyLoot(loot);

        if (lootLogText != null)
            lootLogText.text = $"Loot: {loot}!";
    }

    private void ApplyLoot(string loot)
    {
        switch (loot)
        {
            case "Damage Boost":
                _playerDamageBoost += 0.20f;
                if (damageBoostText != null)
                    damageBoostText.text = $"DMG: x{_playerDamageBoost:0.0}";
                break;
            case "Burn DoT":
                _playerBurnDPS += 3f;
                break;
            case "Slow Aura":
                _playerSlowMult = Mathf.Max(0.30f, _playerSlowMult - 0.10f);
                break;
            case "Block Chance":
                _playerBlockChance = Mathf.Min(0.60f, _playerBlockChance + 0.10f);
                // Give player's block to tasks (they block player attacks less)
                // In this model player block doesn't apply, but tracked for display
                break;
            case "Heal on Kill":
                _playerHealOnKill += 5f;
                break;
            case "Trap Upgrade":
                trapDamage  += 10f;
                trapCooldown = Mathf.Max(2f, trapCooldown - 1f);
                break;
            case "Spawn Slowdown":
                spawnIntervalBattle = Mathf.Min(12f, spawnIntervalBattle + 1.5f);
                break;
        }
    }

    // ── Game end ──────────────────────────────────────────────────────────────

    private void EndGame(string message)
    {
        _running      = false;
        _timerRunning = false;
        if (statusText != null)
            statusText.text = message;
        StartCoroutine(ReturnToMenuAfterDelay(4f));
    }

    private IEnumerator ReturnToMenuAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        ShowMenu();
    }

    // ── HUD update ────────────────────────────────────────────────────────────

    private void UpdateHUD()
    {
        if (healthText != null)
            healthText.text = $"HP: {Mathf.CeilToInt(_playerHP)}/{Mathf.CeilToInt(_playerMaxHP)}";

        if (timerText != null)
        {
            if (_selectedBudget == TaskItem.TimeBudget.VeryLong || _globalTimeLeft >= float.MaxValue * 0.5f)
            {
                timerText.text = "Time: ∞";
            }
            else
            {
                int mins = Mathf.FloorToInt(_globalTimeLeft / 60f);
                int secs = Mathf.FloorToInt(_globalTimeLeft % 60f);
                timerText.text = $"Time: {mins}:{secs:00}";
                if (timerText != null)
                    timerText.color = _globalTimeLeft < 60f ? Color.red : Color.white;
            }
        }

        if (_mode == GameMode.Battle && statusText != null && _running)
        {
            // Don't overwrite attack feedback every frame – only show kills
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void ParseTasks()
    {
        _tasks.Clear();
        if (todoInput == null || string.IsNullOrEmpty(todoInput.text)) return;

        var raw = todoInput.text.Split(new[] { '\n', ',', ';' },
                                       System.StringSplitOptions.RemoveEmptyEntries);
        foreach (var t in raw)
        {
            var trimmed = t.Trim();
            if (trimmed.Length > 0) _tasks.Add(trimmed);
        }
    }

    private float RandomSpeed() => Random.Range(0.4f, 1.0f);
}
