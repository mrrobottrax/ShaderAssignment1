using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryCursor : MonoBehaviour
{
    public bool HasItem { get; private set; }
    [SerializeField] private TextMeshProUGUI _amountText;
    [SerializeField] private Image _itemImage;

    private void Awake()
    {
        ClearCursor();
    }

    /// <summary>
    /// Sets the inventory cursors positon
    /// </summary>
    /// <param name="pos"></param>
    public void SetCursorPos(Vector3 pos)
    {
        transform.position = pos;
    }

    /// <summary>
    /// Assigns an items properties to the cursor UI
    /// </summary>
    public void AssignItem(Item_Base item)
    {
        _amountText.text = item.GetAmount().ToString();
        _itemImage.sprite = item.GetItemData().ItemSprite;
        _itemImage.gameObject.SetActive(true);

        HasItem = true;
    }

    /// <summary>
    /// Clears an items properties from the cursor UI
    /// </summary>
    public void ClearCursor()
    {
        _amountText.text = "";
        _itemImage.sprite = null;
        _itemImage.gameObject.SetActive(false);

        HasItem = false;
    }
}
