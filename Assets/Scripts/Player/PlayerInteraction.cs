using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PlayerInteraction : MonoBehaviour
{
	[Header("References")]
	[SerializeField] Transform m_camera;
	[SerializeField] Text m_uiText;

	[Header("Parameters")]
	[SerializeField] LayerMask m_layerMask = ~0;
	[SerializeField] float m_maxDist = 2;

	InputAction m_useAction;

	private void Awake()
	{
		m_useAction = InputManager.Controls.Player.Use;

		m_uiText.text = null;
	}

	private void Update()
	{
		m_uiText.text = null;

		if (Physics.Raycast(
			m_camera.position,
			m_camera.forward,
			out RaycastHit hit,
			m_maxDist,
			m_layerMask,
			QueryTriggerInteraction.Collide
		))
		{
			if (hit.transform.gameObject.TryGetComponent(out GenericInteractable interactable))
			{
				m_uiText.text = interactable.m_text;

				if (m_useAction.ReadValue<float>() > 0)
				{
					interactable.OnUse();
					interactable.m_onUse.Invoke();
				}
			}
		}
	}
}
