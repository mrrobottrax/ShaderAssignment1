using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class UseableItem : Item
{
	[field: Header("ViewModel")]
	[field: SerializeField] public Vector3 HoldOffset { get; private set; }
    [field: SerializeField] public Vector3 HoldRotation { get; private set; }

    [field: Header("SFX")]
    [field: SerializeField] protected AudioSource audioSource;
	[SerializeField] private AudioClip[] onFireSounds;
    [SerializeField] private AudioClip[] onUseSounds;

    [field: Header("System")]
    protected PlayerViewmodelManager playerViewmodelManager;

    protected override void Awake()
    {
		base.Awake();

		audioSource = GetComponent<AudioSource>();
    }

    #region Item Functionality

    public override void OnAddToInventory(PlayerInventory inventory, InventorySlot slot)
	{
		base.OnAddToInventory(inventory, slot);
		playerViewmodelManager = inventory.GetComponentInChildren<PlayerViewmodelManager>();
	}

	public override void OnDrop()
	{
		base.OnDrop();
		playerViewmodelManager = null;
	}

	public virtual void OnFire1Pressed()
	{
		if (playerViewmodelManager.HasParameter("IsHoldingFire1"))
			playerViewmodelManager.Animator.SetBool("IsHoldingFire1", true);

		if (playerViewmodelManager.HasParameter("Fire1"))
			playerViewmodelManager.Animator.SetTrigger("Fire1");

        if (onFireSounds.Length > 0)// Fire sounds
            audioSource?.PlayOneShot(onFireSounds[Random.Range(0, onFireSounds.Length)]);
    }
	public virtual void OnFire1Released()
	{
		if (playerViewmodelManager.HasParameter("IsHoldingFire1"))
			playerViewmodelManager.Animator.SetBool("IsHoldingFire1", false);
	}

	public virtual void OnFire2Pressed()
	{
		if (playerViewmodelManager.HasParameter("IsHoldingFire2"))
			playerViewmodelManager.Animator.SetBool("IsHoldingFire2", true);

		if (playerViewmodelManager.HasParameter("Fire2"))
			playerViewmodelManager.Animator.SetTrigger("Fire2");
	}
	public virtual void OnFire2Released()
	{
		if (playerViewmodelManager.HasParameter("IsHoldingFire2"))
			playerViewmodelManager.Animator.SetBool("IsHoldingFire2", false);
	}

	#endregion

	#region ViewModel Functionality

	/// <summary>
	/// This method executes a view model's function based on the action title passed in.
	/// </summary>
	public virtual void TryItemFunction(PlayerStats player, PlayerViewmodelManager viewModelManager, Vector3 functionPos, string actionTitle, AttackData attack = null) 
	{
        if (onUseSounds.Length > 0)// Use sounds
            audioSource?.PlayOneShot(onUseSounds[Random.Range(0, onUseSounds.Length)]);
    }
	#endregion
}
