using UnityEngine;
using UnityEngine.Events;

public class GenericInteractable : MonoBehaviour
{
	public string m_text;
	public UnityEvent m_onUse;

	public virtual void OnUse()
	{ }
}
