using UnityEngine;

public class LevelStartPositon : MonoBehaviour
{
    public static LevelStartPositon Instance;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else Destroy(gameObject);
    }
}
