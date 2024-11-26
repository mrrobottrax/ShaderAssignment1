using System;
using System.Linq;
using UnityEngine;

public class CycleManager : MonoBehaviour
{
    static CycleManager instance;
    public static CycleManager Instance { get { return instance; } }

    [Header("Components")]
    private OreCar lootValidator;

    [Header("Cycle Properties")]
    [SerializeField] private int startingQuota = 100;
    [SerializeField] private int daysPerCycle = 3;

    [Header("Clock Properties")]
    [SerializeField] private float secPerMin = 0.5f;

    [SerializeField] private int maxMin = 60;
    [SerializeField] private int maxHr = 24;
    [SerializeField] private int maxDay = 30;
    [SerializeField] private int maxMonth = 12;

    [Header("Components")]
    private LightManager lightManager;
    private TrainManager trainManager;

    [Header("System")]

    // Gameplay Cycle
    private int groupBudget;
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
        lightManager = LightManager.Instance;
        lootValidator = OreCar.Instance;

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
        // Get train reference
        trainManager = TrainManager.Instance;

        // Move train to start
        trainManager.transform.position = LevelStartPositon.Instance.transform.position;
        trainManager.transform.rotation = LevelStartPositon.Instance.transform.rotation;
        trainManager.ResetCarPositions();

        // Spawn Players


        // Start quota
        profitQuota = startingQuota;
        daysRemainingInCycle = daysPerCycle;

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
    public Item[] CheckItems(Vector3 center, Quaternion rot, Vector3 size)
    {
        Collider[] colliders = Physics.OverlapBox(center, size / 2, rot);

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

        // Check loot validator for items
        Transform validator = lootValidator.transform;
        Item[] items = CheckItems(validator.position + lootValidator.Center, validator.rotation, lootValidator.Size);
        foreach (Item i in items)
        {
            totalProfit += i.value;
        }

        AddToBudget(totalProfit);

        bool isSuccessful = totalProfit > profitQuota;

        if (isSuccessful)
        {
            // Continue cycle
            currentCycle++;
            profitQuota = startingQuota * currentCycle;
            daysRemainingInCycle = daysPerCycle;

            Debug.Log("CAN CONTINUE");
        }
        else
        {
            // Lose game
            Debug.Log("LOSER");
        }
    }

    #endregion

    #region Group Budget

    public int GetGroupBudget()
    {
        return groupBudget;
    }

    public void SetBudget(int budget)
    {
        groupBudget = budget;
        Debug.Log($"New group budget: {groupBudget}");

        // TODO
        // Update UI
    }

    public void AddToBudget(int budget)
    {
        SetBudget(groupBudget + budget);
    }

    public void RemoveFromBudget(int budget)
    {
        SetBudget(groupBudget - budget);
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

        if (timer >= secPerMin)
        {
            min++;

            // Check if the minutes exceeds max minutes in an hour
            if (min >= maxMin)
            {
                min = 0;
                hr++;

                OnHourAdvance?.Invoke(hr);

                // Check if the hours exceeds max hours in a day
                if (hr >= maxHr)
                {
                    hr = 0;
                    day++;

                    daysRemainingInCycle--;

                    isMorning = GetIsMorning(); // Check if it is morning
                    AdvanceWeekDay();

                    // Check if the day exceeds max days in month
                    if (day > maxDay)
                    {
                        day = 1;
                        month++;

                        // Check if the month exceeds max months in year
                        if (month > maxMonth)
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

        // Calculate normalized time
        float normalizedTime = (hr + (min / 60f)) / 24f;

        // Update Lighting
        if (lightManager != null)
            lightManager.EvaluateTime(normalizedTime);
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
        string displayDate = $"{weekDay} {day}";
        displayTime = $"{displayHour}:{min:D2} {(isMorning ? "AM" : "PM")}";
        string displayDaysRemaining = $"Days remaining: {daysRemainingInCycle}";
        PlayerUIInstance.HUDManager.Clock.SetValue(displayDate, displayTime, displayDaysRemaining);
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