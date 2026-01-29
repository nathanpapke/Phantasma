using System;

namespace Phantasma.Models;

/// <summary>
/// Tracks game time with year, month, week, day, hour, minute, and tick.
/// </summary>
public class Clock
{
    // ===================================================================
    // TIME STATE
    // ===================================================================
    
    public int Year { get; set; }
    public int Month { get; set; }      // 0-12 (13 months/year)
    public int Week { get; set; }       // 0-3 (4 weeks/month)
    public int Day { get; set; }        // 0-6 (7 days/week)
    public int Hour { get; set; }       // 0-23
    public int Min { get; set; }        // 0-59
    public int BaseTurn { get; set; }
    
    /// <summary>
    /// Total minutes elapsed since start of game.
    /// Used for astral body calculations.
    /// </summary>
    public uint TotalMinutes { get; private set; }
    
    /// <summary>
    /// Current tick within the minute (0 to TickToChangeTime-1).
    /// </summary>
    public int Tick { get; private set; }
    
    /// <summary>
    /// Number of ticks until the minute advances.
    /// Normally CLOCK_TICKS_PER_MINUTE (60).
    /// </summary>
    public int TickToChangeTime { get; set; }
    
    /// <summary>
    /// True if kern-set-clock has been called.
    /// </summary>
    public bool IsSet { get; set; }
    
    // ===================================================================
    // EVENTS
    // ===================================================================
    
    /// <summary>
    /// Fired when the clock time changes (minute rolls over).
    /// </summary>
    public event Action TimeChanged;
    
    /// <summary>
    /// Fired when a new hour begins.
    /// </summary>
    public event Action<int> HourChanged;
    
    /// <summary>
    /// Fired when a new day begins.
    /// </summary>
    public event Action DayChanged;
    
    // ===================================================================
    // CONSTRUCTOR
    // ===================================================================
    
    public Clock()
    {
        TickToChangeTime = Common.CLOCK_TICKS_PER_MINUTE;
    }
    
    // ===================================================================
    // PROPERTIES
    // ===================================================================
    
    /// <summary>
    /// Returns time of day in minutes (0-1439).
    /// </summary>
    public uint TimeOfDay => (uint)(Hour * Common.MINUTES_PER_HOUR + Min);
    
    /// <summary>
    /// Returns total minutes elapsed.
    /// </summary>
    public uint Time => TotalMinutes;
    
    /// <summary>
    /// True if it's exactly noon (12:00).
    /// </summary>
    public bool IsNoon => Hour == 12 && Min == 0;
    
    /// <summary>
    /// True if it's exactly midnight (0:00).
    /// </summary>
    public bool IsMidnight => Hour == 0 && Min == 0;
    
    /// <summary>
    /// True if it's daytime (between sunrise and sunset).
    /// </summary>
    public bool IsDaytime => Hour >= Common.SUNRISE_HOUR && Hour < Common.SUNSET_HOUR;
    
    /// <summary>
    /// True if it's nighttime.
    /// </summary>
    public bool IsNighttime => !IsDaytime;
    
    // ===================================================================
    // TIME ADVANCEMENT
    // ===================================================================
    
    /// <summary>
    /// Advance the clock by the specified number of ticks.
    /// </summary>
    public void Advance(int ticks)
    {
        while (ticks-- > 0)
        {
            Tick++;
            
            if (Tick >= TickToChangeTime)
            {
                TotalMinutes++;
                Tick = 0;
                TickToChangeTime = Common.CLOCK_TICKS_PER_MINUTE;
                
                int oldHour = Hour;
                
                Min++;
                if (Min >= Common.MINUTES_PER_HOUR)
                {
                    Hour++;
                    Min = 0;
                    
                    if (Hour >= Common.HOURS_PER_DAY)
                    {
                        Day++;
                        Hour = 0;
                        
                        if (Day >= Common.DAYS_PER_WEEK)
                        {
                            Week++;
                            Day = 0;
                            
                            if (Week >= Common.WEEKS_PER_MONTH)
                            {
                                Month++;
                                Week = 0;
                                
                                if (Month >= Common.MONTHS_PER_YEAR)
                                {
                                    Year++;
                                    Month = 0;
                                }
                            }
                        }
                        
                        DayChanged?.Invoke();
                    }
                    
                    HourChanged?.Invoke(Hour);
                }
                
                TimeChanged?.Invoke();
            }
        }
    }
    
    // ===================================================================
    // TIME STRING FORMATTING
    // ===================================================================
    
    /// <summary>
    /// Returns time as "HH:MMAM/PM" string.
    /// </summary>
    public string TimeHHMM
    {
        get
        {
            int hr = Hour;
            string ampm = hr >= 12 ? "PM" : "AM";
            hr = hr > 12 ? hr - 12 : hr;
            hr = hr == 0 ? 12 : hr;
            return $"{hr,2}:{Min:D2}{ampm}";
        }
    }
    
    /// <summary>
    /// Returns date as "YYYY/MM/DD" string.
    /// </summary>
    public string DateYYYYMMDD => $"{Year:D4}/{Month:D2}/{Day:D2}";
    
    /// <summary>
    /// Returns the name of the current month.
    /// </summary>
    public string MonthName => Month switch
    {
        0 => "1st Month",
        1 => "2nd Month",
        2 => "3rd Month",
        3 => "4th Month",
        4 => "5th Month",
        5 => "6th Month",
        6 => "7th Month",
        7 => "8th Month",
        8 => "9th Month",
        9 => "10th Month",
        10 => "11th Month",
        11 => "12th Month",
        12 => "13th Month",
        _ => "Unknown Month"
    };
    
    /// <summary>
    /// Returns the name of the current week.
    /// </summary>
    public string WeekName => Week switch
    {
        0 => "1st Week",
        1 => "2nd Week",
        2 => "3rd Week",
        3 => "4th Week",
        _ => "Unknown Week"
    };
    
    /// <summary>
    /// Returns the name of the current day.
    /// </summary>
    public string DayName => Day switch
    {
        0 => "1st Day",
        1 => "2nd Day",
        2 => "3rd Day",
        3 => "4th Day",
        4 => "5th Day",
        5 => "6th Day",
        6 => "7th Day",
        _ => "Unknown Day"
    };
    
    // ===================================================================
    // ALARM SYSTEM
    // ===================================================================
    
    /// <summary>
    /// Set an alarm to expire after the specified number of minutes.
    /// Returns the alarm value (total minutes when it expires).
    /// </summary>
    public uint SetAlarm(uint minutesFromNow)
    {
        return TotalMinutes + minutesFromNow;
    }
    
    /// <summary>
    /// Check if an alarm has expired.
    /// </summary>
    public bool IsAlarmExpired(uint alarm)
    {
        return TotalMinutes >= alarm;
    }
    
    // ===================================================================
    // INITIALIZATION
    // ===================================================================
    
    /// <summary>
    /// Set the clock to a specific time.
    /// Called by kern-set-clock.
    /// </summary>
    public bool Set(int year, int month, int week, int day, int hour, int min)
    {
        bool valid = true;
    
        // Validate and clamp ranges.
        if (month < 0 || month >= Common.MONTHS_PER_YEAR)
        {
            month = Math.Clamp(month, 0, Common.MONTHS_PER_YEAR - 1);
            valid = false;
        }
        if (week < 0 || week >= Common.WEEKS_PER_MONTH)
        {
            week = Math.Clamp(week, 0, Common.WEEKS_PER_MONTH - 1);
            valid = false;
        }
        if (day < 0 || day >= Common.DAYS_PER_WEEK)
        {
            day = Math.Clamp(day, 0, Common.DAYS_PER_WEEK - 1);
            valid = false;
        }
        if (hour < 0 || hour >= Common.HOURS_PER_DAY)
        {
            hour = Math.Clamp(hour, 0, Common.HOURS_PER_DAY - 1);
            valid = false;
        }
        if (min < 0 || min >= Common.MINUTES_PER_HOUR)
        {
            Console.WriteLine($"[Clock] Warning: min {min} out of range, clamping");
            min = Math.Clamp(min, 0, Common.MINUTES_PER_HOUR - 1);
            valid = false;
        }
    
        Year = Math.Max(0, year);
        Month = month;
        Week = week;
        Day = day;
        Hour = hour;
        Min = min;
    
        // Calculate total minutes
        TotalMinutes = (uint)(
            min +
            hour * Common.MINUTES_PER_HOUR +
            day * Common.MINUTES_PER_DAY +
            week * Common.MINUTES_PER_WEEK +
            month * Common.MINUTES_PER_MONTH +
            year * Common.MINUTES_PER_YEAR);
    
        TickToChangeTime = Common.CLOCK_TICKS_PER_MINUTE;
        Tick = 0;
        IsSet = true;
    
        Console.WriteLine($"[Clock] Set to Year {Year}, {MonthName}, {WeekName}, {DayName}, {TimeHHMM}");
    
        return valid;
    }
    
    /// <summary>
    /// Reset the clock to default state.
    /// </summary>
    public void Reset()
    {
        Year = 0;
        Month = 0;
        Week = 0;
        Day = 0;
        Hour = 0;
        Min = 0;
        TotalMinutes = 0;
        Tick = 0;
        TickToChangeTime = Common.CLOCK_TICKS_PER_MINUTE;
        IsSet = false;
    }
    
    // ===================================================================
    // SAVE/LOAD
    // ===================================================================
    
    /// <summary>
    /// Save clock state to a save writer.
    /// </summary>
    public void Save(SaveWriter writer)
    {
        writer.WriteLine($"(kern-set-clock {Year} {Month} {Week} {Day} {Hour} {Min})");
    }
    
    public override string ToString()
    {
        return $"{TimeHHMM} on {DayName}, {WeekName} of {MonthName}, Year {Year}";
    }
}
