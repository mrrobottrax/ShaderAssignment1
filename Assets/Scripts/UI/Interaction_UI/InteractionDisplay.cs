using System.Collections.Generic;
using UnityEngine;

public class InteractionDisplay : MonoBehaviour
{
    [Header("Display")]
    [SerializeField] private GameObject _optionHolder;// The rect that contains the displays options

    [Header("Components")]
    [SerializeField] private InteractionOptionDisplay _optionPrefab;// Used to clone more prompt buttons if needed
    [SerializeField] private GameObject _crossHair;

    [Header("System")]
    private List<InteractionOptionDisplay> availableOptionsPool = new List<InteractionOptionDisplay>();
    private bool isUsingCenterApproach;

    RectTransform optionHolderRect;

    [SerializeField] private Sprite inputSprite; // In the future we should take this from a sprite atlas so we can show contexts based on different platforms

    // This pointer is used to transform the display
    private Transform pointToFollow;

    private void Awake()
    {
        // Add the object used to clone more options to the pool
        availableOptionsPool.Add(_optionPrefab);

        // Cache the holders rect
        optionHolderRect = _optionHolder.GetComponent<RectTransform>();
    }

    private void FixedUpdate()
    {
        if (pointToFollow != null && !isUsingCenterApproach)
        {
            _optionHolder.transform.position = Camera.main.WorldToScreenPoint(pointToFollow.position);
        }
        else if (pointToFollow == null && !isUsingCenterApproach)
        {
            enabled = false;
            ClearInteractableOptions();
        }
    }

    /// <summary>
    /// This method determines if the center of the screen is within the option holders rect. If true, the player can interact.
    /// </summary>
    public bool ValidateInteraction()
    {
        Vector2 screenCenter = new Vector2(Screen.width / 2, Screen.height / 2);
        return RectTransformUtility.RectangleContainsScreenPoint(optionHolderRect, screenCenter);
    }

    /// <summary>
    /// This method displays all of an interactables options
    /// </summary>
    public void SetInteratableOptions(PlayerInteraction playerInteraction, Transform followPoint, Interactable interactable)
    {
        // Clear the prev prompt
        ClearInteractableOptions();

        // Cache new follow point
        pointToFollow = followPoint;
        _optionHolder.SetActive(true); // Enable the option holder object

        // Cache interactions and determine if the center screen aproach should be used
        IInteraction[] interactions = interactable.GetInteractions(out bool isCenterApproach);
        isUsingCenterApproach = isCenterApproach;

        if (!isUsingCenterApproach)
        {
            // Clone Option prefab if the pool is not big enough
            if (availableOptionsPool.Count < interactions.Length)
            {
                // Add the difference to the pool of available options
                int difference = interactions.Length - availableOptionsPool.Count;
                for (int i = 0; i < difference; i++)
                {
                    InteractionOptionDisplay instance = Instantiate(_optionPrefab, optionHolderRect);
                    availableOptionsPool.Add(instance);
                }
            }

            // Init all buttons
            for (int i = 0; i < interactions.Length; i++)
            {
                IInteraction interaction = interactions[i];
                availableOptionsPool[i].SetOption(playerInteraction, interaction.InteractSprite, inputSprite, interaction.InteractionPrompt, interaction);
            }
        }
        else if (interactions.Length > 0)// If the system is using the center approach, ensure there is an interaction present on the interactable.
        {
            // Use the first interaction
            IInteraction interaction = interactions[0];
            availableOptionsPool[0].SetOption(playerInteraction, interaction.InteractSprite, inputSprite, interaction.InteractionPrompt, interaction);

            // Hide the crosshair 
            _crossHair.SetActive(false);
        }

        // Enable this script to update prompt pos
        enabled = true;
    }

    /// <summary>
    /// This method returns this script and all of its components to a null state
    /// </summary>
    public void ClearInteractableOptions()
    {
        // Clear follow point, this will cause the update loop to disable eveything by default
        pointToFollow = null;

        // Return the option holder to the center of the screen
        _optionHolder.transform.position = new Vector2(Screen.width / 2, Screen.height / 2);

        // Clear all of the options
        foreach (var i in availableOptionsPool)
        {
            i.ClearOption();
        }

        _optionHolder.SetActive(false);
        _crossHair.SetActive(true);
    }
}
