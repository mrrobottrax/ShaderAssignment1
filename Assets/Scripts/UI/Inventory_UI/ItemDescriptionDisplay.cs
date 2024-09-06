using UnityEngine;
using TMPro;
using UnityEngine.UI;
using static UnityEditor.Progress;

public class ItemDescriptionDisplay : MonoBehaviour
{
    [Header("Display Context")]
    [SerializeField] private RectTransform _thisRectTransform;

    [Header("Required Feilds")]
    [SerializeField] private TextMeshProUGUI _itemName;
    [SerializeField] private TextMeshProUGUI _itemDescription;
    [SerializeField] private Image _itemImage;

    [Space]

    [Header("Item Uses")]
    [SerializeField] private StatDescription _armorStat;
    [SerializeField] private StatDescription _damageStat;
    [SerializeField] private StatDescription _healthStat;
    [SerializeField] private StatDescription _valueStat;

    [System.Serializable]
    private class StatDescription
    {
        public GameObject StatObject;
        public TextMeshProUGUI StatText;

        public void EnableStatDisplay(string text)
        {
            StatObject.SetActive(true);
            StatText.text = text;
        }

        public void DisableStatDisplay()
        {
            StatObject.SetActive(false);
            StatText.text = null;
        }
    }

    /// <summary>
    /// This method updates the description to match an items data
    /// </summary>
    public void UpdateDescription(ItemData_Base item)
    {
        ClearDescription();

        // Set name, description, image
        _itemName.text = item.ItemName;
        _itemDescription.text = item.ItemDescription;
        _itemImage.sprite = item.ItemSprite;
        _itemImage.gameObject.SetActive(true);

        //- Determine which stats this item should display

        if (item is Armour_ItemData armorItem)
            _armorStat.EnableStatDisplay(armorItem.Defence.ToString());

        if (item is Weapon_ItemData weaponItem)
            _damageStat.EnableStatDisplay(weaponItem.BaseDamage.ToString());

        /*
        if (item is Aid_ItemData survivalItem)
            _healthStat.EnableStatDisplay(survivalItem.Heals.ToString());
        */

        if (item.ItemBaseValue > 0)
            _valueStat.EnableStatDisplay(item.ItemBaseValue.ToString());

        // Enable Window
        gameObject.SetActive(true);
        LayoutRebuilder.ForceRebuildLayoutImmediate(_thisRectTransform);
    }

    /// <summary>
    /// This method resets the description back to a null state
    /// </summary>
    public void ClearDescription()
    {
        // Clear name, description, and sprite
        _itemName.text = "";
        _itemDescription.text = "";
        _itemImage.sprite = null;
        _itemImage.gameObject.SetActive(false);

        // Hide stats
        _armorStat.DisableStatDisplay();
        _damageStat.DisableStatDisplay();
        _healthStat.DisableStatDisplay();
        _valueStat.DisableStatDisplay();

        // Disable Window
        gameObject.SetActive(false);
    }
}
