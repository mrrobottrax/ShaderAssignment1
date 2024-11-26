using UnityEngine;

public class BuyPeriodStart : MonoBehaviour
{
    public static BuyPeriodStart Instance;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else Destroy(gameObject);
    }
}
