using System;
using System.Linq;
using UnityEngine;

public class CycleManager : MonoBehaviour
{
    static CycleManager instance;
    public static CycleManager Instance { get { return instance; } }

    [Header("Components")]
    private Train trainRef;

    [Header("Cycle Properties")]
    [SerializeField] private int _startingQuota = 100;
    [SerializeField] private int _daysPerCycle = 1;

    [Header("Clock Properties")]
    [SerializeField] private float _secPerMin = 1;

    [SerializeField] private int _maxMin = 60;
    [SerializeField] private int _maxHr = 24;
    [SerializeField] private int _maxDay = 30;
    [SerializeField] private int _maxMonth = 12;

    [Header("System")]

    // Gameplay Cycle
    private int currentCycle = 0;
    private int daysRemainingInCycle = 0;
    private int profitQuota = 0;

    // Day night cycle
    private bool isTimeAdvancing;

    private int min;
    private int hr;
    private int day;
    private int month;
    private int year;

    private bool isMorning = false;
    private E_WeekDay weekDay;

    float timer = 0;
    string displayTime;

    [field: Header("Events")]
    public Action<int> OnHourAdvance;

    #region Initialization Methods

    [RuntimeInitializeOnLoadMethod]
    static void Init()
    {
        instance = new GameObject("CycleManager").AddComponent<CycleManager>();
        DontDestroyOnLoad(instance);
    }

    private void Start()
    {
        trainRef = Train.Instance;
        StartCycle();

    }
    #endregion

    #region Unity Callbacks

    private void Update()
    {
        if (isTimeAdvancing)
            TickTime();
    }

    #endregion

    #region Gameplay Cycle Methods

    /// <summary>
    /// Starts off the gameplay cycle
    /// </summary>
    public void StartCycle()
    {
        // Start quota
        profitQuota = _startingQuota;
        daysRemainingInCycle = _daysPerCycle;

        // Set base clock
        hr = 8;
        min = 0;
        day = 1;
        month = 0;
        year = 0;

        // Determine vars dependent on clock
        isMorning = GetIsMorning();
        weekDay = E_WeekDay.Mon;
        UpdateTimeOfDay();

        isTimeAdvancing = true;
    }

    /// <summary>
    /// Checks for items in a bounding box at a specified point
    /// </summary>
    public Item[] CheckItems(Vector3 _center, Vector3 _size)
    {
        Collider[] colliders = Physics.OverlapBox(_center, _size / 2, Quaternion.identity);

        // Ensure only items are passed
        Item[] items = colliders.Select(collider => collider.GetComponent<Item>())
            .Where(item => item != null)
            .ToArray();

        return items;
    }

    /// <summary>
    /// Either advances or stops the cycle based on the outcome of the current cycle
    /// </summary>
    public void EndCurrentCycle()
    {
        isTimeAdvancing = false;

        // Determine total profit
        int totalProfit = 0;
        Item[] items = CheckItems(trainRef.transform.position + trainRef.Center, trainRef.Size);
        foreach (Item i in items)
        {
            totalProfit += i.value;
        }
        Debug.Log(totalProfit.ToString());

        bool isSuccessful = totalProfit > profitQuota;

        if (isSuccessful)
        {
            // Continue cycle
            currentCycle++;
            profitQuota = _startingQuota * currentCycle;
            daysRemainingInCycle = _daysPerCycle;

            Debug.Log("CAN CONTINUE");
        }
        else
        {
            // Lose game
            Debug.Log("LOSER");
        }
    }

    #endregion

    #region Time Methods

    /// <summary>
    /// Ticks the in-game clock
    /// </summary>
    private void TickTime()
    {
        if (daysRemainingInCycle <= 0)
        {
            EndCurrentCycle();
            return;
        }

        if (timer >= _secPerMin)
        {
            min++;

            // Check if the minutes exceeds max minutes in an hour
            if (min >= _maxMin)
            {
                min = 0;
                hr++;

                OnHourAdvance?.Invoke(hr);

                // Check if the hours exceeds max hours in a day
                if (hr >= _maxHr)
                {
                    hr = 0;
                    day++;

                    daysRemainingInCycle--;

                    isMorning = GetIsMorning(); // Check if it is morning
                    AdvanceWeekDay();

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
        // Set AM/PM and convert to 12-hour format
        isMorning = GetIsMorning();
        int displayHour = (hr % 12 == 0) ? 12 : hr % 12;

        // Format the time string
        displayTime = $"{displayHour}:{min:D2} {(isMorning ? "AM" : "PM")}";
    }
    #endregion

    #region Utility

    public bool GetIsMorning()
    {
        return hr < 12;
    }

    public void AdvanceWeekDay()
    {
        weekDay = (E_WeekDay)(((int)weekDay + 1) % 7);
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