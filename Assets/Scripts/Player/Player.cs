using UnityEngine;

public class Player : NetworkBehaviour
{
	[SerializeField] GameObject[] firstPersonObjects;
	[SerializeField] GameObject[] thirdPersonObjects;

	private void Start()
	{
		if (IsOwner)
		{
			SetLocalOnlyStuffEnabled(true);
		}
		else
		{
			SetLocalOnlyStuffEnabled(false);
		}
	}

	private void SetLocalOnlyStuffEnabled(bool enabled)
	{
		foreach (var obj in firstPersonObjects)
		{
			obj.SetActive(enabled);
		}

		foreach (var obj in thirdPersonObjects)
		{
			obj.SetActive(!enabled);
		}
	}
}