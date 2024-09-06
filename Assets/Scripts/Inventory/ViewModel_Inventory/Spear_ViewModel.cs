public class Spear_ViewModel : ViewModel_Base
{
    private static FirstPersonCamera firstPersonCamera;

    private void Awake()
    {
        // Add available actions to the functions list
        functions.Add("Spear_Melee", new MeleeAction());
        functions.Add("Spear_Throw", new ThrowAction());
    }

    private void Start()
    {
        firstPersonCamera = Player.Instance.GetPlayerCamera();
    }

    #region Attack Functions
    private class MeleeAction : ViewModelAction
    {

        public override void Execute(Player player, PlayerViewModelManager viewModelManager, ViewModel_Base viewModel, Weapon_Item weaponItem, AttackList.Attack attack)
        {
            Weapon_ItemData weaponData = weaponItem.GetWeaponData();

            // Perform the spears melee attack
            player.EntityPerformOngoingAttack(viewModelManager, attack.AttackData, attack.AttackPosition.position,
                firstPersonCamera.GetCamera().forward, weaponData.BaseDamage, 0, weaponData.BaseWeaponRange);
        }
    }

    private class ThrowAction : ViewModelAction
    {

        public override void Execute(Player player, PlayerViewModelManager viewModelManager, ViewModel_Base viewModel, Weapon_Item weaponItem, AttackList.Attack attack)
        {
            if (weaponItem is RangedWeapon_Item rangedWeapon_Item)
            {
                // Disable spear model
                viewModel.SetPrimaryMeshActive(false);

                Weapon_ItemData weaponData = weaponItem.GetWeaponData();

                // Perform the spears throwing attack
                player.EntityPerformOngoingAttack(viewModelManager, attack.AttackData, attack.AttackPosition.position,
                    firstPersonCamera.GetCamera().forward, weaponData.BaseDamage, 0, weaponData.BaseWeaponRange);

                // Remove one from the item amount
                rangedWeapon_Item.ProjectileCreated(1);
            }
        }
    }
    #endregion
}
