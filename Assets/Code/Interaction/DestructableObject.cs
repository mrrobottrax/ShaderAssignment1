using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class DestructableObject : MonoBehaviour, IHealthComponent, IDamagable
{
    [field: Header("Health")]
    private int health;
    public int Health => health;

    public int MaxHealth => maxHealth;
    [SerializeField] private int maxHealth;

    public bool IsDead => isDead;
    private bool isDead;

    public bool IsDamagable => isDamagable;
    [SerializeField] private bool isDamagable = true;

    // Components
    public AudioSource AudioSource => audioSource;
    protected AudioSource audioSource;

    [field: Header("Damage Reduction")]
    [SerializeField] private int defence;
    public int Defence => defence;

    public float BludgeoningResistance => bludgeoningResistance;
    [SerializeField, Range(0, 1)] private float bludgeoningResistance;

    public float ExplosiveResistance => explosiveResistance;
    [SerializeField, Range(0, 1)] private float explosiveResistance;

    public float PiercingResistance => piercingResistance;
    [SerializeField, Range(0, 1)] private float piercingResistance;

    public float SlashingResistance => slashingResistance;
    [SerializeField, Range(0, 1)] private float slashingResistance;


    [Header("SFX")]
    [SerializeField] private AudioClip destroySFX;

    #region Initialization Methods

    private void Awake()
    {
        health = maxHealth;
        audioSource = GetComponent<AudioSource>();
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

        if (health <= 0)
        {
            if (destroySFX != null)
                audioSource.PlayOneShot(destroySFX);

            isDead = true;
            Destroy(gameObject);
        }
    }

    public virtual void AddHealth(int amountAdded) =>
        SetHealth(health + amountAdded);

    public virtual void RemoveHealth(int amountRemoved) => 
        SetHealth(health - amountRemoved);
    #endregion

    #region Defence Methods

    /// <summary>
    /// Sets the defence value of an entity.
    /// </summary>
    /// <param name="newDefence">The new value of defence the entity should have.</param>
    public void SetDefence(int newDefence)
    {
        defence = newDefence;
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
                return 1 - bludgeoningResistance;
            case EDamageType.Explosive:
                return 1 - explosiveResistance;
            case EDamageType.Slashing:
                return 1 - slashingResistance;
            case EDamageType.Piercing:
                return 1 - piercingResistance;
        }

        return 1;
    }

    public virtual void TakeDamage(int amount, Vector3 hitPos, Vector3 hitDirection) =>
    RemoveHealth(amount);

    #endregion
}
