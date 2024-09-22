public class Spear_ViewModel : ViewModel_Base
{
    private void Awake()
    {
        // Add available actions to the functions list
        functions.Add("Spear_Melee", new MeleeAction());
        functions.Add("Spear_Throw", new ThrowAction());
    }

    #region Attack Functions
    private class MeleeAction : ViewModelAction
    {

        public override void Execute(PlayerHealth player, PlayerViewModelManager viewModelManager, ViewModel_Base viewModel, Weapon_Item weaponItem, AttackList.Attack attack)
        {
            Weapon_ItemData weaponData = weaponItem.GetWeaponData();

            // Perform the spears melee attack
            player.EntityPerformOngoingAttack(viewModelManager, attack.AttackData, attack.AttackPosition.position,
                player.FirstPersonCamera.CameraTransform.forward, weaponData.BaseDamage, 0, weaponData.BaseWeaponRange);
        }
    }

    private class ThrowAction : ViewModelAction
    {

        public override void Execute(PlayerHealth player, PlayerViewModelManager viewModelManager, ViewModel_Base viewModel, Weapon_Item weaponItem, AttackList.Attack attack)
        {
            if (weaponItem is RangedWeapon_Item rangedWeapon_Item)
            {
                // Disable spear model
                viewModel.SetPrimaryMeshActive(false);

                Weapon_ItemData weaponData = weaponItem.GetWeaponData();

                // Perform the spears throwing attack
                player.EntityPerformOngoingAttack(viewModelManager, attack.AttackData, attack.AttackPosition.position,
                    player.FirstPersonCamera.CameraTransform.forward, weaponData.BaseDamage, 0, weaponData.BaseWeaponRange);

                // Remove one from the item amount
                rangedWeapon_Item.ProjectileCreated(player, 1);
            }
        }
    }
    #endregion
}
