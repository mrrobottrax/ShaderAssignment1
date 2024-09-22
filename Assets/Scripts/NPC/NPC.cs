using UnityEngine;

public class NPC : MonoBehaviour, IInteraction
{
    [Header("Interaction Parameters")]
    private const string interactionPrompt = "Talk";
    public string InteractionPrompt => interactionPrompt;

    [SerializeField] private Sprite interactSprite;
    public Sprite InteractSprite => interactSprite;

    [SerializeField] private bool interactionEnabled = true;
    public bool IsInteractionEnabled => interactionEnabled;

    [Header("Dialogue Node")]
    [SerializeField] private DialogueNode interactionDialogue;

    public void Interact(Transform interactor)
    {
        interactor.TryGetComponent(out PlayerHealth player);
        interactor.TryGetComponent(out PlayerInteraction interaction);

        interaction?.SetUsingInteractable(true);

        // Display dialogue
        DialogueMenuDisplay dialogueMenuDisplay = player.PlayerUIManager.DialogueMenuDisplay;
        dialogueMenuDisplay.DisplayDialogueNode(interactionDialogue);

        interaction?.SetUsingInteractable(false);
    }

    public void SetInteractionEnabled(bool enabled)
    {
        interactionEnabled = enabled;
    }

    public void SetInteractionDialogueNode(DialogueNode dialogueNode)
    {
        interactionDialogue = dialogueNode;
    }
}