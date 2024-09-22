using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// The combat packet class is a context class meant to store data
/// </summary>
[System.Serializable]
public class CombatPacket
{
    [field: Header("Packet Indentifiers")]
    public int PacketID { get; private set; }

    // The entity which initiated this attack
    public Entity_Base Instigator { get; private set; }

    // The holder of the event this packet is listening for to be "complete."
    private ICombatPacketCompleter packetCompleter;

    [field: Header("System")]
    public event Action<CombatPacket, IDamagable, Vector3> OnPacketAddVictem;

    [field: Header("Packet Variables")]
    public List<IDamagable> Victims { get; private set; } = new List<IDamagable>();

    public EDamageType PacketDamageType { get; private set; }
    public int PacketDamage { get; private set; }
    public float PacketKnockback { get; private set; }
    public StatusEffectData_Base[] PacketStatusEffects { get; private set; } = new StatusEffectData_Base[0];

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

        // Reset all variables
        PacketID = 0;

        Victims.Clear();

        PacketDamageType = EDamageType.Bludgeoning;
        PacketDamage = 0;
        PacketKnockback = 0;
        PacketStatusEffects = new StatusEffectData_Base[0];

        IsCooldownSet = false;
        AttackerCooldown = 0;

        IsMultiplierSet = false;
        PacketMultiplier = 0;

        IsPacketScreenFXSet = false;
        ScreenShakeAmplitude = 0;
        ScreenShakeDuration = 0;
        ImpactPauseDuration = 0;

        // Unsubscribe to the completers complete event.
        if (packetCompleter != null)
        {
            packetCompleter.OnAttackCompleted -= CompletePacket;
            packetCompleter = null;
        }
    }

    #endregion

    #region Packet Completion Methods

    /// <summary>
    /// This method will establish what event this packet is waiting for before it is deemed complete.
    /// </summary>
    /// <param name="newCompleter">The event holder</param>
    public void SetPacketCompleter(ICombatPacketCompleter newCompleter)
    {
        // Unsubscribe from the previous packet completer if one is present
        if (packetCompleter != null)
            packetCompleter.OnAttackCompleted -= CompletePacket;

        Debug.Log(newCompleter + " is the new completer");

        // Set the new packet completer
        packetCompleter = newCompleter;

        // Subscribe to the completers complete event.
        newCompleter.OnAttackCompleted += CompletePacket;
    }

    /// <summary>
    /// This method is called when an ICombatPacketCompleters completion event is called.
    /// </summary>
    private void CompletePacket()
    {
        Debug.Log("Completer event recived");

        // Finish this packet
        CombatManager.Instance.CombatPacketFinished(this);
    }
    #endregion

    #region Packet Variable Setting Methods

    /// <summary>
    /// Sets the packet values with optional parameters.
    /// </summary>
    /// /// <param name="damageType">Packets damage type.</param>
    /// <param name="damage">Optional damage value to set. Default is -1 (indicating no change).</param>
    /// <param name="knockback">Optional knockback value to set. Default is -1f (indicating no change).</param>
    /// <param name="statusEffects">Optional status effects array to set. Default is null (indicating no change).</param>
    public void SetPacketDamageVars(EDamageType damageType = EDamageType.Bludgeoning, int damage = -1, float knockback = -1f, StatusEffectData_Base[] statusEffects = null)
    {
        PacketDamageType = damageType;

        if (damage != -1)
        {
            PacketDamage = damage;
        }

        if (knockback != -1f)
        {
            PacketKnockback = knockback;
        }

        if (statusEffects != null)
        {
            // Check if status effects are already present on this packet
            if (PacketStatusEffects.Length <= 0)
                PacketStatusEffects = statusEffects;
            else PacketStatusEffects.Concat(statusEffects);// Add any extra status effects to the existing array
        }
    }

    /// <summary>
    /// Sets the attackers cooldown applied when this packet completes
    /// </summary>
    /// <param name="attackerCooldown">The duration of the attackers cooldown</param>
    public void SetPacketAttackerCooldown(float? attackerCooldown = null)
    {
        if (attackerCooldown.HasValue)
        {
            AttackerCooldown = attackerCooldown.Value;
            IsCooldownSet = true;
        }
    }

    /// <summary>
    /// Sets the damage multiplier of this packet
    /// </summary>
    /// <param name="multiplier">The value of the multiplier</param>
    public void SetPacketMultiplier(float? multiplier = null)
    {
        if (multiplier.HasValue)
        {
            PacketMultiplier = multiplier.Value;
            IsMultiplierSet = true;
        }
    }

    /// <summary>
    /// Sets the values for the screen effects caused when this packet is applied to a victem
    /// </summary>
    /// <param name="screenShakeAmplitude">How strong the screen shake generated on impact is</param>
    /// <param name="screenShakeDuration">How long the screen shake generated on impact lasts</param>
    /// <param name="impactPauseDuration">How long the game will pause for on impact</param>
    public void SetPacketScreenFX(float screenShakeAmplitude, float screenShakeDuration, float impactPauseDuration)
    {
        IsPacketScreenFXSet = true;

        ScreenShakeAmplitude = screenShakeAmplitude;
        ScreenShakeDuration = screenShakeDuration;
        ImpactPauseDuration = impactPauseDuration;
    }

    /// <summary>
    /// Adds a victem to the list of this packet's victems
    /// </summary>
    /// <param name="addedVictim">The health component of the added victem</param>
    public void AddVictim(IDamagable addedVictim, Vector3 damagePos)
    {
        Victims.Add(addedVictim);

        OnPacketAddVictem.Invoke(this, addedVictim, damagePos);
    }

    #endregion
}