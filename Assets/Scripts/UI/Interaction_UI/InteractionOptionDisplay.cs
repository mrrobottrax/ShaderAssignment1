using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InteractionOptionDisplay : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] Image _promptImage;
    [SerializeField] Image _inputImage;
    [SerializeField] TextMeshProUGUI _promptText;

    [Header("System")]
    private PlayerInteraction playerInteraction;
    private RectTransform optionRectTransform;

    private IInteraction interactionData;

    private bool isHighlighted;

    private void Awake()
    {
        optionRectTransform = GetComponent<RectTransform>();
    }

    private void Update()
    {
        // Store the highlight state when the frame starts and the updated highlight state
        bool prevHighlighted = isHighlighted;
        isHighlighted = ValidateInteraction();

        // Set the interaction that the player will perform when selected
        if (isHighlighted)
            playerInteraction.SetInteractionOptions(this);
        else if (prevHighlighted && !isHighlighted)// Compare highlight states. If this display was highlighted but is no longer highlighted, then clear.
            playerInteraction.ClearInteractionOptions();

        // Display input prompt
        _inputImage.gameObject.SetActive(isHighlighted);
    }

    #region Init and Clear methods
    public void SetOption(PlayerInteraction playerRef, Sprite interactSprite, Sprite inputSprite, string prompt, IInteraction interaction)
    {
        // Store the player
        if (playerInteraction == null)
            playerInteraction = playerRef;

        // Set image sprites
        _promptImage.sprite = interactSprite;
        _inputImage.sprite = inputSprite;

        // Set text
        _promptText.text = prompt;

        // Store interaction
        interactionData = interaction;

        // Enable and refresh option
        gameObject.SetActive(true);
        LayoutRebuilder.ForceRebuildLayoutImmediate(optionRectTransform);
    }

    public void ClearOption()
    {
        gameObject.SetActive(false);

        // Clear stored values
        _promptImage.sprite = null;
        _inputImage.sprite = null;

        _promptText.text = null;
        interactionData = null;
    }
    #endregion

    #region Interaction Methods
    public IInteraction GetInteractionData()
    {
        return interactionData;
    }

    /// <summary>
    /// This method determines if the center of the screen is within the options rect. If true, the player can interact.
    /// </summary>
    public bool ValidateInteraction()
    {
        Vector2 screenCenter = new Vector2(Screen.width / 2, Screen.height / 2);
        return RectTransformUtility.RectangleContainsScreenPoint(optionRectTransform, screenCenter);
    }
    #endregion
}
