using UnityEngine;

public class ShovelItem : WeaponItem
{
    public override void TryItemFunction(PlayerStats player, PlayerViewmodelManager viewModelManager, Vector3 attackPos, string actionTitle, AttackData attack = null)
    {
        base.TryItemFunction(player, viewModelManager, attackPos, actionTitle, attack);

        switch (actionTitle)
        {
            case "Shovel_Melee":

                // Perform the shovels melee attack
                player.EntityPerformOngoingAttack(viewModelManager, attack, attackPos,
                    player.FirstPersonCamera.CameraTransform.forward, BaseDamage, 0, BaseWeaponRange);

                break;

            case "TwoHandedAttack":

                // See if the ray hits a digsite
                RaycastHit hit;
                if (Physics.Raycast(attackPos, player.FirstPersonCamera.CameraTransform.forward,
                    out hit, BaseWeaponRange, attack.AffectedLayers, QueryTriggerInteraction.Collide))
                {
                    // Try and advance the digsites stage
                    if (hit.transform.TryGetComponent(out OreVein digSite))
                        digSite.SpawnOre(hit.point, player.FirstPersonCamera.CameraTransform.forward);
                }

                break;
        }
    }
}
