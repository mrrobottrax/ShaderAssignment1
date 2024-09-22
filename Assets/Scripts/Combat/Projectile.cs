using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Projectile : MonoBehaviour, ICombatPacketCompleter
{
    [field: Header("Components")]
    [SerializeField] private Collider _projectileDamageCollider;
    private Rigidbody rb;
    private ProjectileData projectileData;

    [field: Header("System")]
    private float timeRemaining;
    private bool isIntercepted;
    private LayerMask affectedLayers;

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

    private void Awake()
    {
        if (_projectileDamageCollider == null)
            Debug.LogWarning(name + "Is missing a damage collider");
    }

    /// <summary>
    /// Initializing should be done when a projectile is created. Here the data of the projectile, its starting position, and its starting direction should be set.
    /// </summary>
    public void Initialize(ProjectileData projectileData, Vector3 startPos, Vector3 dir, float force, CombatPacket packetRef, LayerMask affectedLayers = default, Vector3 launcherVel = default)
    {
        this.projectileData = projectileData;
        this.packetRef = packetRef;

        // Ensure the projectile game object is enabled
        gameObject.SetActive(true);

        // Cache rigidbody
        rb = GetComponent<Rigidbody>();

        // Ensure no initial velocity before applying the force
        rb.velocity = launcherVel == Vector3.zero? Vector3.zero: launcherVel;
        rb.angularVelocity = Vector3.zero;

        // Set pos and initial direction
        transform.position = startPos;
        transform.rotation = Quaternion.LookRotation(dir);

        // Send projectile in the direction it is facing
        rb.AddForce(transform.forward.normalized * force, ForceMode.Impulse);

        // Start timer
        timeRemaining = projectileData.ProjectileLifetime;

        // Set the affected layers, default to all layers if not specified
        this.affectedLayers = (affectedLayers == default) ? ~0 : affectedLayers;
    }

    private void Update()
    {
        if(timeRemaining > 0)
        {
            timeRemaining -= Time.deltaTime;

            // Complete the packet once time is up
            if (timeRemaining <= 0 && packetRef != null)
            {
                CompleteAttack();
                Destroy(gameObject);
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!isIntercepted)
        {
            bool correctColliderHit = false; // Ensure the collision was on the right one to deal damage

                foreach (ContactPoint i in collision.contacts)
                    if (i.thisCollider == _projectileDamageCollider)
                        correctColliderHit = true;

                // Since the correct collider was hit, hit the victem(s).
                if (correctColliderHit)
                {
                // Get potential victems of the collision
                if (packetRef != null && (affectedLayers & (1 << collision.collider.gameObject.layer)) != 0 && collision.collider.TryGetComponent(out IDamagable damagable))
                    packetRef.AddVictim(damagable, transform.position);

                switch (projectileData.HitResult)
                    {
                        case ProjectileData.EProjectileResult.Damage:

                            isIntercepted = true;
                            break;

                        case ProjectileData.EProjectileResult.Destroy:
                            // Stop multiple collisions from occuring before destruction
                            isIntercepted = true;

                            // Complete the damage packet that is listening for this projectile
                            CompleteAttack();

                            // Destroy the projectile gameobject
                            Destroy(transform.gameObject);
                            break;

                        case ProjectileData.EProjectileResult.Explode:

                            // Stop multiple collisions from occuring before explosion
                            isIntercepted = true;

                            // Spawn the explosion
                            projectileData.ExplosionData.CreateExplosionInstance(transform.position, packetRef.Instigator, affectedLayers);

                            // Complete the damage packet that is listening for this projectile
                            CompleteAttack();

                            // Destroy the projectile gameobject
                            Destroy(transform.gameObject);
                            break;
                    }
                }
        }
    }

    public void TouchedHurbox(Hurtbox hurtbox)
    {
        if (!isIntercepted)
        {

            // Get potential victems of the collision
            if (packetRef != null && (affectedLayers & (1 << hurtbox.gameObject.layer)) != 0)
                packetRef.AddVictim(hurtbox, transform.position);

            switch (projectileData.HitResult)
            {
                case ProjectileData.EProjectileResult.Damage:

                    isIntercepted = true;
                    break;

                case ProjectileData.EProjectileResult.Destroy:
                    // Stop multiple collisions from occuring before destruction
                    isIntercepted = true;

                    // Complete the damage packet that is listening for this projectile
                    CompleteAttack();

                    // Destroy the projectile gameobject
                    Destroy(transform.gameObject);
                    break;

                case ProjectileData.EProjectileResult.Explode:

                    // Stop multiple collisions from occuring before explosion
                    isIntercepted = true;

                    // Spawn the explosion
                    projectileData.ExplosionData.CreateExplosionInstance(transform.position, packetRef.Instigator, affectedLayers);

                    // Complete the damage packet that is listening for this projectile
                    CompleteAttack();

                    // Destroy the projectile gameobject
                    Destroy(transform.gameObject);
                    break;
            }
        }
    }

    public void CompleteAttack()
    {
        onAttackCompleted.Invoke();

        // Clear packet ref
        packetRef = null;
    }
}
