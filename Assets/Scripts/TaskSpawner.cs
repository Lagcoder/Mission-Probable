using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages spawning, tracking, and cleanup of TaskItem instances.
/// </summary>
public class TaskSpawner : MonoBehaviour
{
    [Header("Prefab & Parent")]
    [SerializeField] private TaskItem    taskPrefab;
    [SerializeField] private RectTransform taskParent;

    [Header("Spawn Layout (optional offset for race-mode lanes)")]
    [SerializeField] private float laneHeight = 80f;

    public List<TaskItem> ActiveTasks { get; } = new List<TaskItem>();

    public System.Action<TaskItem> OnTaskFinished;
    public System.Action<TaskItem> OnTaskClicked;

    // ── Spawn ────────────────────────────────────────────────────────────────

    /// <summary>Spawn a new task with the given parameters.</summary>
    public TaskItem Spawn(string taskName, int rank, float baseSpeed,
                          TaskItem.TimeBudget budget = TaskItem.TimeBudget.Medium,
                          bool progressMode = false)
    {
        if (taskPrefab == null)
        {
            Debug.LogError("[TaskSpawner] taskPrefab is not assigned.");
            return null;
        }

        var item = Instantiate(taskPrefab, taskParent);
        item.Init(taskName, rank, baseSpeed, budget, progressMode);

        // Stagger vertically so tasks don't overlap
        PositionTask(item, ActiveTasks.Count);

        item.OnFinished += HandleTaskFinished;
        item.OnClicked  += HandleTaskClicked;
        ActiveTasks.Add(item);

        return item;
    }

    /// <summary>Re-spawn an existing task (for Mode 1 respawn with higher rank).</summary>
    public TaskItem Respawn(string taskName, int newRank, float baseSpeed,
                            TaskItem.TimeBudget budget = TaskItem.TimeBudget.Medium,
                            bool progressMode = false)
    {
        return Spawn(taskName, newRank, baseSpeed, budget, progressMode);
    }

    // ── Cleanup ───────────────────────────────────────────────────────────────

    public void Cleanup(TaskItem t)
    {
        if (t == null) return;
        ActiveTasks.Remove(t);
        Destroy(t.gameObject);
        RefreshPositions();
    }

    public void CleanupAll()
    {
        foreach (var t in new List<TaskItem>(ActiveTasks))
        {
            if (t != null) Destroy(t.gameObject);
        }
        ActiveTasks.Clear();
    }

    // ── Private ───────────────────────────────────────────────────────────────

    private void HandleTaskFinished(TaskItem t)
    {
        OnTaskFinished?.Invoke(t);
        // Note: GameManager calls Cleanup – don't auto-destroy here so GM can read state first.
    }

    private void HandleTaskClicked(TaskItem t)
    {
        OnTaskClicked?.Invoke(t);
    }

    private void PositionTask(TaskItem item, int index)
    {
        var rt = item.GetComponent<RectTransform>();
        if (rt == null) return;
        rt.anchoredPosition = new Vector2(0f, -index * laneHeight);
    }

    private void RefreshPositions()
    {
        for (int i = 0; i < ActiveTasks.Count; i++)
        {
            if (ActiveTasks[i] == null) continue;
            PositionTask(ActiveTasks[i], i);
        }
    }
}
