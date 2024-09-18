using UnityEngine;

enum PropHuntRole : byte
{
	Hunter,
	Prop
}

public class PropHuntPlayer : NetworkBehaviour
{
	[NetVar] PropHuntRole role = PropHuntRole.Prop;
	[NetVar(nameof(OnMeshChange))] int meshIndex = 0;

	[SerializeField] GameObject meshParent;
	[SerializeField] MeshRenderer defaultMeshRenderer;

	GameObject instantiatedMesh;

	static bool hunterSelected = false;
	static float hunterSelectionChance = 0;

	public static void StaticReset()
	{
		hunterSelected = false;
		hunterSelectionChance = 0;
	}

	public void Init()
	{
		if (NetworkManager.Mode == ENetworkMode.Host)
		{
			// Randomly select one player as the hunter
			if (!hunterSelected)
			{
				hunterSelectionChance += 1.0f / NetworkManager.GetPlayerCount();
				hunterSelected |= Random.value <= hunterSelectionChance;

				if (hunterSelected)
				{
					role = PropHuntRole.Hunter;
					meshIndex = -1;
				}
				else
				{
					role = PropHuntRole.Prop;
					meshIndex = Random.Range(0, PropHuntData.GetMeshes().Length - 1);
				}
			}
		}
	}

	void OnMeshChange()
	{
		if (instantiatedMesh)
		{
			Destroy(instantiatedMesh);
		}

		if (meshIndex >= 0)
		{
			EnableDefaultMesh(false);

			instantiatedMesh = Instantiate(PropHuntData.GetMeshes()[meshIndex], meshParent.transform);
		}
		else
		{
			EnableDefaultMesh(true);
		}
	}

	void EnableDefaultMesh(bool enabled)
	{
		defaultMeshRenderer.enabled = enabled;
	}
}
