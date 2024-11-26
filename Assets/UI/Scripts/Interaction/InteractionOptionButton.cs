using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InteractionOptionButton : MonoBehaviour
{
	[SerializeField] TMP_Text _promptText;
	[SerializeField] Image _interactImage;
	[SerializeField] Image _interactButtonImage;

	RectTransform _rect;
	InteractionUIManager promptManager;

	Interaction interaction;

	private void Awake()
	{
		_rect = GetComponent<RectTransform>();
		_interactButtonImage.enabled = false;
	}

	public void SetManager(InteractionUIManager manager)
	{
		promptManager = manager;
	}

	public void Setup(Interaction interaction)
	{
		this.interaction = interaction;
		_promptText.text = interaction.prompt;
		_interactImage.sprite = interaction.sprite;
	}

	public Interaction GetInteraction()
	{
		return interaction;
	}

	public bool IsSelected()
	{
		Vector2 screenCenter = new(Screen.width / 2, Screen.height / 2);
		return RectTransformUtility.RectangleContainsScreenPoint(_rect, screenCenter);
	}

	public void ShowButtonImage(bool show)
	{
		_interactButtonImage.enabled = show;
	}
}