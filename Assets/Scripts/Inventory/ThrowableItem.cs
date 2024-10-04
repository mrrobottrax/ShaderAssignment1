using UnityEngine;

public class ThrowableItem : UseableItem
{
	[SerializeField] float _throwForce = 10;
	[SerializeField] Vector3 _throwOffset = new(0, 0, 0.3f);

    public override void TryModelFunction(PlayerHealth player, PlayerViewmodelManager viewModelManager, Vector3 attackPos, AttackList.Attack attack, string actionTitle)
    {
        if (actionTitle == "Throw")
        {
            // Perform the spears throwing attack
            ownerInventory.DropActiveItem(_throwForce, _throwOffset, Quaternion.Euler(90, 0, 0));

            /*
            player.EntityPerformOngoingAttack(viewModelManager, attack.AttackData, attackPos,
                player.FirstPersonCamera.CameraTransform.forward, BaseDamage, 0, BaseWeaponRange);
            */
        }
    }
}
