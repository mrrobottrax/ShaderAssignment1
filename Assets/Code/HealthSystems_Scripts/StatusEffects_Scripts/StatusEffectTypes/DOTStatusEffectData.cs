using UnityEngine;

[CreateAssetMenu(fileName = "DamageOverTimeEffect", menuName = "StatusEffects/DamageOverTimeEffect")]
public class DOTStatusEffectData : StatusEffectData_Base, ITickingStatusEffectData
{
    public bool IsStackable => _isStackable;
    [SerializeField] private bool _isStackable;

    public bool IsInfinite => _isInfinite;
    [SerializeField] private bool _isInfinite;

    public float TickSpeed => _tickSpeed;
    [SerializeField] private float _tickSpeed;

    public float Duration => _duration;

    [SerializeField] private float _duration;

    [SerializeField] private int _damagePerTick;

    public override void OnApplyEffect(IHealthComponent target)
    {
        target.RemoveHealth(_damagePerTick);
    }

    public override void OnRemoveEffect(IHealthComponent target)
    {

    }

    public void TickEffect(IHealthComponent target)
    {
        target.RemoveHealth(_damagePerTick);
    }

    public StatusEffectObject CreateEffectInstance()
    {
        return new StatusEffectObject(this, this);
    }
}
