using UnityEngine;

[System.Serializable]
public abstract class StatusEffectData_Base : ScriptableObject
{
    [field: Header("Data")]
    // This determines if UI will show the effect when the player has it.
    [field: SerializeField] public bool IsEffectDisplayable { get; private set; } = true;
    [field: SerializeField] public string Name { get; private set; }
    [field: SerializeField] public Sprite EffectIcon { get; private set; }
    [field: SerializeField] public string Description { get; private set; }

    /// <summary>
    /// This method defines how an effect affects a target when it is applied
    /// </summary>
    public abstract void OnApplyEffect(IHealthComponent target);

    /// <summary>
    /// This method defines how an effect affects a target when it is removed
    /// </summary>
    public abstract void OnRemoveEffect(IHealthComponent target);
}

