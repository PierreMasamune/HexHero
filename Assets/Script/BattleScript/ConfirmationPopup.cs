using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ConfirmationPopup : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject rootPanel;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;

    [Tooltip("Hide the Cancel button when showing a message-only popup.")]
    [SerializeField] private bool hideCancelOnMessage = true;

    // ===== Singleton — lets any script call ConfirmationPopup.Instance.Show(...) =====
    public static ConfirmationPopup Instance { get; private set; }

    private Action onConfirm;
    private Action onCancel;

    // ==================================================================
    //  LIFECYCLE
    // ==================================================================

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"[ConfirmationPopup] Duplicate on '{name}' destroyed.");
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (rootPanel == null) rootPanel = gameObject;

        if (confirmButton != null && confirmButton.onClick.GetPersistentEventCount() == 0)
            confirmButton.onClick.AddListener(Confirm);

        if (cancelButton != null && cancelButton.onClick.GetPersistentEventCount() == 0)
            cancelButton.onClick.AddListener(Cancel);

        rootPanel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ==================================================================
    //  PUBLIC API — callable from any script
    // ==================================================================

    /// <summary>
    /// Full control: show a message with Yes/No callbacks.
    /// Either callback may be null.
    /// </summary>
    public void Show(string message, Action onConfirm, Action onCancel = null)
    {
        this.onConfirm = onConfirm;
        this.onCancel = onCancel;

        if (messageText != null) messageText.text = message;

        if (cancelButton != null && hideCancelOnMessage)
            cancelButton.gameObject.SetActive(onCancel != null);

        rootPanel.SetActive(true);
    }

    /// <summary>
    /// Message-only popup (no callbacks, Cancel hidden).
    /// Great for a button in the Inspector: wire OnClick → ShowMessage(string).
    /// </summary>
    public void ShowMessage(string message)
    {
        Show(message, null, null);
    }

    /// <summary>
    /// Yes/No popup with a single confirm callback.
    /// Cancel simply closes the popup.
    /// </summary>
    public void ShowConfirm(string message, Action onConfirm)
    {
        Show(message, onConfirm, null);
    }

    /// <summary>Hide the popup without firing any callback.</summary>
    public void Hide()
    {
        rootPanel.SetActive(false);
        onConfirm = null;
        onCancel = null;

        if (cancelButton != null && hideCancelOnMessage)
            cancelButton.gameObject.SetActive(true);   // restore for next time
    }

    // ==================================================================
    //  PUBLIC BUTTON HANDLERS — wire Confirm / Cancel in the Inspector
    // ==================================================================

    public void Confirm()
    {
        var cb = onConfirm;
        Hide();
        cb?.Invoke();
    }

    public void Cancel()
    {
        var cb = onCancel;
        Hide();
        cb?.Invoke();
    }

    // ==================================================================
    //  CONVENIENCE — common popups
    // ==================================================================

    /// <summary>Wire to any "Quit" button: shows "Are you sure?" then quits on Yes.</summary>
    public void ShowQuitConfirmation()
    {
        Show("Are you sure you want to quit?", () =>
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }, null);
    }

    /// <summary>Wire to any "Restart" button: shows "Are you sure?" then restarts on Yes.</summary>
    public void ShowRestartConfirmation()
    {
        Show("Restart the battle?", () =>
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        }, null);
    }
}