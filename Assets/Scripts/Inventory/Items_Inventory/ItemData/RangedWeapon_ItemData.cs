using UnityEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "Item", menuName = "Items/Ranged Weapon", order = 3)]
public class RangedWeapon_ItemData : Weapon_ItemData
{
    [field: Space(10)]

    [field: Header("Ranged Properites Data")]
    [field: SerializeField] public EConsumeOnFire AmmoType { get; private set; }
    public enum EConsumeOnFire
    {
        None,
        Ammo,
        Self
    }

    [field: SerializeField] public ItemData_Base AmmoItemData { get; private set; }

    [field: SerializeField] public int MagazineSize { get; private set; }

    public override Item_Base CreateItemInstance()
    {
        return new RangedWeapon_Item(this);
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(RangedWeapon_ItemData))]
public class RangedWeapon_ItemDataEditor : Editor
{
    // Properties from ItemData_Base
    SerializedProperty itemID;
    SerializedProperty itemName;
    SerializedProperty itemSprite;
    SerializedProperty itemDescription;
    SerializedProperty maxAmount;
    SerializedProperty itemBaseValue;

    // Properties from Weapon_ItemData
    SerializedProperty baseDamage;
    SerializedProperty baseWeaponRange;
    SerializedProperty attacks;
    SerializedProperty viewModelID;

    // Properties from RangedWeapon_ItemData
    SerializedProperty ammoType;
    SerializedProperty ammoItemData;
    SerializedProperty magazineSize;

    void OnEnable()
    {
        // Cache the serialized properties from ItemData_Base
        itemID = serializedObject.FindProperty("<ItemID>k__BackingField");
        itemName = serializedObject.FindProperty("<ItemName>k__BackingField");
        itemSprite = serializedObject.FindProperty("<ItemSprite>k__BackingField");
        itemDescription = serializedObject.FindProperty("<ItemDescription>k__BackingField");
        maxAmount = serializedObject.FindProperty("<MaxAmount>k__BackingField");
        itemBaseValue = serializedObject.FindProperty("<ItemBaseValue>k__BackingField");

        // Cache the serialized properties from Weapon_ItemData
        baseDamage = serializedObject.FindProperty("<BaseDamage>k__BackingField");
        baseWeaponRange = serializedObject.FindProperty("<BaseWeaponRange>k__BackingField");
        viewModelID = serializedObject.FindProperty("<ViewModelID>k__BackingField");

        // Cache the serialized properties from RangedWeapon_ItemData
        ammoType = serializedObject.FindProperty("<AmmoType>k__BackingField");
        ammoItemData = serializedObject.FindProperty("<AmmoItemData>k__BackingField");
        magazineSize = serializedObject.FindProperty("<MagazineSize>k__BackingField");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Draw properties from ItemData_Base
        EditorGUILayout.PropertyField(itemID);
        EditorGUILayout.PropertyField(itemName);
        EditorGUILayout.PropertyField(itemSprite);
        EditorGUILayout.PropertyField(itemDescription);
        EditorGUILayout.PropertyField(maxAmount);
        EditorGUILayout.PropertyField(itemBaseValue);

        // Draw properties from Weapon_ItemData
        EditorGUILayout.PropertyField(baseDamage);
        EditorGUILayout.PropertyField(baseWeaponRange);
        EditorGUILayout.PropertyField(viewModelID);

        // Draw properties from RangedWeapon_ItemData
        EditorGUILayout.PropertyField(ammoType);

        // Only draw the fields for ammo and magazine size if the item uses ammo
        if ((RangedWeapon_ItemData.EConsumeOnFire)ammoType.enumValueIndex == RangedWeapon_ItemData.EConsumeOnFire.Ammo)
        {
            EditorGUILayout.PropertyField(ammoItemData);
            EditorGUILayout.PropertyField(magazineSize);
        }

        // Apply any changes to the serialized object
        serializedObject.ApplyModifiedProperties();
    }
}
#endif