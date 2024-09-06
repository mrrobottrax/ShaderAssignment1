using UnityEngine;

[RequireComponent(typeof(LightColumn))]
public class OldSpotlamp_Viewmodel : ViewModel_Base
{
    [SerializeField] private Transform _castPoint;
    private static bool isLightEnabled;
    private static FirstPersonCamera firstPersonCamera;
    private static LightColumn lightColoumn;

    private void Awake()
    {

        lightColoumn = GetComponent<LightColumn>();

        // Add available actions to the functions list
        functions.Add("Spotlamp_Toggle", new Shine());
    }

    private void Start()
    {
        firstPersonCamera = Player.Instance.GetPlayerCamera();
        lightColoumn.enabled = false;
    }

    public void Update()
    {
        if (isLightEnabled)
            lightColoumn.CastLight(_castPoint.position, firstPersonCamera.GetCamera().forward);
    }

    #region Attack Functions

    private class Shine : ViewModelAction
    {

        public override void Execute(Player player, PlayerViewModelManager viewModelManager, ViewModel_Base viewModel, Weapon_Item weaponItem, AttackList.Attack attack)
        {
            Weapon_ItemData weaponData = weaponItem.GetWeaponData();

            // Toggle light
            isLightEnabled = !isLightEnabled;
            lightColoumn.enabled = isLightEnabled;
        }
    }
    #endregion
}
