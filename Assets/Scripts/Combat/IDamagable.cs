using UnityEngine;

public interface IDamagable
{
    public bool IsDamagable { get; }

    [field: Header("Damage Reduction")]
    public int Defence { get; }

    public float BludgeoningResistance { get; }
    public float ExplosiveResistance { get; }
    public float PiercingResistance { get; }
    public float SlashingResistance { get; }

    public void SetDefence(int value);

    /// <summary>
    /// Calculates the damagables resistance to a specified damage type.
    /// </summary>
    /// <param name="damageType">The type of damage being checked for resistance against.</param>
    /// <returns>The damagables resistance to the specified damage type.</returns>
    public float CalculateDamageResist(EDamageType damageType);

    public void TakeDamage(int amount);
}
