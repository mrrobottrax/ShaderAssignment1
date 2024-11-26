using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class CombatPacket
{
    [field: Header("Packet Indentifiers")]
    public int PacketID { get; private set; }
    public Entity_Base Instigator { get; private set; }// The entity which initiated this attack

    private ICombatPacketCompleter packetCompleter;

    [field: Header("System")]
    public event Action<CombatPacket, IDamagable, Vector3, Vector3> OnPacketAddVictem;

    [field: Header("Packet Variables")]
    public List<IDamagable> Victims { get; private set; } = new List<IDamagable>();
    public EDamageType PacketDamageType { get; private set; }
    public int PacketDamage { get; private set; }
    public float PacketKnockback { get; private set; }
    public StatusEffectData_Base[] PacketStatusEffects { get; private set; } = Array.Empty<StatusEffectData_Base>();
    public AudioClip ExecutionSound { get; private set; }

    public bool IsCooldownSet { get; private set; }
    public float AttackerCooldown { get; private set; }

    public bool IsMultiplierSet { get; private set; }
    public float PacketMultiplier { get; private set; }

    public bool IsPacketScreenFXSet { get; private set; }
    public float ScreenShakeAmplitude { get; private set; }
    public float ScreenShakeDuration { get; private set; }
    public float ImpactPauseDuration { get; private set; }

    #region Packet Management Methods

    /// <summary>
    /// This method should be called whenever a new combat packet is released
    /// </summary>
    public void Initialize(int packetID, Entity_Base instigator)
    {
        Instigator = instigator;
        PacketID = packetID;
    }

    /// <summary>
    /// This method resets all of this packets information
    /// </summary>
    public void ResetPacket()
    {
        Instigator = null;
        PacketID = 0;
        Victims.Clear();

        PacketDamageType = EDamageType.Bludgeoning;
        PacketDamage = 0;
        PacketKnockback = 0;
        PacketStatusEffects = Array.Empty<StatusEffectData_Base>();

        IsCooldownSet = false;
        AttackerCooldown = 0;

        IsMultiplierSet = false;
        PacketMultiplier = 0;

        IsPacketScreenFXSet = false;
        ScreenShakeAmplitude = 0;
        ScreenShakeDuration = 0;
        ImpactPauseDuration = 0;

        UnsubscribeFromCompleter();
    }

    #endregion

    #region Packet Subscription Methods

    /// <summary>
    /// Removes this packets completer
    /// </summary>
    private void UnsubscribeFromCompleter()
    {
        if (packetCompleter == null) return;
        packetCompleter.OnAttackCompleted -= CompletePacket;
        packetCompleter = null;
    }

    /// <summary>
    /// Assigns a completer for this packet, ensuring only one is active at a time.
    /// </summary>
    public void SetPacketCompleter(ICombatPacketCompleter newCompleter)
    {
        UnsubscribeFromCompleter();

        Debug.Log(newCompleter + " is the new completer");

        packetCompleter = newCompleter;
        packetCompleter.OnAttackCompleted += CompletePacket;
    }

    /// <summary>
    /// Called when the associated completer signals the packet is complete.
    /// </summary>
    private void CompletePacket() =>
        CombatManager.Instance.CombatPacketFinished(this);

    #endregion

    #region Packet Variable Setting Methods

    /// <summary>
    /// Configures damage related properties of the packet.
    /// </summary>
    public void SetPacketDamageVars(EDamageType damageType = EDamageType.Bludgeoning, int damage = -1, float knockback = -1f, StatusEffectData_Base[] statusEffects = null)
    {
        PacketDamageType = damageType;

        if (damage > 0) PacketDamage = damage;
        if (knockback > 0) PacketKnockback = knockback;

        if (statusEffects != null && statusEffects.Length > 0)
            PacketStatusEffects = PacketStatusEffects.Concat(statusEffects).ToArray();
    }

    /// <summary>
    /// Sets the sound that playes when a victem takes damage
    /// </summary>
    public void SetExecutionSound(AudioClip clip) =>
        ExecutionSound = clip;

    /// <summary>
    /// Sets an optional cooldown for the attacker.
    /// </summary>
    public void SetPacketAttackerCooldown(float? attackerCooldown)
    {
        if (!attackerCooldown.HasValue) return;

        AttackerCooldown = attackerCooldown.Value;
        IsCooldownSet = true;
    }

    /// <summary>
    /// Sets the multiplier for the packet's damage output.
    /// </summary>
    public void SetPacketMultiplier(float? multiplier)
    {
        if (!multiplier.HasValue) return;

        PacketMultiplier = multiplier.Value;
        IsMultiplierSet = true;
    }

    /// <summary>
    /// Configures screen effects caused by this packet.
    /// </summary>
    public void SetPacketScreenFX(float screenShakeAmplitude, float screenShakeDuration)
    {
        ScreenShakeAmplitude = screenShakeAmplitude;
        ScreenShakeDuration = screenShakeDuration;
        IsPacketScreenFXSet = true;
    }

    #endregion

    #region Victim Management

    /// <summary>
    /// Adds a victim to the packet and triggers the associated event.
    /// </summary>
    public void AddVictim(IDamagable addedVictim, Vector3 damagePos, Vector3 damageDir)
    {
        Victims.Add(addedVictim);
        OnPacketAddVictem?.Invoke(this, addedVictim, damagePos, damageDir);
    }
    #endregion
}