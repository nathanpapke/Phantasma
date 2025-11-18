using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;

namespace Phantasma.Models;

public class Session
{
    private bool isRunning;
    private Place currentPlace;
    private DispatcherTimer gameTimer;
        
    public Place CurrentPlace => currentPlace;
    public bool IsRunning => isRunning;
        
    public Session()
    {
        // Create a simple test map.
        currentPlace = new Place();
        currentPlace.GenerateTestMap();
    }
        
    public void Start()
    {
        if (isRunning) return;
            
        isRunning = true;
            
        // Set up game timer for updates.
        gameTimer = new DispatcherTimer();
        gameTimer.Interval = TimeSpan.FromMilliseconds(Common.MS_PER_TICK);
        gameTimer.Tick += GameTick;
        gameTimer.Start();
            
        Console.WriteLine("Session started.");
    }
        
    public void Stop()
    {
        isRunning = false;
        gameTimer?.Stop();
        Console.WriteLine("Session stopped.");
    }
        
    private void GameTick(object sender, EventArgs e)
    {
        // Update game state.
        Update();
    }
        
    private void Update()
    {
        // Game update logic will go here.
        // For now, just keep the game running.
    }
        
    public void Quit()
    {
        Stop();
    }
}
