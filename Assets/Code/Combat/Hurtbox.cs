using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Hurtbox : MonoBehaviour, IDamagable
{
    [Header("Entity")]
    private Entity_Base entityBase;

    public bool IsDamagable => isDamagable;
    [SerializeField] private bool isDamagable = true;

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

    [field: Header("Components")]
    public AudioSource AudioSource => audioSource;
    protected AudioSource audioSource;

    [Header("System")]
    private Collider col;

    #region Initialization Methods

    public void Initialize(Entity_Base entity)
    {
        entityBase = entity;

        audioSource = GetComponent<AudioSource>();
        col = GetComponent<Collider>();

        col.isTrigger = true;
    }
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

    public void TakeDamage(int amount, Vector3 hitPos, Vector3 hitDirection) =>
     entityBase.RemoveHealth(amount);
    #endregion

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out Projectile projectile))
            projectile.TouchedHurbox(this);
    }
}
