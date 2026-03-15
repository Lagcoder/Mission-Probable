using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Represents a single task entity in the game.
/// Supports health/rank tiers, time budgets, damage, freezing, burn DoT,
/// slow aura, and block chance for Modes 1 and 2.
/// </summary>
public class TaskItem : MonoBehaviour
{
    // ── Rank / colour tiers ──────────────────────────────────────────────────
    public static readonly Color[] RankColors = new Color[]
    {
        new Color(0.53f, 0.81f, 0.98f), // 0 – Light Blue
        new Color(0.20f, 0.80f, 0.20f), // 1 – Green
        new Color(1.00f, 0.94f, 0.20f), // 2 – Yellow
        new Color(1.00f, 0.55f, 0.10f), // 3 – Orange
        new Color(0.90f, 0.15f, 0.15f), // 4 – Red
        new Color(0.65f, 0.10f, 0.85f), // 5 – Purple
        new Color(0.10f, 0.10f, 0.10f), // 6 – Black
    };

    // ── Time-budget definitions (minutes) ───────────────────────────────────
    public enum TimeBudget { Short = 10, Medium = 30, Long = 60, VeryLong = -1 }

    // ── Serialised UI references ─────────────────────────────────────────────
    [Header("UI References")]
    [SerializeField] private Text      labelText;
    [SerializeField] private Text      healthText;
    [SerializeField] private Text      timerText;
    [SerializeField] private Image     backgroundImage;
    [SerializeField] private Image     healthBarFill;
    [SerializeField] private Image     progressBarFill;
    [SerializeField] private Button    clickButton;

    // ── Public read-only state ───────────────────────────────────────────────
    public string   TaskName   { get; private set; }
    public int      Rank       { get; private set; }          // 0-6
    public float    MaxHealth  { get; private set; }
    public float    Health     { get; private set; }
    public float    BaseSpeed  { get; private set; }
    public float    CurrentSpeed { get; private set; }
    public bool     IsFrozen   { get; private set; }
    public bool     IsBlocked  { get; private set; }
    public bool     IsActive   { get; private set; }

    // ── Battle / loot stats ──────────────────────────────────────────────────
    public float BlockChance      { get; set; } = 0f;    // 0-1
    public float BurnDPS          { get; set; } = 0f;    // damage per second via burn
    public float SlowMultiplier   { get; set; } = 1f;    // <1 = slowed
    public float DamageMultiplier { get; set; } = 1f;    // player damage multiplier
    public bool  HasSlowAura      { get; private set; }

    // ── Time-budget tracking (Mode 2) ────────────────────────────────────────
    public TimeBudget Budget      { get; private set; }
    public float      TimeLeft    { get; private set; }   // seconds
    public bool       OverTime    { get; private set; }
    public float      OvertimeDrained { get; private set; }

    // ── Progress (Mode 1) ────────────────────────────────────────────────────
    public float Progress => _progress;

    // ── Callbacks ────────────────────────────────────────────────────────────
    public System.Action<TaskItem> OnFinished;   // reached finish / died
    public System.Action<TaskItem> OnClicked;

    // ── Private state ────────────────────────────────────────────────────────
    private float _progress       = 0f;
    private bool  _progressMode   = false; // true while used in Mode 1 (race)
    private bool  _burnActive     = false;
    private float _freezeTimer    = 0f;
    private const float FreezeSeconds = 1.5f;

    // ── Initialise ───────────────────────────────────────────────────────────
    /// <summary>Call once after Instantiate to set up task properties.</summary>
    public void Init(string taskName, int rank, float baseSpeed,
                     TimeBudget budget = TimeBudget.Medium, bool progressMode = false)
    {
        TaskName      = taskName;
        Rank          = Mathf.Clamp(rank, 0, RankColors.Length - 1);
        BaseSpeed     = baseSpeed;
        CurrentSpeed  = baseSpeed;
        Budget        = budget;
        _progressMode = progressMode;
        _progress     = 0f;
        IsFrozen      = false;
        OverTime      = false;
        OvertimeDrained = 0f;
        IsActive      = true;

        // Health scales with rank
        MaxHealth = 30f + Rank * 20f;
        Health    = MaxHealth;

        // Timer (negative for VeryLong = unlimited)
        TimeLeft = (budget == TimeBudget.VeryLong) ? float.MaxValue : (int)budget * 60f;

        ApplyRankVisuals();

        if (labelText  != null) labelText.text  = taskName;
        if (healthText != null) healthText.text = FormatHealth();

        if (clickButton != null)
            clickButton.onClick.AddListener(() => OnClicked?.Invoke(this));
    }

    // ── Unity Update ─────────────────────────────────────────────────────────
    private void Update()
    {
        if (!IsActive) return;

        // Freeze countdown
        if (IsFrozen)
        {
            _freezeTimer -= Time.deltaTime;
            if (_freezeTimer <= 0f) IsFrozen = false;
        }

        // Burn DoT
        if (_burnActive && BurnDPS > 0f)
            TakeDamage(BurnDPS * Time.deltaTime, false);

        // Time budget countdown (Mode 2 / any mode with time tracking)
        if (Budget != TimeBudget.VeryLong && TimeLeft > 0f)
        {
            TimeLeft -= Time.deltaTime;
            if (TimeLeft < 0f) TimeLeft = 0f;
        }

        // Progress bar (Mode 1 race)
        if (_progressMode && !IsFrozen)
        {
            _progress += CurrentSpeed * SlowMultiplier * Time.deltaTime * 0.05f;
            _progress  = Mathf.Clamp01(_progress);
            if (progressBarFill != null) progressBarFill.fillAmount = _progress;
            if (_progress >= 1f) FinishTask();
        }

        UpdateTimerUI();
    }

    // ── Public API ───────────────────────────────────────────────────────────

    /// <summary>Apply damage with optional crit calculation (external).</summary>
    public void TakeDamage(float amount, bool canBeBlocked = true)
    {
        if (!IsActive) return;

        if (canBeBlocked && Random.value < BlockChance)
        {
            ShowBlockFeedback();
            return;
        }

        Health -= amount;
        Health  = Mathf.Max(0f, Health);

        if (healthBarFill != null)
            healthBarFill.fillAmount = Health / MaxHealth;
        if (healthText != null)
            healthText.text = FormatHealth();

        if (Health <= 0f)
            Die();
    }

    /// <summary>Apply a timed freeze (Mode 1 trap / Mode 2 slow).</summary>
    public void Freeze(float duration = FreezeSeconds)
    {
        IsFrozen      = true;
        _freezeTimer  = duration;
    }

    /// <summary>Permanently increase rank (called when task respawns or goes unpunished).</summary>
    public void IncreaseRank()
    {
        Rank      = Mathf.Min(Rank + 1, RankColors.Length - 1);
        MaxHealth = 30f + Rank * 20f;
        Health    = MaxHealth;
        BaseSpeed *= 1.25f;
        CurrentSpeed = BaseSpeed;
        _progress = 0f;
        ApplyRankVisuals();
    }

    /// <summary>Start burn damage-over-time.</summary>
    public void ApplyBurn(float dps)
    {
        BurnDPS     = dps;
        _burnActive = true;
    }

    /// <summary>Apply a slow aura to a task (for nearby slow loot).</summary>
    public void EnableSlowAura(float multiplier)
    {
        HasSlowAura    = true;
        SlowMultiplier = Mathf.Min(SlowMultiplier, multiplier);
    }

    /// <summary>Add bonus time (Mode 2 reward for killing a task).</summary>
    public void AddBonusTime(float seconds)
    {
        if (Budget != TimeBudget.VeryLong)
            TimeLeft = Mathf.Min(TimeLeft + seconds, (int)Budget * 60f + seconds);
    }

    /// <summary>Mark task as reaching the player (Mode 2 overtime tracking).</summary>
    public void StartOvertime(System.Action<float> onDrainPerSecond)
    {
        OverTime = true;
        StartCoroutine(OvertimeRoutine(onDrainPerSecond));
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private void Die()
    {
        IsActive = false;
        OnFinished?.Invoke(this);
    }

    private void FinishTask()
    {
        IsActive = false;
        OnFinished?.Invoke(this);
    }

    private void ApplyRankVisuals()
    {
        if (backgroundImage != null)
            backgroundImage.color = RankColors[Rank];
        if (progressBarFill != null)
            progressBarFill.color = RankColors[Rank];
    }

    private string FormatHealth() => $"{Mathf.CeilToInt(Health)}/{Mathf.CeilToInt(MaxHealth)}";

    private void UpdateTimerUI()
    {
        if (timerText == null) return;
        if (Budget == TimeBudget.VeryLong)
        {
            timerText.text = "∞";
            return;
        }
        int mins = Mathf.FloorToInt(TimeLeft / 60f);
        int secs = Mathf.FloorToInt(TimeLeft % 60f);
        timerText.text = $"{mins}:{secs:00}";
        timerText.color = TimeLeft < 60f ? Color.red : Color.white;
    }

    private void ShowBlockFeedback()
    {
        // Brief flash; expand with animation if desired
        StartCoroutine(BlockFlash());
    }

    private IEnumerator BlockFlash()
    {
        if (backgroundImage != null)
        {
            Color orig = backgroundImage.color;
            backgroundImage.color = Color.white;
            yield return new WaitForSeconds(0.12f);
            backgroundImage.color = orig;
        }
    }

    private IEnumerator OvertimeRoutine(System.Action<float> onDrainPerSecond)
    {
        const float maxDrain     = 20f;
        const float drainPerSec  = maxDrain / 60f; // drains over 60 s to hit cap

        while (OverTime && OvertimeDrained < maxDrain && IsActive)
        {
            float drain = drainPerSec * Time.deltaTime;
            OvertimeDrained = Mathf.Min(OvertimeDrained + drain, maxDrain);
            onDrainPerSecond?.Invoke(drain);
            yield return null;
        }
    }
}
