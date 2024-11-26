using UnityEngine;

public class ThrowableItem : UseableItem
{
	[SerializeField] float _throwForce = 10;
	[SerializeField] Vector3 _throwOffset = new(0, 0, 0.3f);

    public override void TryItemFunction(PlayerStats player, PlayerViewmodelManager viewModelManager, Vector3 attackPos, string actionTitle, AttackData attack = null)
    {
        base.TryItemFunction(player, viewModelManager, attackPos, actionTitle, attack);

        if (actionTitle == "Throw")
        {
            // Perform the spears throwing attack
            ownerInventory.TryDropActiveItem(_throwForce, _throwOffset, Quaternion.Euler(90, 0, 0));

            /*
            player.EntityPerformOngoingAttack(viewModelManager, attack.AttackData, attackPos,
                player.FirstPersonCamera.CameraTransform.forward, BaseDamage, 0, BaseWeaponRange);
            */
        }
    }
}
