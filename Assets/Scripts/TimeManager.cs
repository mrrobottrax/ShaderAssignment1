using UnityEngine;

public class TimeManager : MonoBehaviour
{
    static TimeManager instance;
    public static TimeManager Instance { get { return instance; } }

    [Header("Clock Properties")]
    [SerializeField] private float _secPerMin = 1;

    [SerializeField] private int _maxMin = 60;
    [SerializeField] private int _maxHr = 24;
    [SerializeField] private int _maxDay = 30;
    [SerializeField] private int _maxMonth = 12;

    [Header("System")]
    private int min;
    private int hr;
    private int day;
    private int month;
    private int year;

    private bool isMorning = false;
    private E_WeekDay weekDay;

    float timer = 0;
    string displayTime;

    [RuntimeInitializeOnLoadMethod]
    static void Init()
    {
        instance = new GameObject("Time Manager").AddComponent<TimeManager>();
        DontDestroyOnLoad(instance);
    }

    private void Start()
    {
        // Set base clock
        hr = 8;
        min = 0;
        day = 1;
        month = 1;
        year = 1;

        isMorning = GetIsMorning();
        weekDay = GetWeekDay();

        UpdateTimeOfDay();
    }

    private void Update()
    {
        TickTime();
    }

    private void TickTime()
    {
        if (timer >= _secPerMin)
        {
            min++;

            // Check if the minutes exceeds max minutes in an hour
            if (min >= _maxMin)
            {
                min = 0;
                hr++;

                // Check if the hours exceeds max hours in a day
                if (hr >= _maxHr)
                {
                    hr = 0;
                    day++;

                    isMorning = GetIsMorning(); // Check if it is morning
                    weekDay = GetWeekDay();

                    // Check if the day exceeds max days in month
                    if (day > _maxDay)
                    {
                        day = 1;
                        month++;

                        // Check if the month exceeds max months in year
                        if (month > _maxMonth)
                        {
                            month = 1;
                            year++;
                        }
                    }
                }
            }

            UpdateTimeOfDay();

            timer = 0;
        }
        else
        {
            timer += Time.deltaTime;
        }
    }

    /// <summary>
    /// Updates the string that represents the time of day for displays
    /// </summary>
    private void UpdateTimeOfDay()
    {
        int h;

        if (hr >= 13)
        {
            h = hr - 12;
            isMorning = false;
        }
        else if (hr == 0)
        {
            h = 12;
            isMorning = true;
        }
        else
            h = hr;

        displayTime = h + ":";

        if (min <= 9)
            displayTime += "0" + min;
        else
            displayTime += min;

        if (isMorning)
            displayTime += " AM";
        else displayTime += " PM";
    }

    #region Utility

    public bool GetIsMorning()
    {
        return hr < 12;
    }

    public E_WeekDay GetWeekDay()
    {
        return (E_WeekDay)(((int)weekDay + 1) % 7);
    }

    public string GetTimeOfDay()
    {
        return displayTime;
    }
    #endregion
}

[System.Serializable]
public enum E_WeekDay
{
    Mon, Tue, Wed, Thu, Fri, Sat, Sun
}