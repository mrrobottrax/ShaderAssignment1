using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class FavouriteWheelDisplay : MenuDisplayBase
{
	[Header("UI Elements")]
	[SerializeField] private Transform _wheelDisplayRect;
	[SerializeField] private TextMeshProUGUI _itemText;
	[SerializeField] private string _noItemText;

	[Header("Slices")]
	[SerializeField] private float wheelRotOffset = 0f;
	[SerializeField] private FavouriteWheelSliceDisplay[] sliceDisplays = new FavouriteWheelSliceDisplay[8];

	[Header("System")]
	private float sliceRange; // The range in degrees of each slice

	private Vector2 axisRaw;

	private FavouriteWheelSliceDisplay currentSlice;// The slice currently hovered over

	#region Initialization Methods

	private void Start()
	{
		sliceRange = 360 / sliceDisplays.Length;
	}
    #endregion

    #region Input Methods

    public override void SetControlsSubscription(bool isInputEnabled)
	{
        if (isInputEnabled)
            Subscribe();
        else if (InputManager.Instance != null)
            Unsubscribe();
    }

    /// <summary>
    /// This method gets the axis used to determine where the player is pointing on the wheel
    /// </summary>
    private void LookInput(InputAction.CallbackContext context)
    {
        // Cache the input axis
        axisRaw = context.ReadValue<Vector2>().normalized;

        // Check if the input is from the mouse
        if (context.control.device is Mouse)
        {
            // Since mouse pos is delta rather than axis, we need to get the direction the mouse is in from the center of the UI.
            axisRaw = (Input.mousePosition - _wheelDisplayRect.position).normalized;

            return;
        }
    }

    public override void Subscribe()
    {
        // Reset the axis when controls are enabled
        axisRaw = Vector2.zero;

        // Look input
        InputManager.Instance.controls.Player.Look.performed += LookInput;
    }

    public override void Unsubscribe()
    {
        // Look input
        InputManager.Instance.controls.Player.Look.performed -= LookInput;

        // Select the last slice the player was over
        SelectSlice(currentSlice);
    }

    #endregion

    #region Menu Methods

    protected override void OnEnableDisplay()
	{
		base.OnEnableDisplay();

		// Refresh UI
		foreach (FavouriteWheelSliceDisplay i in sliceDisplays)
			i.RefreshContents();
	}
	#endregion

	/// <summary>
	/// This method pairs all of the displays slots with an inventories
	/// </summary>
	public void AssignSlots(InventorySlotPointer[] inventorySlots)
	{
		for (int i = 0; i < inventorySlots.Length; i++)
		{
			FavouriteWheelSliceDisplay display = sliceDisplays[i];
			display.PairSlot(inventorySlots[i]);
		}
	}

    /// <summary>
    /// This method sets the slice that the player is currently hovering over
    /// </summary>
    private void SetCurrentSlice(FavouriteWheelSliceDisplay slice)
	{
		// Reset the last slot the player was over if there was one
		if (currentSlice != null) currentSlice.SetSlotCurrent(false);

		// Set the new slot as the current slot
		currentSlice = slice;

		// Ensure a null slot was not passed in
		if (currentSlice != null)
		{
			currentSlice.SetSlotCurrent(true);

			if (slice.AssignedSlot.GetPairedSlot() != null)
			{
				if (slice.AssignedSlot.GetPairedSlot().GetSlotsItem() != null) // Set the item text name
					_itemText.text = currentSlice.AssignedSlot.GetPairedSlot().GetSlotsItem().GetItemData().ItemName;
				else // Reset the item text
					_itemText.text = _noItemText;
			}
			else _itemText.text = _noItemText;
		}
		else
			_itemText.text = _noItemText;
	}

	/// <summary>
	/// Selects the given slice and uses the item assigned to it, if any.
	/// </summary>
	/// <param name="slice">The slice to be used for equipping the item.</param>
	private void SelectSlice(FavouriteWheelSliceDisplay slice)
	{
		// Equip the item in the slot that was selected
		if (slice != null)
		{
			// Check if the selected slice has an item to equip in it
			if (slice.AssignedSlot.GetPairedSlot() != null &&
				slice.AssignedSlot.GetPairedSlot().GetSlotsItem() != null)
			{
				// Use the items favourite function
				if (slice.AssignedSlot.GetPairedSlot().GetSlotsItem() is IFavouritableItem favouritableItem)
					favouritableItem.UseFavouritedItem();
			}
			else // In the case that an empty slot was selected, leave the player unarmed
				Player.Instance.GetViewModelManager().ClearCurrentViewModel();
		}
		else
			Player.Instance.GetViewModelManager().ClearCurrentViewModel();
	}

    #region Unity Callbacks

    private void Update()
    {
        // Ensure the player is moving the axis before 
        if (axisRaw.magnitude > 0)
        {
            // Calculate the angle from the center that the look input is
            float angle = Mathf.Atan2(axisRaw.x, axisRaw.y) * Mathf.Rad2Deg;

            // Apply the wheel offset
            angle += wheelRotOffset;

            // Normalize the angle to be between 0 and 360 degrees
            if (angle < 0)
                angle += 360;
            if (angle >= 360)
                angle -= 360;

            // Determine which slice the angle falls into
            int sliceID = Mathf.FloorToInt(angle / sliceRange);

            // Set the current slice
            SetCurrentSlice(sliceDisplays[sliceID]);
        }
        else
            SetCurrentSlice(null);
    }
    #endregion
}
