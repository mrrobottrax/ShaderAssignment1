using System.Collections;
using TMPro;
using UnityEngine;

public class HUD_AmmoDisplay : MonoBehaviour
{
    [Header("Properties")]
    [SerializeField] private float _timeBeforeHidden;

    [Header("UI Elelements")]
    [SerializeField] private Animator _animator;
    [SerializeField] private TextMeshProUGUI _clipAndClipSizeText; // The top text
    [SerializeField] private TextMeshProUGUI ammoTotalText; // The bottom text
    [SerializeField] private GameObject _devider;

    [Header("System")]
    private bool isDisplayHidden = true;
    private Coroutine hideTimer;
    private WaitForSeconds waitForSeconds;

    private void Awake()
    {
        // Create wait timer on awake to save space
        waitForSeconds = new WaitForSeconds(_timeBeforeHidden);
    }

    /// <summary>
    /// This method refreshes the HUD's ammo display.
    /// </summary>
    /// <param name="isTopUsed"> Toggles the text that displays amountInClip and clipSize text</param>
    /// <param name="amountInClip"> This int represents the amount of ammo currently in the weapons clip</param>
    /// <param name="clipSize"> This int represents the capacity of the weapons clip</param>
    /// <param name="ammoTotal"> This int represents how much of the weapons desired ammo type the player has in their inventory</param>
    public void SetDisplay(bool isTopUsed, int amountInClip, int clipSize, int ammoTotal)
    {
        // Enable the top portion of the display if needed
        _clipAndClipSizeText.gameObject.SetActive(isTopUsed);
        _devider.SetActive(isTopUsed);

        // Set the top text
        _clipAndClipSizeText.text = $"{amountInClip} / {clipSize} ";

        // Set the bottom text
        ammoTotalText.text = ammoTotal.ToString();

        // Start the display refresh animation
        RefreshDisplay();
    }


    /// <summary>
    /// When the display is refreshed, it either enables the slider element, or restarts the hide countdown.
    /// </summary>
    private void RefreshDisplay()
    {
        // Turn slider graphic on
        if (isDisplayHidden)
        {
            isDisplayHidden = false;
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

        isDisplayHidden = true;
        _animator.SetTrigger("Hide");
    }
}
