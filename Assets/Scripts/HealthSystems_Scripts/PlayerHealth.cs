using UnityEngine;

[RequireComponent(typeof(PlayerController), typeof(InventoryComponent))]
public class PlayerHealth : Entity_Base
{ 
    [field: Header("Components")]
    [field: SerializeField] public FirstPersonCamera FirstPersonCamera { get; private set; }

    [field: SerializeField] private PlayerController playerController;
    [field: SerializeField] private InventoryComponent inventoryComponent;
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

    public InventoryComponent GetPlayerInventory()
    {
        return inventoryComponent;
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
