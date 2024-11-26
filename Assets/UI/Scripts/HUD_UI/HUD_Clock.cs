using TMPro;
using UnityEngine;

public class HUD_Clock : MonoBehaviour
{
    [Header("UI Elelements")]
    [SerializeField] private Animator animator;

    [SerializeField] private TextMeshProUGUI dateText;
    [SerializeField] private TextMeshProUGUI timeText;
    [SerializeField] private TextMeshProUGUI quotaTimeText;


    /// <summary>
    /// This method sets the value of the clock text
    /// </summary>
    public void SetValue(string date, string time, string quotaTime)
    {
        dateText.text = date;
        timeText.text = time;
        quotaTimeText.text = quotaTime;
    }

    public void ToggleClock(bool isActive)
    {
        if (isActive)
            animator.SetTrigger("Show");
        else animator.SetTrigger("Hide");
    }
}
