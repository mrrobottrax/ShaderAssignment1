using UnityEngine;

[CreateAssetMenu(fileName = "Readable", menuName = "Readable", order = 0)]
public class ReadableData : ScriptableObject
{
    [field: Header("Background & Text Area")]
    [field:SerializeField] public float TextAreaWidth { get; private set; }
    [field: SerializeField] public float TextAreaHeight { get; private set; }
    [field: SerializeField] public Sprite TextBackground { get; private set; }

    [field: Header("Pages")]

    [field: TextArea(10, 15)]
    [field: SerializeField] public string[] PageText { get; private set; }
}
