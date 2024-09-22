using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemOptionsDisplay : MenuDisplayBase
{
	[field: Header("Components")]
    [SerializeField] private PlayerHealth _playerHealth;
    [SerializeField] private ItemQuantityDisplay _itemQuantityDisplay;
	private PlayerInventoryComponent playerInventoryComponent;

	[field: Header("Primary Button")]
	[SerializeField] private GameObject _primaryButtonHolder;



	[field: Header("Player Inventory Buttons")]
	[SerializeField] private Button _playerInventoryPrimaryFunction_Button;
	[SerializeField] private TextMeshProUGUI _playerInventoryPrimaryFunction_Text;

	private const string aidItemText = "Use";
	private const string equipText = "Equip";
	private const string unequipText = "Unequip";

	[Space(5)]

	[SerializeField] private Button _favourite_Button;
	[SerializeField] private TextMeshProUGUI _favourite_Text;

	private const string favouriteText = "Favourite";
	private const string unfavouriteText = "Unfavourite";



	[field: Header("Container Primary Button")]
	[SerializeField] private Button _containerPrimaryFunction_Button;
	[SerializeField] private TextMeshProUGUI _containerPrimaryFunction_Text;

	private const string storeText = "Store";
	private const string takeText = "Take";



	[field: Header("Vendor Primary Button")]
	[SerializeField] private Button _vendorPrimaryFunction_Button;
	[SerializeField] private TextMeshProUGUI _vendorPrimaryFunction_Text;

	private const string sellText = "Sell";
	private const string buyText = "Buy";



	[field: Header("Buttons")]
	[SerializeField] private Button _trash_Button;
	[SerializeField] private Button _back_Button;

	[Header("System")]

	private InventorySlotDisplay currentSlotDisplay;
	private InventoryDisplay currentInventoryDisplay;
	private EInventoryType currentDisplayType;
	public enum EInventoryType
	{
		PlayerInventory,
		Container,
		Vendor
	}

    #region Input Methods
    public override void SetControlsSubscription(bool isSubscribing)
    {

    }

    public override void Subscribe()
    {
        
    }

    public override void Unsubscribe()
    {
       
    }
    #endregion

    #region Menu Methods

	protected override void OnEnableDisplay()
	{
		base.OnEnableDisplay();

		// Enable the option displays controls
		SetControlsSubscription(true);
	}

	protected override void OnDisableDisplay()
	{
		base.OnDisableDisplay();

		// Disable the option displays controls
		SetControlsSubscription(false);
	}
	#endregion

	/// <summary>
	/// This method should be called when an slotDisplay is pressed by the player.
	/// This method will determine which buttons will be used for the interaction.
	/// </summary>
	public void SetItem(InventorySlotDisplay slotDisplay, InventoryDisplay inventoryDisplay)
	{
		SetDisplayActive(true);

		// Cache a pointer to the players inventory component
		if (playerInventoryComponent == null)
			playerInventoryComponent = _playerHealth.GetPlayerInventory();

		// Cache slot and display info
		currentSlotDisplay = slotDisplay;
		currentInventoryDisplay = inventoryDisplay;

		// Create pointer to slots item
		Item_Base item = slotDisplay.AssignedSlot.GetSlotsItem();

		// Determine display type using pattern matching
		currentDisplayType = inventoryDisplay switch
		{
			PlayerInventoryDisplay => EInventoryType.PlayerInventory,
			ContainerInventoryDisplay => EInventoryType.Container,
			VendorInventoryDisplay => EInventoryType.Vendor,
			_ => throw new ArgumentException("Unsupported display type")
		};

        // Enable primary button based on inventory type
        _playerInventoryPrimaryFunction_Button.gameObject.SetActive(currentDisplayType == EInventoryType.PlayerInventory);
		_containerPrimaryFunction_Button.gameObject.SetActive(currentDisplayType == EInventoryType.Container);
		_vendorPrimaryFunction_Button.gameObject.SetActive(currentDisplayType == EInventoryType.Vendor);

		// Check which buttons should be enabled and what their text should updated too
		bool isPInventoryPrimaryAvailable = false;
		bool isContainerPrimaryAvailable = false;
		bool isVendorPrimaryAvailable = false;
		bool isFavouriteToggleAvailable = false;
		bool isTrashAvailable = false;

		switch (currentDisplayType)
		{
			case EInventoryType.PlayerInventory:

				if (item is Aid_Item) // Set the text for aid items
				{
					// Set the player inventory primary button as avaiable
					isPInventoryPrimaryAvailable = true;

					_playerInventoryPrimaryFunction_Text.text = aidItemText;
				}
				else if (item is IEquippableItem equipableItem)
				{
					// Set the player inventory primary button as avaiable
					isPInventoryPrimaryAvailable = true;

					// Set the equip text
					_playerInventoryPrimaryFunction_Text.text = !equipableItem.IsEquipped ? equipText : unequipText;
				}

				// Check for favouritable items
				if (item is IFavouritableItem favouriteItem)
				{
					// Favourite button
					// If an item is favourited, the button should be active to unfavourite the item.
					// Or, it should be active to favorite an item if there are empty slots available.
					isFavouriteToggleAvailable = (favouriteItem.IsItemFavourited || !favouriteItem.IsItemFavourited &&
						playerInventoryComponent.GetEmptyFavouriteSlot(out int slotIndex) != null);

					// If the toggle is available, check if the text should say favourite or unfavourite
					if (isFavouriteToggleAvailable)
						_favourite_Text.text = !favouriteItem.IsItemFavourited ? favouriteText : unfavouriteText;
				}

				// Set the trash button as avaiable
				isTrashAvailable = true;
				break;

			case EInventoryType.Container:

				// Set thecontainer primary button as avaiable
				isContainerPrimaryAvailable = true;

				// Set the trash button as avaiable
				isTrashAvailable = true;
				break;

			case EInventoryType.Vendor:

				// Set the vendor primary button as avaiable
				isVendorPrimaryAvailable = true;

				break;

		}

		// Enable the primary button holder if any of its children are active
		_primaryButtonHolder.SetActive(isPInventoryPrimaryAvailable || isContainerPrimaryAvailable || isVendorPrimaryAvailable);

		if (_primaryButtonHolder.activeInHierarchy)
		{
            // Stop the PlayerInventoryComponent's and InventoryDisplay's controls
            playerInventoryComponent.SetControlsSubscription(false);
            currentInventoryDisplay.SetControlsSubscription(false);
        }

        // Set the correct display primary buttons to active
        _playerInventoryPrimaryFunction_Button.gameObject.SetActive(isPInventoryPrimaryAvailable);

		_containerPrimaryFunction_Button.gameObject.SetActive(isContainerPrimaryAvailable);

		_vendorPrimaryFunction_Button.gameObject.SetActive(isVendorPrimaryAvailable);

		// Set favourite button enabled based on inventory & item state
		_favourite_Button.gameObject.SetActive(isFavouriteToggleAvailable);

		// Set trash button enabled based on inventory & item state
		_trash_Button.gameObject.SetActive(isTrashAvailable);
	}

	#region Button Methods

	/// <summary>
	/// This button should be called by the primary button for the player inventory.
	/// </summary>
	public void PlayerInventoryPrimary()
	{
		if (currentDisplayType == EInventoryType.PlayerInventory)
		{
			// Try to use the item for its main purpose
			(currentInventoryDisplay as PlayerInventoryDisplay).TryDisplayMainFunction(currentSlotDisplay);

			// Close this panel
			Back();
		}
	}

	/// <summary>
	/// This button should be called by the primary button for the player inventory.
	/// </summary>
	public void ContainerPrimary()
	{
		// Open the quantity panel under the move context
		_itemQuantityDisplay.SetItem(currentSlotDisplay, currentInventoryDisplay, ItemQuantityDisplay.EQuantityInteractionType.Move);

		// Close this panel
		Back();
	}

	/// <summary>
	/// This button should be called by the primary button for the player inventory.
	/// </summary>
	public void VendorPrimary()
	{
		// Open the quantity panel under the transaction context
		_itemQuantityDisplay.SetItem(currentSlotDisplay, currentInventoryDisplay, ItemQuantityDisplay.EQuantityInteractionType.Transaction);

		// Close this panel
		Back();
	}

	/// <summary>
	/// This button should be called by the favourite button.
	/// For items that can be equipped, this method toggles if they are favourited or not
	/// </summary>
	public void Favourite()
	{
		if (currentDisplayType == EInventoryType.PlayerInventory)
			(currentInventoryDisplay as PlayerInventoryDisplay).TryFavouriteItem(currentSlotDisplay);

		// Close this panel
		Back();
	}

	/// <summary>
	/// This button should be called by the trash button.
	/// It opens the item amount selection display which is used to destroy a number of items
	/// </summary>
	public void Trash()
	{
		// Open the quantity panel under the trash context
		_itemQuantityDisplay.SetItem(currentSlotDisplay, currentInventoryDisplay, ItemQuantityDisplay.EQuantityInteractionType.Trash);

		// Close this panel
		Back(false);
	}

	/// <summary>
	/// This button should be called by the back button.
	/// It disables this option display as well as resets it to a null state
	/// </summary>
	public void Back(bool enableControls = true)
	{
		// Hide this display
		SetDisplayActive(false);

		// Enable the PlayerInventoryComponent's and InventoryDisplay's controls
		playerInventoryComponent.SetControlsSubscription(enableControls);
		currentInventoryDisplay.SetControlsSubscription(enableControls);

		// Reset vars to a null state
		currentSlotDisplay = null;
		currentInventoryDisplay = null;
	}
	#endregion
}
