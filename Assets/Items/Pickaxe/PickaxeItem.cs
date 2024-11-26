using UnityEngine;

public class PickaxeItem : WeaponItem
{
    public override void TryItemFunction(PlayerStats player, PlayerViewmodelManager viewModelManager, Vector3 attackPos, string actionTitle, AttackData attack = null)
    {
        base.TryItemFunction(player, viewModelManager, attackPos, actionTitle, attack);

        switch (actionTitle)
        {
            case "TwoHandedAttack":

                // Perform the pick axes melee attack
                player.EntityPerformOngoingAttack(viewModelManager, attack, attackPos,
                    player.FirstPersonCamera.CameraTransform.forward, BaseDamage, 0, BaseWeaponRange);

                break;
        }
    }
}
