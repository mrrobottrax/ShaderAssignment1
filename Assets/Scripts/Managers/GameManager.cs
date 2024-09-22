using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Singleton")]
    private static GameManager instance;
    public static GameManager Instance
    {
        get
        {
            if (!isApplicationClosing)
            {
                // Create new instance if none is found
                if (instance == null)
                    instance = CreateSingletonManager().GetComponent<GameManager>();

                return instance;
            }
            else return null;
        }
    }

    [field: Header("Components")]
    private Player player;

    [field: Header("System")]
    private static bool isApplicationClosing = false;

    #region Initialization Methods

    /// <summary>
    /// This static method creates an instance of the game manager.
    /// This should only be called when no manager exists
    /// </summary>
    private static GameObject CreateSingletonManager()
    {
        var managerPrefab = Instantiate(Resources.Load<GameObject>("Managers/GameManager"));

        // This will be helpful for debugging instances when the manager should not be created
        Debug.Log("Game Manager Created");

        return managerPrefab;
    } 

    private void Awake()
    {
        // Check if a GameManager instance already exists
        if (instance != null && instance != this)
        {
            // Destroy the new this instance if one already was found
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(this);
    }

    #endregion

    #region Player Reference Methods
    public Player GetPlayer()
    {
        return player;
    }

    public void SetPlayer(Player player)
    {
        this.player = player;
    }

    /// <summary>
    /// This method disables FPS controls then clears the player
    /// </summary>
    public void ClearPlayer()
    {
        player = null;
    }
    #endregion

    #region Helper Methods

    private void OnApplicationQuit()
    {
        isApplicationClosing = true;
    }
    #endregion
}
