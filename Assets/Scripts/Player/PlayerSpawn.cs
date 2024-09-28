using UnityEngine;

public class PlayerSpawn : MonoBehaviour
{
	private void OnDrawGizmos()
	{
		Gizmos.color = Color.green;
		Gizmos.DrawWireCube(transform.position + Vector3.up, new Vector3(1, 2, 1));
	}
}
