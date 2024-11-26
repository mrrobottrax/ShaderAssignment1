using UnityEngine;

[CreateAssetMenu(fileName = "RegeneratingEffect", menuName = "StatusEffects/RegeneratingEffect")]
public class RegeneratingStatusEffect : StatusEffectData_Base, ITickingStatusEffectData
{
    public bool IsStackable => _isStackable;
    [SerializeField] private bool _isStackable;

    public bool IsInfinite => _isInfinite;
    [SerializeField] private bool _isInfinite;

    public float TickSpeed => _tickSpeed;
    [SerializeField] private float _tickSpeed;

    public float Duration => _duration;

    [SerializeField] private float _duration;

    [SerializeField] private int _healthPerTick;

    public override void OnApplyEffect(IHealthComponent target)
    {
        target.RemoveHealth(_healthPerTick);
    }

    public override void OnRemoveEffect(IHealthComponent target)
    {

    }

    public void TickEffect(IHealthComponent target)
    {
        target.AddHealth(_healthPerTick);
    }

    public StatusEffectObject CreateEffectInstance()
    {
        return new StatusEffectObject(this, this);
    }
}