using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryCursor : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _amountText;
    [SerializeField] private Image _itemImage;

    private void Awake()
    {
        ClearCursor();
    }

    public void SetCursorPos(Vector3 pos)
    {
        transform.position = pos;
    }

    public void AssignItem(Item_Base item)
    {
        _amountText.text = item.GetAmount().ToString();
        _itemImage.sprite = item.GetItemData().ItemSprite;
        _itemImage.gameObject.SetActive(true);
    }

    public void ClearCursor()
    {
        _amountText.text = "";
        _itemImage.sprite = null;
        _itemImage.gameObject.SetActive(false);
    }
}
