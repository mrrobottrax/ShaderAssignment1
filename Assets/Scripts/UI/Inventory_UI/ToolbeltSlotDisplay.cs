using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class ToolbeltSlotDisplay : InventorySlotDisplay, IDragHandler, IBeginDragHandler, IEndDragHandler
{
	[SerializeField] float _lerpSpeed = 3;
	[SerializeField] AnimationCurve _lerpCurve;

	public Action OnDragAction;

	[HideInInspector] public Vector2 goalPosition;
	[HideInInspector] public bool disableLerp = false;

	Vector2 dragOffset = Vector2.zero;
	Vector2 dropPos = Vector2.zero;
	float lerpProgress = 1;

	new void Awake()
	{
		base.Awake();
	}

	public void OnBeginDrag(PointerEventData eventData)
	{
		disableLerp = true;
		lerpProgress = 0;
		dragOffset = new Vector2(transform.position.x, transform.position.y) - eventData.position;
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
}
