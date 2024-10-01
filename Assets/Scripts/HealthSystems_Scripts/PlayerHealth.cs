using UnityEngine;

[RequireComponent(typeof(PlayerController), typeof(PlayerInventory))]
public class PlayerHealth : Entity_Base
{ 
    [field: Header("Components")]
    [field: SerializeField] public FirstPersonCamera FirstPersonCamera { get; private set; }

    [field: SerializeField] private PlayerController playerController;
    [field: SerializeField] private PlayerInventory inventoryComponent;
    [field: SerializeField] private PlayerViewmodelManager playerViewModelManager;

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

    public PlayerInventory GetPlayerInventory()
    {
        return inventoryComponent;
    }

    public FirstPersonCamera GetPlayerCamera()
    {
        return FirstPersonCamera;
    }

    public PlayerViewmodelManager GetViewModelManager()
    {
        return playerViewModelManager;
    }

    #endregion
}
