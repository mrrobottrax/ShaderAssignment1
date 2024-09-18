using UnityEngine;

//[CreateAssetMenu]
public class PropHuntData : ScriptableObject
{
	#region Singleton
	static PropHuntData s_instance;


	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
	static void OnRuntimeLoad()
	{
		s_instance = Resources.LoadAll<PropHuntData>("")[0];
	}
	#endregion

	[SerializeField] GameObject[] m_meshPrefabs;

	public static GameObject[] GetMeshes()
	{
		return s_instance.m_meshPrefabs;
	}
}
