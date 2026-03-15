using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Mode 3 – Cooking / Stacking minigame.
///
/// Each task is an "ingredient block" that falls from the top of the screen.
/// The player taps/clicks to drop it onto the tower.
///
/// Rules:
///   • A clean drop (lands within the current platform width) adds a layer
///     and grants 2 bonus minutes to the game timer.
///   • A bad drop (misses the platform) means the player must complete that
///     task immediately – the block is removed with a penalty message.
///   • The taller the tower, the more bonus time is banked.
///   • The game ends when all tasks have been processed.
/// </summary>
public class CookingTowerMinigame : MonoBehaviour
{
    // ── Inspector references ──────────────────────────────────────────────────
    [Header("UI References")]
    [SerializeField] private RectTransform towerParent;    // parent for stacked blocks
    [SerializeField] private RectTransform fallingBlock;   // the currently dropping block
    [SerializeField] private Text          fallingLabel;   // label on the falling block
    [SerializeField] private Text          resultText;     // feedback ("Clean!", "Miss!")
    [SerializeField] private Text          bonusTimeText;  // accumulated bonus time
    [SerializeField] private Text          towerHeightText;
    [SerializeField] private Button        dropButton;     // tap/click to drop
    [SerializeField] private GameObject    cookingPanel;

    [Header("Layout")]
    [SerializeField] private float blockHeight   = 60f;
    [SerializeField] private float startWidth    = 300f;
    [SerializeField] private float minWidth      = 60f;
    [SerializeField] private float fallStartY    = 400f;   // top of screen in local space
    [SerializeField] private float fallSpeed     = 180f;   // pixels per second
    [SerializeField] private float swingSpeed    = 90f;    // side-to-side swing speed
    [SerializeField] private float swingAmplitude= 160f;   // max horizontal offset
    [SerializeField] private float bonusPerLayer = 120f;   // seconds per clean layer

    // ── Private state ─────────────────────────────────────────────────────────
    private List<string>          _tasks;
    private int                   _taskIndex;
    private float                 _currentWidth;
    private float                 _towerTopY;        // Y-position of top of stack
    private float                 _totalBonusTime;
    private int                   _cleanLayers;
    private bool                  _blockFalling;
    private bool                  _gameActive;
    private System.Action<string> _onGameEnded;

    // Swing/pendulum state
    private float _swingAngle;
    private bool  _swingRight = true;

    // ── Public entry / exit ───────────────────────────────────────────────────

    public void StartGame(List<string> tasks, System.Action<string> onEnded)
    {
        _tasks          = new List<string>(tasks);
        _taskIndex      = 0;
        _currentWidth   = startWidth;
        _towerTopY      = 0f;
        _totalBonusTime = 0f;
        _cleanLayers    = 0;
        _onGameEnded    = onEnded;
        _gameActive     = true;

        if (cookingPanel != null) cookingPanel.SetActive(true);
        if (resultText   != null) resultText.text = "";
        if (towerParent  != null)
        {
            foreach (Transform child in towerParent)
                Destroy(child.gameObject);
        }

        if (dropButton != null)
        {
            dropButton.onClick.RemoveAllListeners();
            dropButton.onClick.AddListener(OnDropPressed);
        }

        UpdateUI();
        SpawnNextBlock();
    }

    public void EndGame()
    {
        _gameActive   = false;
        _blockFalling = false;
        if (cookingPanel != null) cookingPanel.SetActive(false);
    }

    // ── Unity loop ────────────────────────────────────────────────────────────

    private void Update()
    {
        if (!_gameActive || !_blockFalling) return;

        AnimateFallingBlock();
    }

    // ── Block animation ───────────────────────────────────────────────────────

    private void AnimateFallingBlock()
    {
        if (fallingBlock == null) return;

        Vector2 pos = fallingBlock.anchoredPosition;

        // Pendulum swing
        if (_swingRight)
        {
            _swingAngle += swingSpeed * Time.deltaTime;
            if (_swingAngle > swingAmplitude) { _swingAngle = swingAmplitude; _swingRight = false; }
        }
        else
        {
            _swingAngle -= swingSpeed * Time.deltaTime;
            if (_swingAngle < -swingAmplitude) { _swingAngle = -swingAmplitude; _swingRight = true; }
        }

        // Move downward
        pos.y -= fallSpeed * Time.deltaTime;
        pos.x  = _swingAngle;
        fallingBlock.anchoredPosition = pos;

        // Auto-drop if block reaches tower top
        if (pos.y <= _towerTopY + blockHeight)
        {
            ResolveDrop();
        }
    }

    // ── Input ─────────────────────────────────────────────────────────────────

    private void OnDropPressed()
    {
        if (!_gameActive || !_blockFalling) return;
        ResolveDrop();
    }

    // ── Drop resolution ───────────────────────────────────────────────────────

    private void ResolveDrop()
    {
        if (!_blockFalling) return;
        _blockFalling = false;

        float blockCenterX = fallingBlock != null ? fallingBlock.anchoredPosition.x : 0f;
        bool  clean        = Mathf.Abs(blockCenterX) <= (_currentWidth / 2f);

        string taskName = _taskIndex > 0 && _taskIndex <= _tasks.Count
            ? _tasks[_taskIndex - 1]
            : "Unknown Task";

        if (clean)
        {
            PlaceClean(taskName, blockCenterX);
        }
        else
        {
            PlaceMiss(taskName);
        }
    }

    private void PlaceClean(string taskName, float centerX)
    {
        _cleanLayers++;
        _totalBonusTime += bonusPerLayer;

        // Narrow the platform slightly (gets harder)
        _currentWidth = Mathf.Max(minWidth, _currentWidth - 12f);

        // Add a visual block to the tower
        AddTowerBlock(taskName, RankColorForLayer(_cleanLayers - 1));

        _towerTopY += blockHeight;

        ShowResult("Clean! +" + Mathf.RoundToInt(bonusPerLayer / 60f) + " min bonus", Color.green);
        UpdateUI();
        StartCoroutine(NextBlockDelay(0.8f));
    }

    private void PlaceMiss(string taskName)
    {
        ShowResult($"Miss! Do '{taskName}' now!", Color.red);
        UpdateUI();
        StartCoroutine(NextBlockDelay(1.2f));
    }

    private IEnumerator NextBlockDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (!_gameActive) yield break;

        if (_taskIndex >= _tasks.Count)
        {
            // All tasks processed
            string summary =
                $"Tower done! {_cleanLayers} clean layers, " +
                $"+{Mathf.RoundToInt(_totalBonusTime / 60f)} bonus minutes.";
            EndGame();
            _onGameEnded?.Invoke(summary);
        }
        else
        {
            SpawnNextBlock();
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void SpawnNextBlock()
    {
        if (_taskIndex >= _tasks.Count) return;

        string task = _tasks[_taskIndex];
        _taskIndex++;

        if (fallingBlock != null)
        {
            fallingBlock.anchoredPosition = new Vector2(0f, fallStartY);
            fallingBlock.sizeDelta        = new Vector2(_currentWidth, blockHeight);

            var img = fallingBlock.GetComponent<Image>();
            if (img != null) img.color = RankColorForLayer(_cleanLayers);
        }

        if (fallingLabel != null) fallingLabel.text = task;

        _swingAngle   = 0f;
        _swingRight   = (Random.value > 0.5f);
        _blockFalling = true;
    }

    private void AddTowerBlock(string taskName, Color color)
    {
        if (towerParent == null || fallingBlock == null) return;

        // Clone the falling block as a static tower layer
        var block = Instantiate(fallingBlock, towerParent);
        block.anchoredPosition = new Vector2(0f, _towerTopY);
        block.sizeDelta        = new Vector2(_currentWidth, blockHeight);

        var img = block.GetComponent<Image>();
        if (img != null) img.color = color;

        var lbl = block.GetComponentInChildren<Text>();
        if (lbl != null) lbl.text = taskName;

        block.gameObject.SetActive(true);
    }

    private void ShowResult(string message, Color color)
    {
        if (resultText == null) return;
        resultText.text  = message;
        resultText.color = color;
    }

    private void UpdateUI()
    {
        if (bonusTimeText   != null)
            bonusTimeText.text = $"+{Mathf.RoundToInt(_totalBonusTime / 60f)} min banked";
        if (towerHeightText != null)
            towerHeightText.text = $"Tower: {_cleanLayers} layers";
    }

    private Color RankColorForLayer(int layer)
    {
        int idx = Mathf.Clamp(layer, 0, TaskItem.RankColors.Length - 1);
        return TaskItem.RankColors[idx];
    }

    // ── Public accessors ──────────────────────────────────────────────────────

    public float TotalBonusTimeSeconds => _totalBonusTime;
    public int   CleanLayers           => _cleanLayers;
}
