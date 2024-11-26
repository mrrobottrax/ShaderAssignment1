using UnityEngine;

[CreateAssetMenu(fileName = "HealEffect", menuName = "StatusEffects/HealEffect")]
public class HealStatusEffectData : StatusEffectData_Base
{

    [Header("Healing Properties")]
    [SerializeField] private int _heals;

    public override void OnApplyEffect(IHealthComponent targetEntity)
    {
        // Add the effects magnitude
        targetEntity.AddHealth(_heals);
    }

    public override void OnRemoveEffect(IHealthComponent targetEntity)
    {
        throw new System.NotImplementedException();
    }
}
