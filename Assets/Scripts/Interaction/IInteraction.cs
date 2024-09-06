using UnityEngine;

public interface IInteraction
{
    [field: Header("Interaction Properties")]
    public string InteractionPrompt { get; }
    public Sprite InteractSprite { get; }
    public bool IsInteractionEnabled { get; }


    public abstract void Interact(Transform interactor);

    public abstract void SetInteractionEnabled(bool enabled);
}
