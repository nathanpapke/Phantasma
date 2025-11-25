using System;
using System.Collections.Generic;

namespace Phantasma.Models;

/// <summary>
/// Activities that NPCs can perform during their daily routine.
/// </summary>
public enum Activity
{
    Idle = 0,
    Working = 1,
    Sleeping = 2,
    Commuting = 3,
    Eating = 4,
    Drunk = 5,
    Wandering = 6  // Additional activity for free roaming
}

/// <summary>
/// An appointment represents a specific time and location where an NPC should be.
/// </summary>
public class Appointment
{
    public int Hour { get; set; }      // Hour of day (0-23)
    public int Minute { get; set; }    // Minute (0-59)
    public int X { get; set; }         // Target X coordinate
    public int Y { get; set; }         // Target Y coordinate
    public int Width { get; set; }     // Width of target area
    public int Height { get; set; }    // Height of target area
    public Activity Activity { get; set; }
    
    public Appointment(int hour, int minute, int x, int y, int w, int h, Activity activity)
    {
        Hour = hour;
        Minute = minute;
        X = x;
        Y = y;
        Width = w;
        Height = h;
        Activity = activity;
    }
    
    /// <summary>
    /// Check if a position is within this appointment's area.
    /// </summary>
    public bool ContainsPoint(int px, int py)
    {
        return px >= X && px < X + Width &&
               py >= Y && py < Y + Height;
    }
    
    /// <summary>
    /// Get a random position within the appointment area.
    /// </summary>
    public (int x, int y) GetRandomPosition()
    {
        var random = new Random();
        return (X + random.Next(Width), Y + random.Next(Height));
    }
}

/// <summary>
/// Schedule defines an NPC's daily routine with appointments at specific times.
/// </summary>
public class Schedule
{
    public string Tag { get; set; }
    public List<Appointment> Appointments { get; private set; }
    
    public Schedule(string tag)
    {
        Tag = tag;
        Appointments = new List<Appointment>();
    }
    
    /// <summary>
    /// Add an appointment to the schedule.
    /// </summary>
    public void AddAppointment(int hour, int minute, int x, int y, int w, int h, Activity activity)
    {
        var appt = new Appointment(hour, minute, x, y, w, h, activity);
        Appointments.Add(appt);
    }
    
    /// <summary>
    /// Get the current appointment based on game time.
    /// Returns the appointment whose time has passed but whose next appointment hasn't started yet.
    /// </summary>
    public int GetCurrentAppointmentIndex(int currentHour, int currentMinute)
    {
        if (Appointments.Count == 0)
            return -1;
        
        int currentTime = currentHour * 60 + currentMinute;
        int currentApptIndex = -1;
        
        // Find the latest appointment that has already started.
        for (int i = 0; i < Appointments.Count; i++)
        {
            int apptTime = Appointments[i].Hour * 60 + Appointments[i].Minute;
            
            if (apptTime <= currentTime)
            {
                currentApptIndex = i;
            }
            else
            {
                break; // We've found the first future appointment.
            }
        }
        
        // If no appointment has started yet, use the last one (wrapping from previous day).
        if (currentApptIndex == -1)
        {
            currentApptIndex = Appointments.Count - 1;
        }
        
        return currentApptIndex;
    }
    
    /// <summary>
    /// Get the current appointment based on game time.
    /// </summary>
    public Appointment? GetCurrentAppointment(int currentHour, int currentMinute)
    {
        int index = GetCurrentAppointmentIndex(currentHour, currentMinute);
        if (index >= 0 && index < Appointments.Count)
            return Appointments[index];
        return null;
    }
    
    /// <summary>
    /// Create a simple wandering schedule (no fixed appointments).
    /// </summary>
    public static Schedule CreateWanderingSchedule(string tag)
    {
        return new Schedule($"{tag}_wandering");
    }
    
    /// <summary>
    /// Create a typical townsperson schedule.
    /// </summary>
    public static Schedule CreateTownspersonSchedule(string tag, int homeX, int homeY, int workX, int workY)
    {
        var schedule = new Schedule(tag);
        
        // Sleep at home (midnight to 6am)
        schedule.AddAppointment(0, 0, homeX, homeY, 3, 3, Activity.Sleeping);
        
        // Commute to work (6am)
        schedule.AddAppointment(6, 0, workX, workY, 1, 1, Activity.Commuting);
        
        // Work (8am to 5pm)
        schedule.AddAppointment(8, 0, workX, workY, 5, 5, Activity.Working);
        
        // Commute home (5pm)
        schedule.AddAppointment(17, 0, homeX, homeY, 1, 1, Activity.Commuting);
        
        // Eat dinner (6pm)
        schedule.AddAppointment(18, 0, homeX, homeY, 3, 3, Activity.Eating);
        
        // Idle/relaxing at home (7pm)
        schedule.AddAppointment(19, 0, homeX, homeY, 3, 3, Activity.Idle);
        
        return schedule;
    }
}
