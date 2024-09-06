using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Hurtbox : MonoBehaviour, IDamagable
{
    [Header("Entity")]
    private Entity_Base entityBase;

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

    [Header("System")]
    private Collider col;

    #region Initialization Methods

    public void Initialize(Entity_Base entity)
    {
        entityBase = entity;

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
        entityBase.RemoveHealth(amount);
    }
    #endregion

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out Projectile projectile))
            projectile.TouchedHurbox(this);
    }
}
