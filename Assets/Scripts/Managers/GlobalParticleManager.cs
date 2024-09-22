using UnityEngine;

/// <summary>
/// This manager should be used for particle systems that need to follow the player on a global scale like wind and dust motes.
/// </summary>
public class GlobalParticleManager : MonoBehaviour
{
    /*
    [SerializeField] private Transform _globalParitlcesParent;

    [SerializeField] private Transform _globalWindParitlces;

    [Header("System")]
    private Transform cameraRef;

    private bool isWindParticlesActive;

    #region Initialization Methods

    private void Start()
    {
        EnableWindParticles(true);

        cameraRef = GameManager.Instance.GetPlayer().GetPlayerCamera().GetCamera();
    }
    #endregion

    #region Particle System Toggle Methods

    public void EnableWindParticles(bool enabled)
    {
        isWindParticlesActive = enabled;
        _globalWindParitlces.gameObject.SetActive(isWindParticlesActive);
    }
    #endregion

    #region Unity Callbacks

    private void Update()
    {
        // Update the particle parent to follow the players position
        _globalParitlcesParent.transform.position = cameraRef.transform.position;
    }
    #endregion
    */
}
