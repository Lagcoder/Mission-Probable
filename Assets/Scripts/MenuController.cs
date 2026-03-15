using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Handles the main menu UI: showing/hiding panels, credits, and any
/// pre-game navigation that lives outside GameManager's control.
/// </summary>
public class MenuController : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject creditsPanel;
    [SerializeField] private GameObject howToPlayPanel;

    [Header("Main Menu Buttons")]
    [SerializeField] private Button creditsButton;
    [SerializeField] private Button howToPlayButton;

    [Header("Credits Panel")]
    [SerializeField] private Button closeCreditsButton;

    [Header("How-to-Play Panel")]
    [SerializeField] private Button closeHowToPlayButton;

    // ── Unity lifecycle ───────────────────────────────────────────────────────

    private void Start()
    {
        // Credits
        if (creditsButton != null)
            creditsButton.onClick.AddListener(OpenCredits);
        if (closeCreditsButton != null)
            closeCreditsButton.onClick.AddListener(CloseCredits);

        // How to Play
        if (howToPlayButton != null)
            howToPlayButton.onClick.AddListener(OpenHowToPlay);
        if (closeHowToPlayButton != null)
            closeHowToPlayButton.onClick.AddListener(CloseHowToPlay);

        // Start with secondary panels hidden
        if (creditsPanel   != null) creditsPanel.SetActive(false);
        if (howToPlayPanel != null) howToPlayPanel.SetActive(false);
    }

    // ── Panel helpers ─────────────────────────────────────────────────────────

    private void OpenCredits()
    {
        if (creditsPanel != null) creditsPanel.SetActive(true);
    }

    private void CloseCredits()
    {
        if (creditsPanel != null) creditsPanel.SetActive(false);
    }

    private void OpenHowToPlay()
    {
        if (howToPlayPanel != null) howToPlayPanel.SetActive(true);
    }

    private void CloseHowToPlay()
    {
        if (howToPlayPanel != null) howToPlayPanel.SetActive(false);
    }

    // ── Public toggle (can be called from other scripts) ──────────────────────

    public void ShowMainMenu()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        if (creditsPanel  != null) creditsPanel.SetActive(false);
        if (howToPlayPanel != null) howToPlayPanel.SetActive(false);
    }
}
