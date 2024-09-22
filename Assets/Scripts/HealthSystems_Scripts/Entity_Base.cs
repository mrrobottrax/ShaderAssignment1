using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(StatusEffectComponent))]
public abstract class Entity_Base : MonoBehaviour, IHealthComponent, ICombatPacketCompleter
{
    [field: Header("Health")]
    private int health;
    public int Health => health;

    public int MaxHealth => maxHealth;
    [SerializeField] private int maxHealth;

    public bool IsDead => isDead;
    private bool isDead;

    [field: Header("Hurtboxes")]
    [SerializeField] private bool _gatherHurtboxesOnInit = false;
    [SerializeField] private Hurtbox[] _hurtBoxes;

    [field: Header("Components")]
    public StatusEffectComponent StatusEffectComponent { get; private set; }
    private CombatManager combatManager;
    private Rigidbody rb;

    [field: Header("Systems")]
    public bool IsAbleToAttack { get; private set; } = true;

    public bool IsAttackInProgress { get; private set; }
    private int ongoingAttackID;

    private Coroutine attackCooldown;

    // Debug vars
    private Vector3 lastAttackPos;
    private Vector3 lastAttackDir;
    private float lastAttackRange;
    private float lastSwingAngle;
    private EAttackApplicationType lastApplicationType;

    [field: Header("CombatPacket")]
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

    #region Initialization methods

    protected virtual void Awake()
    {
        health = maxHealth;

        rb = GetComponent<Rigidbody>();
        StatusEffectComponent = GetComponent<StatusEffectComponent>();

        if (_gatherHurtboxesOnInit)
            _hurtBoxes = GetComponentsInChildren<Hurtbox>();

    }

    protected virtual void Start()
    {
        combatManager = CombatManager.Instance;

        foreach (Hurtbox i in _hurtBoxes)
            i.Initialize(this);
    }
    #endregion

    #region Implemented Health Methods

    /// <summary>
    /// Sets an entities health directly
    /// </summary>
    public virtual void SetHealth(int value)
    {
        health = value;
        health = Mathf.Clamp(health, 0, maxHealth);

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

    #region Attack Methods

    /// <summary>
    /// This method should be called by an entity when it begins an attack
    /// It will grab an attack packet from the combatmanager and sign it with this entities data as well as attack data
    /// </summary>
    /// <param name="attack">The data of the attack that will be performed</param>
    public void EntityBeginAttack()
    {
        IsAbleToAttack = false;
        IsAttackInProgress = true;

        // Create a combat packet with this entities data
        combatManager.BeginCombatPacket(this, out int packetID);

        // Store current attacks ID
        ongoingAttackID = packetID;
    }

    /// <summary>
    /// Performs an ongoing attack for the entity using the provided attack data, position, and direction.
    /// </summary>
    /// <param name="entityAnimationManager">The animation manager for the entity.</param>
    /// <param name="attackData">The data for the attack being performed.</param>
    /// <param name="attackDir">The direction of the attack.</param>
    /// <param name="attackPosition">The position where the attack is performed.</param>
    /// <param name="baseDamage">The listed damage of the weapon or attack. Default is zero (indicating no change)</param>
    /// <param name="damageMultiplier">The multiplier to apply to the attack damage. Default is 0 (indicating no change).</param>
    public virtual void EntityPerformOngoingAttack(EntityAnimationManager_Base entityAnimationManager, AttackData attackData,
        Vector3 attackPosition, Vector3 attackDir, int baseDamage = 0, float damageMultiplier = 0, float baseRange = 0)
    {
        // Return if EntityBeginAttack was not called first
        if (!IsAttackInProgress)
            return;

        // Retrieve the combat packet assigned to the ongoing attack
        CombatPacket packet = combatManager.GetCombatPacket(this, ongoingAttackID);

        // Ensure a packet with this instigator and ongoing attack ID exists
        if (packet == null)
            return;

        // Determine damage, range, and multiplier values
        int totalDamage = baseDamage + attackData.AttackDamage;
        float totalRange = baseRange + attackData.AttackRange;
        float? multiplierToSet = damageMultiplier != 0 ? damageMultiplier : null;

        // Add the chosen attack to the packet
        packet.SetPacketDamageVars(damage: totalDamage, knockback: attackData.Knockback, 
            statusEffects: attackData.EffectsApplied.Length > 0 ? attackData.EffectsApplied : null);

        packet.SetPacketMultiplier(multiplierToSet);
        packet.SetPacketAttackerCooldown(attackData.Cooldown);

        // Get the attack data and position locally for debug use
        lastAttackPos = attackPosition;
        lastAttackDir = attackDir;
        lastAttackRange = totalRange;
        lastSwingAngle = attackData.SwingRange;
        lastApplicationType = attackData.ApplicationType;

        if (attackData.IsPhysicalAttack)
        {
            // Set the packets damage type
            packet.SetPacketDamageVars(attackData.DamageType);
            packet.SetPacketScreenFX(attackData.ScreenShakeAmplitude, attackData.ScreenShakeDuration, attackData.ImpactPauseDuration);

            // Set the completer of the attack to the entity animator
            // The packet will complete on animation finish
            packet.SetPacketCompleter(this);
        }

        switch (attackData.ApplicationType)
        {
            case EAttackApplicationType.Positional:

                // Get all health components in a radius from the attack position using range as the size.
                Collider[] colliders = Physics.OverlapSphere(attackPosition, totalRange, attackData.AffectedLayers, QueryTriggerInteraction.Collide);
                foreach (Collider i in colliders)
                {
                    if (i.TryGetComponent(out IDamagable damagable) && damagable.IsDamagable)
                    {
                        packet.AddVictim(damagable, attackPosition);
                    }
                    else if (i.TryGetComponent(out Rigidbody rb))// If the object has a rigidbody but no health, still apply knockback.
                        rb.AddForce((i.transform.position - attackPosition).normalized * packet.PacketKnockback, ForceMode.Impulse);
                }
                break;

            case EAttackApplicationType.Swinging:

                // Calculate the perpendicular direction vectors
                Vector3 right = Vector3.Cross(Vector3.up, attackDir).normalized; // Perpendicular direction to forward
                Vector3 up = Vector3.Cross(attackDir, right).normalized; // Ensure up is perpendicular to both forward and right

                // Number of rays to cast within the arc
                int rayCount = 12;
                float angleIncrement = attackData.SwingRange * 2 / (rayCount - 1);

                // Calculate the rotation increment around the forward axis
                Quaternion clockwiseRotation = Quaternion.AngleAxis(attackData.SwingClockwiseAngle, attackDir);

                // Store hit victems
                HashSet<IDamagable> hitVictims = new HashSet<IDamagable>();

                // Iterate through the rays within the swing arc
                for (int i = 0; i < rayCount; i++)
                {
                    float angle = -attackData.SwingRange + (angleIncrement * i); // Find angle increment in loop

                    // Create a rotation around the up axis for each ray
                    Quaternion rotation = Quaternion.AngleAxis(angle, up); // Rotate around the calculated up vector
                    Vector3 rayDirection = rotation * attackDir; // Find ray direction in relation to the attackDir

                    // Apply the additional clockwise rotation around the forward axis
                    rayDirection = clockwiseRotation * rayDirection;

                    Debug.DrawRay(attackPosition, rayDirection * totalRange, Color.cyan, 1);

                    RaycastHit[] hits = Physics.RaycastAll(attackPosition, rayDirection, totalRange, attackData.AffectedLayers);
                    foreach (RaycastHit h in hits)
                    {
                        if (h.collider.TryGetComponent(out IDamagable damagable) && damagable.IsDamagable)
                        {
                            // Add the victim to the packet if not previously hit
                            if (hitVictims.Add(damagable))
                                packet.AddVictim(damagable, attackPosition);
                        }
                        else if (h.collider.TryGetComponent(out Rigidbody rb))// If the object has a rigidbody but no health, still apply knockback.
                            rb.AddForce((h.collider.transform.position - attackPosition).normalized * packet.PacketKnockback, ForceMode.Impulse);
                    }
                }
                break;

            case EAttackApplicationType.Hitscan:

                RaycastHit hit;

                // Cast a ray and find the first victem
                if(Physics.Raycast(attackPosition, attackDir * totalRange, out hit, totalRange, attackData.AffectedLayers, QueryTriggerInteraction.Collide))
                {
                    if (hit.collider.TryGetComponent(out IDamagable damagable) && damagable.IsDamagable)
                    {
                        packet.AddVictim(damagable, attackPosition);
                    }
                    else if (hit.collider.TryGetComponent(out Rigidbody rb))// If the object has a rigidbody but no health, still apply knockback.
                        rb.AddForce((hit.collider.transform.position - attackPosition).normalized * packet.PacketKnockback, ForceMode.Impulse);
                }

                break;
        }

        // Create a projectile if possible
        // Projectiles will always become the packet completer, even if the attack has melee data.
        if (attackData.ProjectileCreated != null)
        {
            // Create the projectile
            // Set the completer of the attack to the projectile
            attackData.ProjectileCreated.CreateProjectileInstance(packet, attackPosition, attackDir, totalRange, attackData.AffectedLayers, rb.velocity);
            packet.SetPacketDamageVars(damage: totalDamage + attackData.ProjectileCreated.Damage);// Use the base damage, plus the attacks, plus projectiles base damage.
        }
    }

    /// <summary>
    /// Finishes an attack packet and starts cooldown
    /// </summary>
    public void FinishAttack()
    {
        // Retrieve the combat packet assigned to the ongoing attack
        CombatPacket packet = combatManager.GetCombatPacket(this, ongoingAttackID);

        // Start cooldown
        if (packet.IsCooldownSet)
            StartAttackCooldown(packet.AttackerCooldown);
        else IsAbleToAttack = true;

        // Complete the active attack
        CompleteAttack();

        IsAttackInProgress = false;
    }

    /// <summary>
    /// Cancels an active attack while it is in progress
    /// </summary>
    /// <remarks>This should be called by the combat manager when an entity is stunned, killed, or stagered</remarks>
    public void StopCurrentAttack()
    {
        IsAttackInProgress = false;
        ongoingAttackID = 0;
    }
    #endregion

    #region Cooldown

    /// <summary>
    /// Starts a new attack cooldown
    /// </summary>
    /// <param name="duration">How long the cooldown lasts</param>
    public void StartAttackCooldown(float duration)
    {
        if (attackCooldown != null)
            StopCoroutine(attackCooldown);

        attackCooldown = StartCoroutine(AttackCooldownCoroutine(duration));
    }

    private IEnumerator AttackCooldownCoroutine(float duration)
    {
        IsAbleToAttack = false;

        yield return new WaitForSecondsRealtime(duration);

        IsAbleToAttack = true;
    }
    #endregion

    #region Debug Methods
#if DEBUG
    private void OnDrawGizmos()
    {
        if (!IsAttackInProgress) return;

        switch (lastApplicationType)
        {
            case EAttackApplicationType.Positional:

                // Draw a red sphere gizmo at the last attack position
                Gizmos.color = new Color(1, 0, 0, 0.5f);
                Gizmos.DrawSphere(lastAttackPos, lastAttackRange);
                break;

            case EAttackApplicationType.Swinging:

                // The direction of the arc
                Vector3 right = Vector3.Cross(Vector3.up, lastAttackDir).normalized; // Perpendicular direction to forward
                Vector3 up = Vector3.Cross(lastAttackDir, right); // Ensure up is perpendicular to both forward and right

                // Draw arc
                Handles.color = new Color(1, 0, 0, 0.5f);
                Handles.DrawSolidArc(lastAttackPos, up, Quaternion.AngleAxis(-lastSwingAngle, up) * lastAttackDir, lastSwingAngle * 2, lastAttackRange);

                break;

            case EAttackApplicationType.Hitscan:

                // Draw a red ray gizmo at the last attack position in the last attack direction
                Gizmos.color = new Color(1, 0, 0, 0.5f);
                Gizmos.DrawRay(lastAttackPos, lastAttackDir * lastAttackRange);
                break;
        }
    }
#endif
    #endregion

    #region Implemented Attack Completer Methods

    public void CompleteAttack()
    {
        onAttackCompleted?.Invoke();

        ongoingAttackID = 0;
    }
    #endregion
}
