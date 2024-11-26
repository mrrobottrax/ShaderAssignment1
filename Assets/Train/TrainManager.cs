using UnityEngine;

public class TrainManager : MonoBehaviour
{
    public static TrainManager Instance;

    [Header("Train Cars")]
    [SerializeField] TrainCar engine;
    [SerializeField] TrainCar playerCar;
    [SerializeField] TrainCar shopCar;
    [SerializeField] TrainCar oreCar;

    [Header("Train Car Start Positions")]
    [SerializeField] Transform engineStartPos;
    [SerializeField] Transform playerCarStartPos;
    [SerializeField] Transform shopCarStartPos;
    [SerializeField] Transform oreCarPos;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else Destroy(gameObject);
    }

    public void ResetCarPositions()
    {
        engine.SetCarStopped(true);
        playerCar.SetCarStopped(true);
        shopCar.SetCarStopped(true);
        oreCar.SetCarStopped(true);

        engine.transform.localPosition = engineStartPos.position;
        engine.rb.MoveRotation(engineStartPos.transform.rotation);

        playerCar.transform.localPosition = playerCarStartPos.position;
        playerCar.rb.MoveRotation(playerCarStartPos.transform.rotation);

        shopCar.transform.localPosition = shopCarStartPos.position;
        shopCar.rb.MoveRotation(shopCarStartPos.transform.rotation);

        oreCar.transform.localPosition = oreCarPos.position;
        oreCar.rb.MoveRotation(oreCarPos.transform.rotation);
    }

    public void StartTrain()
    {
        engine.SetCarStopped(false);
        playerCar.SetCarStopped(false);
        shopCar.SetCarStopped(false);
        oreCar.SetCarStopped(false);
    }
}
