using System.Collections.Generic;
using UnityEngine;

public abstract class ViewModel_Base : MonoBehaviour
{
    [field: Header("View Model")]
    [field: SerializeField] public MeshRenderer PrimaryMesh { get; private set; }
    [field: SerializeField] public MeshRenderer[] AdditionalMeshes { get; private set; }

    [field: SerializeField] public string AnimationLayer { get; private set; }
    [field: SerializeField] public string AnimationSet { get; private set; }

    [field: Header("Functions")]
    [field: SerializeField] public AttackList ViewModelAttacks { get; private set; }

    protected Dictionary<string, ViewModelAction> functions = new Dictionary<string, ViewModelAction>();


    #region ViewModel Functionality

    /// <summary>
    /// This method executes a view model's function based on the action title passed in.
    /// </summary>
    /// <param name="player">The player object performing the action.</param>
    /// <param name="viewModelManager">The view model manager handling the action.</param>
    /// <param name="weaponItem">The weapon item involved in the action.</param>
    /// <param name="attack">The attack data for the action.</param>
    /// <param name="actionTitle">The title of the action to execute.</param>
    public void TryModelFunction(PlayerHealth player, PlayerViewModelManager viewModelManager, Weapon_Item weaponItem, AttackList.Attack attack, string actionTitle)
    {
        Debug.Log(actionTitle);

        // Check if the action title exists in the functions dictionary
        if (functions.ContainsKey(actionTitle))
        {
            // Execute the function associated with the action title
            functions[actionTitle].Execute(player, viewModelManager, this, weaponItem, attack);
        }
        else
        {
            // Log a warning if the action title does not exist
            Debug.LogWarning($"Action title '{actionTitle}' does not exist in the functions dictionary of {PrimaryMesh.name}");
        }
    }
    #endregion

    #region ViewModel Rendering Methods

    /// <summary>
    /// This method either enables or disables the mesh renderer of the primary mesh of a view model.
    /// </summary>
    public void SetPrimaryMeshActive(bool isActive)
    {
        PrimaryMesh.enabled = isActive;
    }

    /// <summary>
    /// This method uses an ID to either enable or disable the mesh renderer of one the additional meshes of a view model.
    /// </summary>
    public void SetAdditionalMeshActive(int ID, bool isActive)
    {
        AdditionalMeshes[ID].enabled = isActive;
    }

    /// <summary>
    /// This method either enables or disables all of the ViewModels associated meshes
    /// </summary>
    public void SetViewModelMeshesActive(bool isActive)
    {
        // Set Primary Mesh
        SetPrimaryMeshActive(isActive);

        // Set Additional Meshes
        if (AdditionalMeshes.Length > 0)
            for (int i = 0; i < AdditionalMeshes.Length; i++)
                SetAdditionalMeshActive(i, isActive);
    }
    #endregion

    /// <summary>
    /// This class should be used to create a concrete weapon function
    /// </summary>
    protected abstract class ViewModelAction
    {

        /// <summary>
        /// This method executs an actions logic
        /// </summary>
        /// <param name="player">The players entity</param>
        /// <param name="viewModelManager">The players view model manager</param>
        /// <param name="viewModel">The view model this action originates from</param>
        /// <param name="weaponItem">The item this action represents </param>
        /// <param name="attack">The attack data used. This is taken from the view models AttackList</param>
        public abstract void Execute(PlayerHealth player, PlayerViewModelManager viewModelManager, ViewModel_Base viewModel, Weapon_Item weaponItem, AttackList.Attack attack);
    }
}

