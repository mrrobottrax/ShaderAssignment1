using UnityEngine;

public class Weapon_ItemData : ItemData_Base
{
    [field: Space(10)]

    [field: Header("Weapon Data")]
    [field: SerializeField] public int BaseDamage { get; private set; }
    [field: SerializeField] public float BaseWeaponRange { get; private set; }

    [field: Header("View Model")]
    [field: SerializeField] public int ViewModelID { get; private set; }
}
