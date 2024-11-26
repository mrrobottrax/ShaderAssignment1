using UnityEngine;

public abstract class ViewModelAction
{
    /// <summary>
    /// This method executs a viewmodel actions logic
    /// </summary>
    public abstract void Execute(PlayerStats player, Vector3 attackPos, WeaponItem weaponItem, string actionTitle, AttackData attack = null);
}