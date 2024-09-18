using UnityEngine;

public class PortableCamera_ViewModel : ViewModel_Base
{
    private static FirstPersonCamera firstPersonCamera;

    private void Awake()
    {
        // Add available actions to the functions list
        functions.Add("Shoot", new ShootAction());
    }

    private void Start()
    {
        firstPersonCamera = Player.Instance.GetPlayerCamera();
    }

    #region Attack Functions

    private class ShootAction : ViewModelAction
    {

        public override void Execute(Player player, PlayerViewModelManager viewModelManager, ViewModel_Base viewModel, Weapon_Item weaponItem, AttackList.Attack attack)
        {
            Weapon_ItemData weaponData = weaponItem.GetWeaponData();

            
        }
    }
    #endregion
}
