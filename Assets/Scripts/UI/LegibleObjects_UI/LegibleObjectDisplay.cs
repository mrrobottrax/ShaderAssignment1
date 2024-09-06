using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class LegibleObjectDisplay : MenuDisplayBase
{
    [Header("Components")]

    [Header("UI Elements")]
    [SerializeField] private Image _background;

    [Space(10)]

    [SerializeField] private TextMeshProUGUI _text;
    [SerializeField] private RectTransform _textRect;

    [Space(10)]

    [SerializeField] private TextMeshProUGUI _pageNumberText;

    [Space(10)]

    [SerializeField] private GameObject _backButton;
    [SerializeField] private GameObject _advanceButton;

    [Header("System")]
    private ReadableData currentData;
    private int currentPage;

    public override void SetControlsSubscription(bool isInputEnabled)
    {
        if (isInputEnabled)
            Subscribe();
        else if (InputManager.Instance != null)
            Unsubscribe();
    }

    public void CloseDisplay(InputAction.CallbackContext context)
    {
        // Disable this display if it is the active one
        if (UIManager.Instance.GetActiveDisplay() == this)
            UIManager.Instance.DisableActiveDisplay();
    }

    public override void Subscribe()
    {
        InputManager.Instance.controls.Permanents.Pause.performed += CloseDisplay;
    }

    public override void Unsubscribe()
    {
        InputManager.Instance.controls.Permanents.Pause.performed -= CloseDisplay;
    }

    public void SetReadableData(ReadableData data)
    {
        // Set this as the active UI display
        UIManager.Instance.SetActiveDisplay(this);

        currentPage = 0;

        currentData = data;

        DisplayPage(currentPage);
    }

    #region Button Methods

    public void TryIncrementPage(int amount)
    {
        int result = currentPage + amount;

        if (result >= 0 && result < currentData.PageText.Length)
        {
            currentPage += amount;
            DisplayPage(currentPage);
        }
    }
    #endregion

    private void DisplayPage(int pageNum)
    {
        _background.sprite = currentData.TextBackground;

        _textRect.sizeDelta = new Vector2(currentData.TextAreaWidth, currentData.TextAreaHeight);
        _text.text = currentData.PageText[pageNum];
        _pageNumberText.text = $"{pageNum + 1}";

        // Display page arrows
        _backButton.SetActive(pageNum > 0);
        _advanceButton.SetActive(pageNum < currentData.PageText.Length - 1);
    }
}
