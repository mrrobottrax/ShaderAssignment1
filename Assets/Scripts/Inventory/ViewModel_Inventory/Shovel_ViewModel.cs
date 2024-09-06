using UnityEngine;

public class Shovel_ViewModel : ViewModel_Base
{
    [SerializeField] static private LayerMask _digLayer;

    private static FirstPersonCamera firstPersonCamera;

    private void Awake()
    {
        // Add available actions to the functions list
        functions.Add("Shovel_Melee", new MeleeAction());
        functions.Add("Shovel_Dig", new DigAction());
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

            // Perform the shovels melee attack
            player.EntityPerformOngoingAttack(viewModelManager, attack.AttackData, attack.AttackPosition.position,
                firstPersonCamera.GetCamera().forward, weaponData.BaseDamage, 0, weaponData.BaseWeaponRange);
        }
    }

    private class DigAction : ViewModelAction
    {

        public override void Execute(Player player, PlayerViewModelManager viewModelManager, ViewModel_Base viewModel, Weapon_Item weaponItem, AttackList.Attack attack)
        {
            Weapon_ItemData weaponData = weaponItem.GetWeaponData();

            // See if the ray hits a digsite
            RaycastHit hit;
            if (Physics.Raycast(attack.AttackPosition.position, firstPersonCamera.GetCamera().forward, 
                out hit, weaponData.BaseWeaponRange + attack.AttackData.AttackRange, attack.AttackData.AffectedLayers, QueryTriggerInteraction.Collide))
            {
                // Try and advance the digsites stage
                if (hit.transform.TryGetComponent(out DigSite digSite))
                    digSite.TryAdvanceStage();
            }
        }
    }
    #endregion
}

