using UnityEngine;

[RequireComponent(typeof(PlayerController), typeof(PlayerInventoryComponent), typeof(FirstPersonCamera))]
public class Player : Entity_Base
{
    public static Player Instance;

    [Header("Components")]
    private PlayerController playerController;
    private PlayerInventoryComponent playerInventoryComponent;
    private FirstPersonCamera firstPersonCamera;
    private PlayerViewModelManager playerViewModelManager;

    #region Initialization Methods
    protected override void Awake()
    {
        base.Awake();

        Instance = this;

        // Cahce player components
        playerController = GetComponent<PlayerController>();
        playerInventoryComponent = GetComponent<PlayerInventoryComponent>();
        firstPersonCamera = GetComponent<FirstPersonCamera>();
        playerViewModelManager = GetComponentInChildren<PlayerViewModelManager>();

        // Send a reference to the player to the GameManager
        GameManager.Instance.SetPlayer(this);
    }

    protected override void Start()
    {
        base.Start();
    }
    #endregion

    public override void SetHealth(int value)
    {
        base.SetHealth(value);

        UIManager.Instance.HUDManager.HealthBar.SetHealthBar(value);
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
        return firstPersonCamera;
    }

    public PlayerViewModelManager GetViewModelManager()
    {
        return playerViewModelManager;
    }

    #endregion
}
