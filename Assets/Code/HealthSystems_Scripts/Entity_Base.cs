using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(StatusEffectComponent), typeof (AudioSource))]
public abstract class Entity_Base : MonoBehaviour, IHealthComponent, IDamagable, ICombatPacketCompleter
{
    [field: Header("Health")]
    public int Health => health;
    protected int health;

    public int MaxHealth => maxHealth;
    [SerializeField] private int maxHealth;

    public bool IsDead => isDead;
    private bool isDead;

    public bool IsDamagable => isDamagable;
    [SerializeField] private bool isDamagable = true;

    [field: Header("Hurtboxes")]
    [SerializeField] private bool gatherHurtboxesOnInit = false;
    [SerializeField] private Hurtbox[] hurtBoxes;

    [field: Header("Components")]
    public AudioSource AudioSource { get; private set; }
    public StatusEffectComponent StatusEffectComponent { get; private set; }
    private Rigidbody rb;

    [field: Header("Damage Reduction")]
    [SerializeField] private int defence;
    public int Defence => defence;

    [SerializeField, Range(0, 1)] private float bludgeoningResistance;
    public float BludgeoningResistance => bludgeoningResistance;

    [SerializeField, Range(0, 1)] private float explosiveResistance;
    public float ExplosiveResistance => explosiveResistance;

    [SerializeField, Range(0, 1)] private float piercingResistance;
    public float PiercingResistance => piercingResistance;

    [SerializeField, Range(0, 1)] private float slashingResistance;
    public float SlashingResistance => slashingResistance;

    [field: Header("Attack System")]
    public bool IsAbleToAttack { get; private set; } = true;
    public bool IsAttackInProgress { get; private set; }

    private int ongoingAttackID;
    private Coroutine attackCooldown;

    public event Action OnAttackCompleted;

    #region Initialization methods

    protected virtual void Awake()
    {
        health = maxHealth;

        AudioSource = GetComponent<AudioSource>();
        rb = GetComponent<Rigidbody>();
        StatusEffectComponent = GetComponent<StatusEffectComponent>();

        if (gatherHurtboxesOnInit)
            hurtBoxes = GetComponentsInChildren<Hurtbox>();
    }

    protected virtual void Start()
    {
        foreach (var hurtbox in hurtBoxes)
            hurtbox.Initialize(this);
    }
    #endregion

    #region Health Management

    public virtual void SetHealth(int value)
    {
        health = Mathf.Clamp(value, 0, maxHealth);
        isDead = health <= 0;
    }

    public virtual void AddHealth(int amount) =>
        SetHealth(health + amount);

    public virtual void RemoveHealth(int amount) =>
        SetHealth(health - amount);

    #endregion

    #region Attack Methods

    /// <summary>
    /// This method should always be called when an entity starts and attack
    /// </summary>
    public void EntityBeginAttack()
    {
        IsAbleToAttack = false;
        IsAttackInProgress = true;

        // Register attack packet
        CombatManager.Instance.BeginCombatPacket(this, out int packetID);
        ongoingAttackID = packetID;
    }

    /// <summary>
    /// This method should always be called after an attack has begun
    /// </summary>
    public virtual void EntityPerformOngoingAttack(EntityAnimationManager_Base entityAnimationManager, AttackData attackData,
        Vector3 attackPos, Vector3 attackDir, int baseDamage = 0, float damageMultiplier = 0, float baseRange = 0)
    {
        if (!IsAttackInProgress) return;

        var packet = CombatManager.Instance.GetCombatPacket(this, ongoingAttackID);
        if (packet == null) return;

        float totalRange = baseRange + attackData.AttackRange;

        ConfigurePacket(packet, attackData, baseDamage, damageMultiplier);

        switch (attackData.ApplicationType)
        {
            case EAttackApplicationType.Positional:
                ApplyPositionalAttack(packet, attackData, attackPos, attackDir, totalRange);
                break;
            case EAttackApplicationType.Swinging:
                ApplySwingingAttack(packet, attackData, attackPos, attackDir, totalRange);
                break;
            case EAttackApplicationType.Hitscan:
                ApplyHitscanAttack(packet, attackData, attackPos, attackDir, totalRange);
                break;
        }

        CreateProjectileIfApplicable(packet, attackData, attackPos, attackDir, totalRange);
    }

    /// <summary>
    /// Configures a packet with basic damage information
    /// </summary>
    private void ConfigurePacket(CombatPacket packet, AttackData attackData, int baseDamage, float damageMultiplier)
    {
        int totalDamage = baseDamage + attackData.AttackDamage;
        packet.SetPacketDamageVars(damage: totalDamage, knockback: attackData.Knockback, statusEffects: attackData.EffectsApplied);
        packet.SetPacketMultiplier(damageMultiplier > 0 ? damageMultiplier : null);
        packet.SetPacketAttackerCooldown(attackData.Cooldown > 0? attackData.Cooldown : null);

        if (attackData.IsPhysicalAttack)
        {
            packet.SetPacketDamageVars(attackData.DamageType);
            packet.SetExecutionSound(attackData.GetRandomImpactSound());
            packet.SetPacketScreenFX(attackData.ScreenShakeAmplitude, attackData.ScreenShakeDuration);
            packet.SetPacketCompleter(this);
        }
    }

    private void ApplyPositionalAttack(CombatPacket packet, AttackData attackData, Vector3 attackDir, Vector3 attackPos, float range)
    {
        var colliders = Physics.OverlapSphere(attackPos, range, attackData.AffectedLayers);
        foreach (var collider in colliders)
            TryApplyDamageOrKnockback(packet, collider, attackPos, attackDir);
    }

    private void ApplySwingingAttack(CombatPacket packet, AttackData attackData, Vector3 attackPos, Vector3 attackDir, float range)
    {
        HashSet<Collider> hitColliders = new HashSet<Collider>();
        foreach (var ray in GenerateSwingArcs(attackData, attackDir))
        {
            // Cast a ray in the current direction
            RaycastHit[] hits = Physics.RaycastAll(attackPos, ray, range, attackData.AffectedLayers);

            foreach (var hit in hits)
            {
                // Only process the collider if it hasn't been processed yet
                if (hitColliders.Add(hit.collider))
                {
                    TryApplyDamageOrKnockback(packet, hit.collider, hit.point, attackDir);
                }
            }
        }
    }

    private void ApplyHitscanAttack(CombatPacket packet, AttackData attackData, Vector3 attackPos, Vector3 attackDir, float range)
    {
        if (Physics.Raycast(attackPos, attackDir, out var hit, range, attackData.AffectedLayers))
            TryApplyDamageOrKnockback(packet, hit.collider, hit.point, attackDir);
    }

    private void CreateProjectileIfApplicable(CombatPacket packet, AttackData attackData, Vector3 attackPos, Vector3 attackDir, float range)
    {
        if (attackData.ProjectileCreated == null) return;

        attackData.ProjectileCreated.CreateProjectileInstance(packet, attackPos, attackDir, range, attackData.AffectedLayers, rb.velocity);
        packet.SetPacketDamageVars(damage: packet.PacketDamage + attackData.ProjectileCreated.Damage);
    }

    private void TryApplyDamageOrKnockback(CombatPacket packet, Collider target, Vector3 attackPos, Vector3 attackDir)
    {
        if (target.TryGetComponent<IDamagable>(out var damagable) && damagable.IsDamagable)
            packet.AddVictim(damagable, attackPos, attackDir);
        else if (target.TryGetComponent<Rigidbody>(out var rb))
            rb.AddForce((target.transform.position - attackPos).normalized * packet.PacketKnockback, ForceMode.Impulse);
    }

    private IEnumerable<Vector3> GenerateSwingArcs(AttackData attackData, Vector3 attackDir)
    {
        int rayCount = 12;
        float angleStep = attackData.SwingRange * 2 / (rayCount - 1);

        for (int i = 0; i < rayCount; i++)
        {
            float angle = -attackData.SwingRange + angleStep * i;
            yield return Quaternion.AngleAxis(angle, Vector3.up) * attackDir;
        }
    }

    /// <summary>
    /// This method should be called when an entity completes an attack
    /// </summary>
    public void FinishAttack()
    {
        var packet = CombatManager.Instance.GetCombatPacket(this, ongoingAttackID);

        if (packet?.IsCooldownSet == true)
            StartAttackCooldown(packet.AttackerCooldown);
        else
            IsAbleToAttack = true;

        CompleteAttack();
        IsAttackInProgress = false;
    }

    /// <summary>
    /// Stops an entities attack while it is in progress
    /// </summary>
    public void StopCurrentAttack()
    {
        IsAttackInProgress = false;
        ongoingAttackID = 0;
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
     RemoveHealth(amount);
    #endregion

    #region Cooldown

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

    private Vector3 lastAttackPos;
    private Vector3 lastAttackDir;
    private float lastAttackRange;
    private float lastSwingAngle;
    private EAttackApplicationType lastApplicationType;

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
		OnAttackCompleted?.Invoke();

		ongoingAttackID = 0;
	}
	#endregion
}
