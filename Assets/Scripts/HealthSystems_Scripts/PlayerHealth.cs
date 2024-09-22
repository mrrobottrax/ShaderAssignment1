using UnityEngine;

[RequireComponent(typeof(PlayerController), typeof(PlayerInventoryComponent))]
public class PlayerHealth : Entity_Base
{ 
    [field: Header("Components")]
    [field: SerializeField] public PlayerUIManager PlayerUIManager { get; private set; }
    [field: SerializeField] public FirstPersonCamera FirstPersonCamera { get; private set; }

    [field: SerializeField] private PlayerController playerController;
    [field: SerializeField] private PlayerInventoryComponent playerInventoryComponent;
    [field: SerializeField] private PlayerViewModelManager playerViewModelManager;

    #region Initialization Methods
    protected override void Awake()
    {
        base.Awake();
    }

    protected override void Start()
    {
        base.Start();
    }
    #endregion

    public override void SetHealth(int value)
    {
        base.SetHealth(value);

        PlayerUIManager.HUDManager.HealthBar.SetHealthBar(value);
    }

    #region Helper Methods
    public PlayerController GetPlayerController()
    {
        return playerController;
    }

    public PlayerInventoryComponent GetPlayerInventory()
    {
        return playerInventoryComponent;
    }

    public FirstPersonCamera GetPlayerCamera()
    {
        return FirstPersonCamera;
    }

    public PlayerViewModelManager GetViewModelManager()
    {
        return playerViewModelManager;
    }

    #endregion
}
