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

    public virtual void Fire1(bool pressed)
    {
        if (!pressed) return;
        playerViewmodelManager.Animator.SetTrigger("Fire1");
    }

    public virtual void Fire2(bool pressed)
    {
        if (!pressed) return;
        playerViewmodelManager.Animator.SetTrigger("Fire2");
    }

    /// <summary>
    /// This method executes a view model's function based on the action title passed in.
    /// </summary>
    public virtual void TryModelFunction(PlayerHealth player, PlayerViewmodelManager viewModelManager, Vector3 functionPos, AttackList.Attack attack = null, string actionTitle = "") { }
    #endregion
}
