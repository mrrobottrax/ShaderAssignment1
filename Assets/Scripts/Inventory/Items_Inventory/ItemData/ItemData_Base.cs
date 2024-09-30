using UnityEngine;

[System.Serializable]
public abstract class ItemData_Base : ScriptableObject
{
    [field: Header("Item Data")]
    [field: SerializeField] public string ItemName { get; private set; }
    [field: SerializeField] public Sprite ItemSprite { get; private set; }
    [field: SerializeField][field: TextArea(10, 15)] public string ItemDescription { get; private set; }

    [field: Header("Item Properties")]
    [field: SerializeField] public int StackSize { get; private set; } = 16;
    [field: SerializeField] public int ItemBaseValue { get; private set; } = 0;
    [field: SerializeField] public float ItemWeight { get; private set; } = 0;
}