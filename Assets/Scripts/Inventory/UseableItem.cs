using UnityEngine;

public class UseableItem : Item
{
    // System
    protected PlayerViewmodelManager playerViewmodelManager;

    #region Item Functionality

    protected override void PickUp(PlayerInteraction interactor)
    {
        base.PickUp(interactor);
        playerViewmodelManager = interactor.GetComponentInChildren<PlayerViewmodelManager>();
    }

    public virtual void Fire1(bool pressed) { }
    public virtual void Fire2(bool pressed) { }
    #endregion

    #region ViewModel Functionality

    /// <summary>
    /// This method executes a view model's function based on the action title passed in.
    /// </summary>
    public virtual void TryModelFunction(PlayerHealth player, PlayerViewmodelManager viewModelManager, Vector3 functionPos, string actionTitle, AttackList.Attack attack = null) { }
    #endregion
}
