using Cinemachine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CombatManager : MonoBehaviour
{
	[Header("Singleton")]
	private static CombatManager instance;
	public static CombatManager Instance
	{
		get
		{
			if (!instance)
				instance = GameManager.Instance.GetComponent<CombatManager>();

			return instance;
		}
	}

	[field: Header("Components")]
    [SerializeField] private CinemachineImpulseSource _cinemachineImpulseSource;
    private GameManager gameManager;
	private HUDManager hudManager;

    [field: Header("Pool")]
	// Combat Packets
	private Queue<CombatPacket> combatPacketPool = new Queue<CombatPacket>();
	private int combatPacketPoolSize = 25;

	[field: Header("System")]
	private Dictionary<Entity_Base, List<CombatPacket>> activePackets = new Dictionary<Entity_Base, List<CombatPacket>>();

	private Coroutine impactPause;

    [field: Header("Events")]
    public event Action<Entity_Base, CombatPacket> CombatPacketComplete;

	#region Initialization Methods
	private void Awake()
	{
		instance = this;

		// Initialize combat packets
		for (int i = 0; i < combatPacketPoolSize; i++)
		{
			CombatPacket packet = new CombatPacket();
			combatPacketPool.Enqueue(packet);
		}
	}

	private void Start()
	{
		gameManager = GetComponent<GameManager>();
		hudManager = UIManager.Instance.HUDManager;
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

		// Generate a unique packet ID based on the instigators pool
		packetID = GeneratePacketID(instigator);

		// Init the packet
		packet.Initialize(packetID, instigator);

		Debug.Log("Packet Dequeued");

		// Return the packet that was retrieved
		return packet;
	}

	/// <summary>
	/// This method should be called when a combat packet is complete by its "completer"
	/// </summary>
	/// <param name="combatPacket"></param>
	public void CombatPacketFinished(CombatPacket combatPacket)
	{
		Debug.Log("Packet Finished");

		// Invoke combat packet complete event for other listeners
		CombatPacketComplete?.Invoke(combatPacket.Instigator, combatPacket);

		// Return the combat packet
		ReturnCombatPacket(combatPacket);
	}

	/// <summary>
	/// This method releases all of an entitys held packets
	/// </summary>
	public void ResetInstigatorsList(Entity_Base key)
	{
		key.StopCurrentAttack();

		// Ensure key is present in the dictionary
		if (!activePackets.ContainsKey(key))
			return;

		// Return all of the instigators keys to the pool
		foreach (CombatPacket i in activePackets[key])
			ReturnCombatPacket(i);

		// Remove the instigator from the dictioary
		activePackets.Remove(key);
	}
	#endregion

	#region Combat Packet Helper Methods

	/// <summary>
	/// Gets an attack packet from the active pool, based on the attacker and an ID.
	/// </summary>
	public CombatPacket GetCombatPacket(Entity_Base key, int packetId)
	{
		// Check if attacker is in the system
		if (activePackets.ContainsKey(key))
		{
			// Cahce all of their packets
			List<CombatPacket> holdersPackets = activePackets[key];

			// Find the CombatPacket with the specified PacketID
			CombatPacket packet = holdersPackets.FirstOrDefault(p => p.PacketID == packetId);

			return packet;
		}
		else
		{
			// No key was found
			return null;
		}
	}

	/// <summary>
	/// Gets all of a attackers packets from the active packets pool
	/// </summary>
	public CombatPacket[] GetKeysPackets(Entity_Base attacker)
	{
		// Check if attacker is in the system
		if (activePackets.ContainsKey(attacker))
		{
			// Cahce all of their packets
			List<CombatPacket> holdersPackets = activePackets[attacker];

			return holdersPackets.ToArray();
		}
		else
		{
			// No key was found
			return null;
		}
	}

	/// <summary>
	/// Generates a unique packet ID
	/// </summary>
	private int GeneratePacketID(Entity_Base key)
	{
		System.Random random = new System.Random();
		int newPacketID;

		// Generate a random packet ID and check if it already exists in the list
		do
		{
			newPacketID = random.Next();
		} while (activePackets.ContainsKey(key) && activePackets[key].Any(packet => packet.PacketID == newPacketID));

		return newPacketID;
	}

	/// <summary>
	/// This method takes a combat packet from the pool and puts it in the "in progress" queue.
	/// </summary>
	private CombatPacket ReleaseCombatPacket(Entity_Base key)
	{
		// Check if the key exists, and if not, add it to the dictionary with an empty list
		if (!activePackets.ContainsKey(key))
		{
			activePackets[key] = new List<CombatPacket>();
		}

		// Return a packet from the pool if it has any left, if not, create a new packet.
		CombatPacket releasedPacket = combatPacketPool.Count > 0 ? combatPacketPool.Dequeue() : new CombatPacket();

		activePackets[key].Add(releasedPacket);

		// Subscribe to variable changes from the released packet
		releasedPacket.OnPacketAddVictem += EvaluatePacketOnVictem;

		// return the released packet
        return releasedPacket;
	}

	/// <summary>
	/// This method removes a combat packet from the active pool and returns it to the object pool.
	/// </summary>
	private void ReturnCombatPacket(CombatPacket combatPacket)
	{
		// Remove the packet from the active packet pool
		activePackets.Remove(combatPacket.Instigator);

		combatPacket.OnPacketAddVictem -= EvaluatePacketOnVictem;

        combatPacket.ResetPacket();

		// Return the packet to the packet pool
		combatPacketPool.Enqueue(combatPacket);

		Debug.Log("Packet pool size = " + combatPacketPool.Count);

    }
	#endregion

	#region Packet Application Methods
	/// <summary>
	/// Evaluates and applys a combat packet's variables on a victem
	/// </summary>
	/// <param name="packet">The packet containing the data</param>
	/// <param name="victem">The victem the packet data will be applied to</param>
	private void EvaluatePacketOnVictem(CombatPacket packet, IDamagable victem, Vector3 damagePos)
	{
		// Check if addedVictim has a GameObject
		GameObject victimGameObject = (victem as Component)?.gameObject;

		// Determine if the damage can be multiplied
		int rawDamage = packet.IsMultiplierSet ? Mathf.FloorToInt(packet.PacketDamage * packet.PacketMultiplier) : packet.PacketDamage;
		int finalDamage = rawDamage;

		if (victimGameObject != null)
		{
			// Apply any status effects to the victem if possible
			if (victimGameObject.TryGetComponent(out StatusEffectComponent statusEffectComponent))
				statusEffectComponent.AddStatusEffect(packet.PacketStatusEffects);

            // Check if the victem is a damagable
            if (victem is IDamagable damagable)
			{
				// Use damage formula to determine final damage
				finalDamage = Mathf.FloorToInt((rawDamage - damagable.Defence) * damagable.CalculateDamageResist(packet.PacketDamageType));
			}

			// Try knockback
            if (victimGameObject.TryGetComponent(out Rigidbody victemRB))
                victemRB.AddForce((victimGameObject.transform.position - damagePos).normalized * packet.PacketKnockback, ForceMode.Impulse);

            // Apply damage result to victem
            victem.TakeDamage(finalDamage);
		}

		// Screen FX
		if (packet.IsPacketScreenFXSet)
		{
			if (packet.Instigator == GameManager.Instance.GetPlayer())
			{
				// Create screen shake
				// The player should be the only one calling screen shake from the combat manager.
				// Other entities should have local screen shake impulse generators.
				ShakeScreen(packet.ScreenShakeAmplitude, packet.ScreenShakeDuration);

				// Create an impact pause
				StartImpactPause(packet.ImpactPauseDuration);
			}
			/* When enemys are added they should derive from a base enemy class that implements Entity_Base. This class should require a screen shake generator
            else if (packet.Instigator is Enemy enemy)
            {

            }
            */
		}
	}
    #endregion

    #region Screen FX

    /// <summary>
    /// Uses a cinemachine impulse to shake the whole screen without location data
    /// </summary>
    /// <param name="ScreenShakeAmplitude">How strong the shake is</param>
    /// <param name="duration">How long the shake lasts</param>
    public void ShakeScreen(float ScreenShakeAmplitude, float duration)
    {
		_cinemachineImpulseSource.m_ImpulseDefinition.m_ImpulseDuration = duration;
        _cinemachineImpulseSource.GenerateImpulseWithForce(ScreenShakeAmplitude);
    }

    /// <summary>
	/// Pauses the game for a duration before returning to normal
	/// </summary>
	/// <param name="duration"></param>
	/// <returns>The impact pause coroutine</returns>
    private IEnumerator ImpactPause(float duration)
    {
        Time.timeScale = 0f;

        float pauseEndTime = Time.realtimeSinceStartup + duration;

        while (Time.realtimeSinceStartup < pauseEndTime)
        {
            yield return 0;
        }

        Time.timeScale = 1;
    }

	/// <summary>
	/// Begins a new impact pause coroutine (cancles the previous one if present)
	/// </summary>
	/// <param name="pauseDuration"></param>
    public void StartImpactPause(float pauseDuration)
    {
		// Stop the current impact pause
		if (impactPause != null)
			StopCoroutine(impactPause);

        // Start a new impact pause
        if (pauseDuration > 0)
            impactPause = StartCoroutine(ImpactPause(pauseDuration));
    }
    #endregion
}