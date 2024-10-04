using UnityEngine;

public class ShovelItem : WeaponItem
{
    public override void TryModelFunction(PlayerHealth player, PlayerViewmodelManager viewModelManager, Vector3 attackPos, string actionTitle, AttackList.Attack attack = null)
    {
        switch (actionTitle)
        {
            case "Shovel_Melee":

                // Perform the shovels melee attack
                player.EntityPerformOngoingAttack(viewModelManager, attack.AttackData, attackPos,
                    player.FirstPersonCamera.CameraTransform.forward, BaseDamage, 0, BaseWeaponRange);

                break;

            case "Shovel_Dig":

                // See if the ray hits a digsite
                RaycastHit hit;
                if (Physics.Raycast(attackPos, player.FirstPersonCamera.CameraTransform.forward,
                    out hit, BaseWeaponRange + attack.AttackData.AttackRange, attack.AttackData.AffectedLayers, QueryTriggerInteraction.Collide))
                {
                    // Try and advance the digsites stage
                    if (hit.transform.TryGetComponent(out OreVein digSite))
                        digSite.SpawnOre(hit.point);
                }

                break;
        }
    }
}
