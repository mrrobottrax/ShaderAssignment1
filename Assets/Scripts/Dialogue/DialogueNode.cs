using UnityEngine;

[CreateAssetMenu(fileName = "Dialogue", menuName = "DialogueNode", order = 0)]
public class DialogueNode : ScriptableObject
{
    [field: Header("Text")]
    [field: SerializeField] public string DisplayName { get; private set; }
    [field: SerializeField, TextArea(10,15)] public string Text { get; private set; }

    [field: Header("FollowingNode")]
    [field: SerializeField] public DialogueNode FollowingNode { get; private set; }// If no node, this is treated as the end

    [field: Header("Properties")]
    [field: SerializeField] public AudioClip TypeClip { get; private set; }
    [field: SerializeField] public float PrintSpeed { get; private set; } = 0.03f;
}
