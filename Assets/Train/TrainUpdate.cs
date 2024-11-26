using UnityEngine;

public class TrainUpdate : MonoBehaviour
{
    [SerializeField] LayerMask trainLayers;

    [SerializeField] E_TainUpdateType updateType;
    public enum E_TainUpdateType
    {
        None,
        RespawnPlayers,
        StopTrain,
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("TRAIN PASSED THROUGH");

        if(other.TryGetComponent(out SteamEngine steamEngine))
        {
            switch (updateType)
            {
                case E_TainUpdateType.RespawnPlayers:

                    // Call respawn from the cycle manager

                    break;

                case E_TainUpdateType.StopTrain:
                    steamEngine.SetBrakesActive(true);
                    break;
            }
        }
    }
}
