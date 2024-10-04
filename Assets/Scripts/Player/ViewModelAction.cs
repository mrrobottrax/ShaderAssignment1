using UnityEngine;

public abstract class ViewModelAction
{
    /// <summary>
    /// This method executs a viewmodel actions logic
    /// </summary>
    public abstract void Execute(PlayerHealth player, PlayerViewmodelManager viewModelManager, Vector3 attackPos, WeaponItem weaponItem, AttackList.Attack attack, string actionTitle);
}