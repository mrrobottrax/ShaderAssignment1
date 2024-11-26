using UnityEngine;

[RequireComponent(typeof(PlayerController), typeof(PlayerInventory))]
public class PlayerStats : Entity_Base
{
    [field: Header("Stamina")]
    [SerializeField] private float maxStamina;
    [SerializeField] private float staminaRecovery = 2f;
    [field: SerializeField] public float SprintStaminaReduction { get; private set; } = 10f;
    [field: SerializeField] public float JumpStaminaReduction { get; private set; } = 20f;
    public float Stamina { get; private set; }

    [field: Header("Toxins")]
    [SerializeField] private float toxinRecovery = 0.5f;

    private int toxinLevel;
    private float toxins;

    [field: Header("Clock")]
    [SerializeField] private float clockCheckDist = 10;
    [SerializeField] private LayerMask clockLayers;
    private bool isInClockRange;

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

        // To Do
        // Call this when spawning the player at a new mine
        RefreshOnSpawn();
    }

    private void RefreshOnSpawn()
    {
        health = MaxHealth;
        Stamina = maxStamina;
    }
    #endregion

    #region Unity Callbacks

    private void Update()
    {
        DetermineBloodToxins();
        RegenStamina();

        CheckClockDisplayZone();
    }

    private void CheckClockDisplayZone()
    {
        bool prevInRange = isInClockRange;

        Collider[] hits = Physics.OverlapSphere(transform.position, clockCheckDist, clockLayers, queryTriggerInteraction: QueryTriggerInteraction.Collide);

        // Check if any object has the ClockDisplayZone component
        isInClockRange = false;
        foreach (var hit in hits)
        {
            if (hit.TryGetComponent(out ClockActivator clockActivator) && clockActivator != null && clockActivator.IsActive)
            {
                isInClockRange = true;
                break;
            }
        }

        // Toggle the clock based on the result
        if (prevInRange != isInClockRange)
            PlayerUIInstance.HUDManager.Clock.ToggleClock(isInClockRange);
    }

    #endregion

    #region Stat Methods

    public override void SetHealth(int value)
    {
        base.SetHealth(value);
    }

    public void SetStamina(float value)
    {
        Stamina = Mathf.Clamp(value, 0, maxStamina - toxins);

        PlayerUIInstance.HUDManager.O2Bar.SetValue(Mathf.FloorToInt(Stamina), Mathf.FloorToInt(toxins));
    }

    public void SetToxins(float value)
    {
        toxins = Mathf.Clamp(value, 0, maxStamina);

        PlayerUIInstance.HUDManager.O2Bar.SetValue(Mathf.FloorToInt(Stamina), Mathf.FloorToInt(toxins));
    }
    #endregion

    #region Regeneration Methods

    #region Toxin Level Methods

    public void SetToxinLevel(int value)
    {
        toxinLevel = value;

        // Clamp
        if (toxinLevel < 0)
            toxinLevel = 0;

        PlayerUIInstance.HUDManager.O2Bar.SetValue(Mathf.FloorToInt(Stamina), Mathf.FloorToInt(toxins));
    }

    public void AddToxinLevel(int value)
    {
        SetToxinLevel(toxinLevel + value);
    }

    public void RemoveToxinLevel(int value)
    {
        SetToxinLevel(toxinLevel - value);
    }
    #endregion

    private void RegenStamina()
    {
        if (!PlayerController.IsSprinting && Stamina < maxStamina)
            SetStamina(Stamina + staminaRecovery * Time.deltaTime);
    }

    private void DetermineBloodToxins()
    {
        if (toxinLevel > 0)
            SetToxins(toxins + toxinLevel * Time.deltaTime);
        else if (toxins > 0)
            SetToxins(toxins - toxinRecovery * Time.deltaTime);
    }
    #endregion

    #region Utility Methods

    public float GetStamina()
    {
        return Stamina;
    }

    public float GetToxins()
    {
        return Stamina;
    }
    #endregion
}
