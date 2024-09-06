using UnityEngine;

public interface ITickingStatusEffectData
{

    [field: Header("Application Properties")]
    public bool IsStackable { get; }
    public bool IsInfinite { get; }

    [field: Header("Tick Properties")]
    public float TickSpeed { get; } // The amount of seconds that pass before the effect is applied again.
    public float Duration { get; }  // The duration of the effect in seconds

    /// <summary>
    /// Return a new instnace of a status effect using the scriptable object as the base data
    /// </summary>
    public abstract StatusEffectObject CreateEffectInstance();

    /// <summary>
    /// This method defines how an effect affects a target per tick
    /// </summary>
    public abstract void TickEffect(IHealthComponent target);
}
