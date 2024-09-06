using Cinemachine;
using System;
using UnityEngine;

public class Explosion : MonoBehaviour, ICombatPacketCompleter
{
    [field: Header("Components")]
    [SerializeField] private CinemachineImpulseSource _cinemachineImpulseSource;
    private ExplosionData explosionData;

    [field: Header("System")]
    private LayerMask affectedLayers = ~0;

    [field: Header("Combat Packet")]
    private CombatPacket packetRef;
    private event Action onAttackCompleted;
    public event Action OnAttackCompleted
    {
        add
        {
            onAttackCompleted += value;
        }

        remove
        {
            onAttackCompleted -= value;
        }
    }

    public void CompleteAttack()
    {
        onAttackCompleted.Invoke();

        // Clear packet ref
        packetRef = null;
    }

    /// <summary>
    /// Initializing will establish what data an explosion uses and the CombatPacket it will complete
    /// </summary>
    public void Initialize(ExplosionData explosionData, CombatPacket packetRef = null, LayerMask affectedLayers = default)
    {
        this.explosionData = explosionData;
        this.packetRef = packetRef;

        // Set the affected layers, default to all layers if not specified
        this.affectedLayers = (affectedLayers == default) ? ~0 : affectedLayers;

        Explode();
    }

    /// <summary>
    /// Check for victems in a defined blast radius and complete the combat packet if one is present
    /// </summary>
    public void Explode()
    {
        Vector3 explosionPos = transform.position;
        Collider[] colliders = Physics.OverlapSphere(explosionPos, explosionData.BlastRadius, affectedLayers, QueryTriggerInteraction.Collide);

        foreach (Collider hit in colliders)
        {
            // Add damage to each victem
            if (packetRef != null && hit.gameObject.TryGetComponent(out IDamagable damagable) && damagable.IsDamagable)
                packetRef.AddVictim(damagable, explosionPos);

            // Add explosion force to each rb
            if (hit.TryGetComponent(out Rigidbody rb))
                rb.AddExplosionForce(explosionData.BlastForce, explosionPos, explosionData.BlastRadius);
        }

        // Invoke the packet complete event
        if (packetRef != null)
            CompleteAttack();

        // Start screen shake
        _cinemachineImpulseSource?.GenerateImpulse();
    }
}
