using UnityEngine;

public class UseableItem : Item
{
    // System
    protected PlayerViewmodelManager playerViewmodelManager;
    protected PlayerInventory ownerInventory;

    #region Item Functionality

    protected override void PickUp(PlayerInteraction interactor)
    {
        base.PickUp(interactor);

        ownerInventory = interactor.GetComponent<PlayerInventory>();
        playerViewmodelManager = interactor.GetComponentInChildren<PlayerViewmodelManager>();
    }

    public override void Drop()
    {
        base.Drop();
        ownerInventory = null;
    }
    #endregion

    #region ViewModel Functionality

    /// <summary>
    /// This method executes a view model's function based on the action title passed in.
    /// </summary>
    public virtual void TryModelFunction(PlayerHealth player, PlayerViewmodelManager viewModelManager, Vector3 functionPos, string actionTitle, AttackList.Attack attack = null) { }
    #endregion
}
