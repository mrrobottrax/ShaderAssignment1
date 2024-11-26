using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class ToolbeltSlotDisplay : InventorySlotDisplay, IDragHandler, IBeginDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
{
	[SerializeField] float _lerpSpeed = 3;
	[SerializeField] AnimationCurve _lerpCurve;

	public Action OnDragAction;

	[HideInInspector] public Vector2 goalPosition;
	[HideInInspector] public bool disableLerp = false;

	Vector2 dragOffset = Vector2.zero;
	Vector2 dropPos = Vector2.zero;
	float lerpProgress = 1;

	Canvas canvas;

	new void Awake()
	{
		base.Awake();
		canvas = GetComponent<Canvas>();
		canvas.overrideSorting = false;
	}

	public void OnBeginDrag(PointerEventData eventData)
	{
		disableLerp = true;
		lerpProgress = 0;
		dragOffset = new Vector2(transform.position.x, transform.position.y) - eventData.position;

		canvas.overrideSorting = true;
		canvas.sortingOrder = 20;
	}

	public void OnDrag(PointerEventData eventData)
	{
		transform.position = eventData.position + dragOffset;

		OnDragAction?.Invoke();
	}

	public void OnEndDrag(PointerEventData eventData)
	{
		disableLerp = false;
		dragOffset = Vector2.zero;
		dropPos = transform.position;

		canvas.overrideSorting = false;
	}

	private void Update()
	{
		if (disableLerp) return;

		transform.position = Vector2.Lerp(dropPos, goalPosition, _lerpCurve.Evaluate(lerpProgress));

		lerpProgress += Time.deltaTime * _lerpSpeed;
	}

	public void MoveTo(Vector2 pos)
	{
		dropPos = transform.position;
		goalPosition = pos;
		lerpProgress = 0;
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		_slotImage.color = _selectedColour;
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		_slotImage.color = _defaultColour;
	}
}
