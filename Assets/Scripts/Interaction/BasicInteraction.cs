using UnityEngine;
using UnityEngine.Events;

public class BasicInteraction : MonoBehaviour, IInteraction
{
    [Header("Interaction Parameters")]
    [SerializeField] private string interactionPrompt;
    public string InteractionPrompt => interactionPrompt;

    [SerializeField] private Sprite interactSprite;
    public Sprite InteractSprite => interactSprite;

    [SerializeField] private bool interactionEnabled = true;
    public bool IsInteractionEnabled => interactionEnabled;

    [Header("Interaction Event")]
    [SerializeField] private UnityEvent interactionEvent;

    public void Interact(Transform interactor)
    {
        interactor.TryGetComponent(out PlayerInteraction interaction);

        interaction?.SetUsingInteractable(true);

        interactionEvent.Invoke();

        interaction?.SetUsingInteractable(false);
    }

    public void SetInteractionEnabled(bool enabled)
    {
        interactionEnabled = enabled;
    }
}
