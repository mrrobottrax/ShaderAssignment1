using UnityEngine;

[RequireComponent(typeof(PlayerController), typeof(PlayerInventory))]
public class PlayerStats : Entity_Base
{
    [field: Header("Stamina")]
    [SerializeField] private float maxStamina;
    private float stamina;
    private float toxins;

    [field: Header("Components")]
    [field: SerializeField] public FirstPersonCamera FirstPersonCamera { get; private set; }
    public PlayerController PlayerController { get; private set; }

    #region Initialization Methods

    protected override void Awake()
    {
        base.Awake();
    }

    protected override void Start()
    {
        base.Start();

        PlayerController = GetComponent<PlayerController>();
    }
    #endregion

    #region Stat Methods

    public override void SetHealth(int value)
    {
        base.SetHealth(value);

        PlayerUIManager.HUDManager.HealthBar.SetHealthBar(value);
    }

    public void SetStamina(float value)
    {
        stamina = Mathf.Clamp(value, 0, maxStamina - toxins);
    }

    public void SetToxins(float value)
    {
        toxins = Mathf.Clamp(value, 0, maxStamina);
    }

    #endregion

    #region Utility Methods

    public float GetStamina()
    {
        return stamina;
    }

    public float GetToxins()
    {
        return stamina;
    }
    #endregion
}
