using UnityEngine;

[CreateAssetMenu(fileName = "Projectiles", menuName = "ExplosionData", order = 1)]
public class ExplosionData : ScriptableObject
{
    [field: Header("Explosion Prefab")]
    [field: SerializeField] public Explosion ExplosionPrefab { get; private set; }

    [field: Header("Explosion Stats")]
    [field: SerializeField] public EDamageType DamageType { get; private set; } = EDamageType.Explosive;
    [field: SerializeField] public float BlastRadius { get; private set; }
    [field: SerializeField] public float BlastForce { get; private set; }
    [field: SerializeField] public int ExplosionDamage { get; private set; }

    [field: Header("Hit Effects")]
    [field: SerializeField] public StatusEffectData_Base[] AppliedEffects { get; private set; }

    /// <summary>
    /// This method creates then returns an instance of this data's explosion prefab
    /// </summary>
    public Explosion CreateExplosionInstance(Vector3 startPos, Entity_Base instigator = null, LayerMask affectedLayers = default)
    {
        // Instantiate then init the explosion
        Explosion explosion = Instantiate(ExplosionPrefab, startPos, Quaternion.identity, null);

        // Assign a combat packet to the explosion if there was an instigator
        if (instigator != null)
        {
            // Begin a packet 
            CombatPacket combatPacket = CombatManager.Instance.BeginCombatPacket(instigator, out int packetID);

            // Set the packets values
            combatPacket.SetPacketDamageVars(DamageType, ExplosionDamage, BlastForce, AppliedEffects);
            combatPacket.SetPacketCompleter(explosion);

            // Start the explosion
            explosion.Initialize(this, combatPacket, affectedLayers: affectedLayers);
        }
        else
        {
            // Start the explosion without an assigned packet
            explosion.Initialize(this, affectedLayers: affectedLayers);
        }

        return explosion;
    }
}
