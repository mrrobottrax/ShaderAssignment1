using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CombatManager : MonoBehaviour
{
    public static CombatManager Instance { get; private set; }

    [field: Header("Pool")]
    private Queue<CombatPacket> combatPacketPool = new Queue<CombatPacket>();
    private int combatPacketPoolSize = 25;

    [field: Header("System")]
    private Dictionary<Entity_Base, List<CombatPacket>> activePackets = new Dictionary<Entity_Base, List<CombatPacket>>();

    [field: Header("Events")]
    public event Action<Entity_Base, CombatPacket> CombatPacketComplete;

    #region Initialization Methods

    [RuntimeInitializeOnLoadMethod]
    static void Initialize() =>
        new GameObject("Combat Manager").AddComponent<CombatManager>();

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        Instance = this;

        for (int i = 0; i < combatPacketPoolSize; i++)
            combatPacketPool.Enqueue(new CombatPacket());
    }
    #endregion

    #region Combat Packet Methods

    /// <summary>
    /// This method should be called by an entity when it begins an attack
    /// </summary>
    /// <param name="instigator">The instigator of the attack</param>
    /// <param name="packetID">The ID of the packet returned</param>
    /// <param name="fetchedPacket">The returned combat packet</param>
    public CombatPacket BeginCombatPacket(Entity_Base instigator, out int packetID)
	{
		// Dequeue packet from pool
		CombatPacket packet = ReleaseCombatPacket(instigator);
		packetID = GeneratePacketID(instigator);
		packet.Initialize(packetID, instigator);

		Debug.Log("Packet Dequeued");

		return packet;
	}

	public void CombatPacketFinished(CombatPacket combatPacket)
	{
		Debug.Log("Packet Finished");

		// Invoke combat packet complete event for other listeners
		CombatPacketComplete?.Invoke(combatPacket.Instigator, combatPacket);

		ReturnCombatPacket(combatPacket);
	}

	/// <summary>
	/// This method releases all of an entitys held packets
	/// </summary>
	public void ResetInstigatorsList(Entity_Base key)
	{
		key.StopCurrentAttack();

		if (!activePackets.ContainsKey(key))
			return;

		// Return all of the instigators keys to the pool
		foreach (CombatPacket i in activePackets[key])
			ReturnCombatPacket(i);

		activePackets.Remove(key);
	}
	#endregion

	#region Combat Packet Helper Methods

	/// <summary>
	/// Gets an attack packet from the active pool, based on the attacker and an ID.
	/// </summary>
	public CombatPacket GetCombatPacket(Entity_Base key, int packetId) =>
        activePackets.TryGetValue(key, out var packets)
            ? packets.FirstOrDefault(p => p.PacketID == packetId)
            : null;

    /// <summary>
    /// Returns all active packets associated with an entity
    /// </summary>
    public CombatPacket[] GetKeysPackets(Entity_Base entitiy) =>
        activePackets.TryGetValue(entitiy, out var packets)
            ? packets.ToArray()
            : null;

    /// <summary>
    /// Generates a unique packet ID
    /// </summary>
    private int GeneratePacketID(Entity_Base key)
	{
        int newPacketID = 1;
        if (!activePackets.ContainsKey(key)) return newPacketID;

        var ids = activePackets[key].Select(p => p.PacketID).ToHashSet();

        while (ids.Contains(newPacketID)) newPacketID++;
        return newPacketID;
    }

	/// <summary>
	/// This method takes a combat packet from the pool and puts it in the "in progress" queue.
	/// </summary>
	private CombatPacket ReleaseCombatPacket(Entity_Base key)
	{
		// Check if the key exists, and if not, add it to the dictionary with an empty list
		if (!activePackets.ContainsKey(key))
            activePackets[key] = new List<CombatPacket>();

        // Return a packet from the pool (or create an extra)
        CombatPacket releasedPacket = combatPacketPool.Count > 0 ? combatPacketPool.Dequeue() : new CombatPacket();
		activePackets[key].Add(releasedPacket);
		releasedPacket.OnPacketAddVictem += EvaluatePacketOnVictem;
        return releasedPacket;
	}

	/// <summary>
	/// This method removes a combat packet from the active pool and returns it to the object pool.
	/// </summary>
	private void ReturnCombatPacket(CombatPacket combatPacket)
	{
		// Remove the packet from the active packet pool
		activePackets.Remove(combatPacket.Instigator);

        // Return the packet to the packet pool
        combatPacket.OnPacketAddVictem -= EvaluatePacketOnVictem;
        combatPacket.ResetPacket();
		combatPacketPool.Enqueue(combatPacket);

		Debug.Log("Packet pool size = " + combatPacketPool.Count);

    }
	#endregion

	#region Packet Application Methods

	/// <summary>
	/// Evaluates and applys a combat packet's variables on a victem
	/// </summary>
	private void EvaluatePacketOnVictem(CombatPacket packet, IDamagable victim, Vector3 damagePos, Vector3 damageDir)
	{
        GameObject victimObj = (victim as Component)?.gameObject;
        if (victimObj == null) return;

        int rawDamage = packet.IsMultiplierSet
            ? Mathf.FloorToInt(packet.PacketDamage * packet.PacketMultiplier)
            : packet.PacketDamage;

        int finalDamage = victim is IDamagable damgable// Apply for defense to the raw damage 
            ? Mathf.FloorToInt((rawDamage - damgable.Defence) * damgable.CalculateDamageResist(packet.PacketDamageType))
            : rawDamage;

        if (packet.ExecutionSound != null)
            victim.AudioSource.PlayOneShot(packet.ExecutionSound);

        if (victimObj.TryGetComponent(out StatusEffectComponent status))
            status.AddStatusEffect(packet.PacketStatusEffects);

        if (victimObj.TryGetComponent(out Rigidbody rb))// Knockback
            rb.AddForce((victimObj.transform.position - damagePos).normalized * packet.PacketKnockback, ForceMode.Impulse);

        victim.TakeDamage(finalDamage, damagePos, damageDir);
    }
    #endregion
}