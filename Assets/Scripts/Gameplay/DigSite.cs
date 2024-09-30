using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class DigSite : MonoBehaviour
{
	[Header("Dig Parameters")]
	[SerializeField] private float _digTime;
	[SerializeField] private AnimationCurve _digCurve;

	[Header("Diggable Object")]
	[SerializeField] private Transform _diggableObject;

	[Header("Dig progress position")]
	[SerializeField] private Transform[] _progressPositions;

	[Header("System")]
	private SphereCollider sphereCollider;

	private bool isDigging;
	private int digProgress;

	private void Awake()
	{
		sphereCollider = GetComponent<SphereCollider>();
		sphereCollider.isTrigger = true;
	}

	public void TryAdvanceStage()
	{
		if (!isDigging && digProgress < _progressPositions.Length)
		{
			isDigging = true;

			Transform newPos = _progressPositions[digProgress];
			StartCoroutine(LerpDiggableObject(_digTime, _diggableObject, newPos));

			digProgress++;


			if (digProgress >= _progressPositions.Length)
			{
				sphereCollider.enabled = false;
			}
		}
	}

	IEnumerator LerpDiggableObject(float digDur, Transform startPos, Transform endPos)
	{
		float endTime = Time.time + digDur;

		float curProgress = 0;
		while (Time.time <= endTime)
		{
			yield return new WaitForFixedUpdate();

			curProgress += Time.fixedDeltaTime / digDur;

			_diggableObject.SetPositionAndRotation(
				Vector3.Lerp(startPos.position, endPos.position, _digCurve.Evaluate(curProgress)),
				Quaternion.Lerp(startPos.rotation, endPos.rotation, _digCurve.Evaluate(curProgress))
				);
		}

		isDigging = false;
	}
}
