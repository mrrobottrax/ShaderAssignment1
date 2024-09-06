using UnityEngine;

public class DestructableObject : MonoBehaviour, IHealthComponent, IDamagable
{
    [field: Header("Health")]
    public int Health => health;
    private int health;

    public int MaxHealth => maxHealth;
    [SerializeField] private int maxHealth;

    public bool IsDead => isDead;
    private bool isDead;

    public bool IsDamagable => _isDamagable;
    [SerializeField] private bool _isDamagable = true;

    [field: Header("Damage Reduction")]
    public int Defence => _defence;
    [SerializeField] private int _defence;

    public float BludgeoningResistance => _bludgeoningResistance;
    [SerializeField, Range(0, 1)] private float _bludgeoningResistance;

    public float ExplosiveResistance => _explosiveResistance;
    [SerializeField, Range(0, 1)] private float _explosiveResistance;

    public float PiercingResistance => _piercingResistance;
    [SerializeField, Range(0, 1)] private float _piercingResistance;

    public float SlashingResistance => _slashingResistance;
    [SerializeField, Range(0, 1)] private float _slashingResistance;

    #region Initialization Methods

    private void Awake()
    {
        health = maxHealth;
    }
    #endregion

    #region Health Methods

    /// <summary>
    /// Sets an entities health directly
    /// </summary>
    public virtual void SetHealth(int value)
    {
        health = value;
        health = Mathf.Clamp(health, 0, maxHealth);

        Debug.Log(health);

        if (health <= 0)
            isDead = true;
    }

    public virtual void AddHealth(int amountAdded)
    {
        SetHealth(health + amountAdded);
    }

    public virtual void RemoveHealth(int amountRemoved)
    {
        SetHealth(health - amountRemoved);
    }
    #endregion

    #region Defence Methods

    /// <summary>
    /// Sets the defence value of an entity.
    /// </summary>
    /// <param name="newDefence">The new value of defence the entity should have.</param>
    public void SetDefence(int newDefence)
    {
        _defence = newDefence;
    }

    /// <summary>
    /// Calculates the entity's resistance to a specified damage type.
    /// </summary>
    /// <param name="damageType">The type of damage being checked for resistance against.</param>
    /// <returns>The entity's resistance to the specified damage type.</returns>
    public float CalculateDamageResist(EDamageType damageType)
    {
        switch (damageType)
        {
            case EDamageType.Bludgeoning:
                return 1 - _bludgeoningResistance;
            case EDamageType.Explosive:
                return 1 - _explosiveResistance;
            case EDamageType.Slashing:
                return 1 - _slashingResistance;
            case EDamageType.Piercing:
                return 1 - _piercingResistance;
        }

        return 1;
    }

    public void TakeDamage(int amount)
    {
        RemoveHealth(amount);
    }
    #endregion
}
