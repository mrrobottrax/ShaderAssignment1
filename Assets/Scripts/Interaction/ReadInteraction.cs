using UnityEngine;

public class ReadInteraction : MonoBehaviour, IInteraction
{
    [Header("Interaction Parameters")]
    private const string interactionPrompt = "Read";
    public string InteractionPrompt => interactionPrompt;

    [SerializeField] private Sprite interactSprite;
    public Sprite InteractSprite => interactSprite;

    [SerializeField] private bool interactionEnabled = true;
    public bool IsInteractionEnabled => interactionEnabled;

    [Header("Readable Data")]
    [SerializeField] private ReadableData readableData;

    public void Interact(Transform interactor)
    {
        interactor.TryGetComponent(out PlayerHealth player);
        interactor.TryGetComponent(out PlayerInteraction interaction);

        interaction?.SetUsingInteractable(true);

        // Display readable data
        LegibleObjectDisplay display = player.PlayerUIManager.LegibleObjectDisplay;
        display.SetReadableData(readableData);

        interaction?.SetUsingInteractable(false);
    }

    public void SetInteractionEnabled(bool enabled)
    {
        interactionEnabled = enabled;
    }
}
