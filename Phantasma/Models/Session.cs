using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Input;
using Avalonia.Threading;

namespace Phantasma.Models;

public class Session
{
    private bool isRunning;
    private Place currentPlace;
    private Character playerCharacter;
    private Map map;
    private DispatcherTimer gameTimer;
        
    public Place CurrentPlace => currentPlace;
    public Character Player => playerCharacter;
    public Map Map => map;
    public bool IsRunning => isRunning;
        
    // Singleton for Global Access
    private static Session current;
    public static Session Current => current;
        
    public Session()//Mode mode = Mode.Normal)
    {
        //RunMode = mode;
        current = this;
            
        // Create a simple test map.
        currentPlace = new Place();
        currentPlace.GenerateTestMap();
        
        // Create map rendering system
        map = new Map(800, 600, 32);
        map.SetPlace(currentPlace);
            
        // Create the player character.
        CreatePlayer();
        
        // Attach camera to player.
        if (playerCharacter != null && map != null)
        {
            map.AttachCamera(playerCharacter);
        }
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
        // Update camera to follow player.
        map?.UpdateCamera();
        
        // Game update logic will go here.
        // For now just ensure player has action points.
        if (playerCharacter != null && playerCharacter.IsTurnEnded())
        {
            playerCharacter.StartTurn();
        }
    }
        
    public void Quit()
    {
        Stop();
    }
    
    /// <summary>
    /// Handle player movement input
    /// Based on Nazghul's input handling approach
    /// </summary>
    public void HandlePlayerMove(Key key)
    {
        if (playerCharacter == null || playerCharacter.IsTurnEnded())
            return;
    
        // Convert key to movement direction.
        int dx = 0, dy = 0;
    
        switch (key)
        {
            case Key.Up:
            case Key.NumPad8:
                dy = -1;
                break;
            case Key.Down:
            case Key.NumPad2:
                dy = 1;
                break;
            case Key.Left:
            case Key.NumPad4:
                dx = -1;
                break;
            case Key.Right:
            case Key.NumPad6:
                dx = 1;
                break;
            case Key.NumPad7:
                dx = -1; dy = -1;  // Northwest
                break;
            case Key.NumPad9:
                dx = 1; dy = -1;   // Northeast
                break;
            case Key.NumPad1:
                dx = -1; dy = 1;   // Southwest
                break;
            case Key.NumPad3:
                dx = 1; dy = 1;    // Southeast
                break;
            case Key.NumPad5:
                // Wait/Rest - no movement
                break;
            default:
                return; // Unhandled key
        }
    
        // Execute the movement.
        if (dx == 0 && dy == 0)
        {
            // Wait action.
            playerCharacter.DecreaseActionPoints(1);
            Console.WriteLine("Player waits.");
        }
        else if (playerCharacter.Move(dx, dy))
        {
            playerCharacter.DecreaseActionPoints(1);
            Console.WriteLine($"Player moved to ({playerCharacter.GetX()}, {playerCharacter.GetY()}) - AP: {playerCharacter.ActionPoints}");
        }
        else
        {
            Console.WriteLine("Can't move there!");
        }
    
        // Check if turn ended.
        if (playerCharacter.ActionPoints <= 0)
        {
            playerCharacter.EndTurn();
            Console.WriteLine("Player turn ended.");
        }
    }
    
    /// <summary>
    /// Handle player movement with direct direction values.
    /// </summary>
    public void HandlePlayerMove(int dx, int dy)
    {
        if (playerCharacter == null || playerCharacter.IsTurnEnded())
            return;
    
        // Execute the movement.
        if (dx == 0 && dy == 0)
        {
            // Wait action.
            playerCharacter.DecreaseActionPoints(1);
            Console.WriteLine("Player waits.");
        }
        else if (playerCharacter.Move(dx, dy))
        {
            playerCharacter.DecreaseActionPoints(1);
            Console.WriteLine($"Player moved to ({playerCharacter.GetX()}, {playerCharacter.GetY()}) - AP: {playerCharacter.ActionPoints}");
        }
        else
        {
            Console.WriteLine("Can't move there!");
        }
    
        // Check if turn ended.
        if (playerCharacter.ActionPoints <= 0)
        {
            playerCharacter.EndTurn();
            Console.WriteLine("Player turn ended.");
        }
    }
}
