using UnityEngine;

public class WeaponItem : UseableItem
{
    [field: Header("Weapon Data")]
    [field: SerializeField] public int BaseDamage { get; private set; }
    [field: SerializeField] public float BaseWeaponRange { get; private set; }

    [field: Header("Functions")]
    [field: SerializeField] public AttackList ViewModelAttacks { get; private set; }
}