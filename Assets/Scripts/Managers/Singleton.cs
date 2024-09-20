using UnityEngine;

public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
	private static T instance;
	public static T Instance
	{
		get
		{
			if (GameManager.Instance == null) return null;

			if (!instance)
				instance = GameManager.Instance.GetComponent<T>();

			return instance;
		}
	}
}