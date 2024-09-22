using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class HUD_HealthBar : MonoBehaviour
{
    [Header("Properties")]
    [SerializeField] private float _timeBeforeHidden;

    [Header("UI Elelements")]
    [SerializeField] private Animator _animator;
    [SerializeField] private Slider _healthBar;

    [Header("System")]
    private bool isHealthBarHidden = true;
    private Coroutine hideTimer;
    private WaitForSeconds waitForSeconds;

    private void Awake()
    {
        // Create wait timer on awake to save space
        waitForSeconds = new WaitForSeconds(_timeBeforeHidden);
    }

    /// <summary>
    /// This method sets the value of the healthbar and refreshes the animation
    /// </summary>
    public void SetHealthBar(int value)
    {
        _healthBar.value = value;
        RefreshDisplay();
    }

    /// <summary>
    /// When the display is refreshed, it either enables the slider element, or restarts the hide countdown.
    /// </summary>
    private void RefreshDisplay()
    {
        // Turn slider graphic on
        if (isHealthBarHidden)
        {
            isHealthBarHidden = false;
            _animator.SetTrigger("Show");
        }

        // Stop timer if there is one
        if (hideTimer != null)
            StopCoroutine(hideTimer);

        // Begin a new timer
        hideTimer = StartCoroutine(HideCountdown());

    }

    /// <summary>
    /// Hides the Health Bar after wait time has elapsed
    /// </summary>
    /// <returns></returns>
    private IEnumerator HideCountdown()
    {
        yield return waitForSeconds;

        isHealthBarHidden = true;
        _animator.SetTrigger("Hide");
    }
}
