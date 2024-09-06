using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class FilterButton : MonoBehaviour
{
    [SerializeField] private InventoryDisplay inventoryDisplay;
    [SerializeField] private Button _filtersButton;
    [SerializeField] private InventoryDisplay.FilterType _filterType;

    #region Button Listeners
    private void OnEnable()
    {
        _filtersButton?.onClick.AddListener(OnButtonClick);
    }

    private void OnDisable()
    {
        _filtersButton?.onClick.RemoveAllListeners();
    }


    private void OnButtonClick()
    {
        inventoryDisplay.FilterDisplaySlots(_filterType);
        inventoryDisplay.RefreshSlots();
    }
    #endregion
}
