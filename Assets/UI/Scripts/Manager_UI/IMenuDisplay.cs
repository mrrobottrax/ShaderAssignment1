using UnityEngine;

public abstract class MenuDisplayBase : MonoBehaviour, IInputHandler
{
    [Header("Display UI Element")]
    [SerializeField] private GameObject _displayElement;

    [Header("System")]
    private bool isDisplayActive;

    #region Input Subscription

    public abstract void SetControlsSubscription(bool isSubscribing);

    public abstract void Subscribe();

    public abstract void Unsubscribe();
    #endregion

    #region Display Status Methods

    /// <summary>
    /// This method executes either the enable or disable logic implemented by the child class
    /// </summary>
    public void SetDisplayActive(bool isActive)
    {
        isDisplayActive = isActive;

        if (isDisplayActive)
            OnEnableDisplay();
        else OnDisableDisplay();
    }

    protected virtual void OnEnableDisplay()
    {
        _displayElement.SetActive(true);
        SetControlsSubscription(true);
    }

    protected virtual void OnDisableDisplay()
    {
        _displayElement.SetActive(false);
        SetControlsSubscription(false);
    }
    #endregion

    #region Helper Methods

    public bool GetDisplayActive()
    {
        return isDisplayActive;
    }
    #endregion

    protected void OnDestroy()
    {
        SetControlsSubscription(false);
    }
}
