using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;

namespace Phantasma.Models;

public class Session
{
    private bool isRunning;
    private Place currentPlace;
    private Character playerCharacter;
    private DispatcherTimer gameTimer;
        
    public Place CurrentPlace => currentPlace;
    public Character Player => playerCharacter;
    public bool IsRunning => isRunning;
        
    // Singleton for Global Access
    private static Session current;
    public static Session Current => current;
        
    public Session()
    {
        current = this;
            
        // Create a simple test map.
        currentPlace = new Place();
        currentPlace.GenerateTestMap();
            
        // Create the player character.
        CreatePlayer();
    }
        
    private void CreatePlayer()
    {
        // Create a test player.
        playerCharacter = Character.CreateTestPlayer();
            
        // Find a good starting position (clear grass).
        int startX = 10;
        int startY = 10;
            
        // Make sure starting position is passable.
        for (int y = 0; y < currentPlace.Height; y++)
        {
            for (int x = 0; x < currentPlace.Width; x++)
            {
                var terrain = currentPlace.GetTerrainAt(x, y);
                if (terrain != null && terrain.Passable)
                {
                    startX = x;
                    startY = y;
                    goto FoundSpot; // Break out of nested loop
                }
            }
        }
        FoundSpot:
            
        // Place the player on the map.
        currentPlace.AddObject(playerCharacter, startX, startY);
            
        Console.WriteLine($"Player '{playerCharacter.GetName()}' placed at ({startX}, {startY})");
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
        // For now just ensure player has action points
        if (playerCharacter != null && playerCharacter.IsTurnEnded())
        {
            playerCharacter.StartTurn();
        }
    }
        
    public void Quit()
    {
        Stop();
    }
}
