using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Input;
using Avalonia.Threading;
using IronScheme;

namespace Phantasma.Models;

/// <summary>
/// A Session represents one instance of the game world.
/// Multiple Sessions can exist simultaneously:
/// - MainSession: The real game the user is playing
/// - AgentSessions: Temporary simulations for AI agents
/// 
/// Sessions are managed by Phantasma and do NOT reference Kernel.
/// This avoids circular dependencies and allows clean multi-session support.
/// </summary>
public class Session
{
    private bool isRunning;
    private Place currentPlace;
    private Character playerCharacter;
    private Party playerParty;
    private Map map;
    private DispatcherTimer gameTimer;
    private Status status;
    
    // Clock, Sky, and Wind System
    private readonly Clock clock = new();
    private readonly Sky sky = new();
    private readonly Wind wind = new();
    private int timeAcceleration = 1;  // Time speed multiplier
    
    /// <summary>
    /// Current combat state for this session.
    /// </summary>
    private CombatState combatState = CombatState.Done;
    
    // Targeting
    public bool IsTargeting { get; private set; }
    public int TargetOriginX { get; private set; }
    public int TargetOriginY { get; private set; }
    public int TargetRange { get; private set; }
    public int TargetX { get; private set; }
    public int TargetY { get; private set; }
    
    /// <summary>
    /// Key handler stack.
    /// Top handler receives all key events.
    /// </summary>
    private readonly Stack<IKeyHandler> keyHandlers = new();

    
    // ===================================================================
    // UI EVENTS - For displaying messages to user
    // ===================================================================
    
    /// <summary>
    /// Fired when a message should be displayed in the command window.
    /// </summary>
    public event Action<string> MessageDisplayed;
    
    /// <summary>
    /// Fired when the command prompt changes (e.g., "Talk-", "Ready-").
    /// </summary>
    public event Action<string> PromptChanged;
    
    /// <summary>
    /// Fired when a message should be displayed in the console (multi-line scrollable).
    /// Used for NPC dialog, combat log, game messages.
    /// </summary>
    public event Action<string> ConsoleMessage;

    /// <summary>
    /// Fired when command input text changes (user typing).
    /// </summary>
    public event Action<string> CommandInputChanged;
    
    // ===================================================================
    // SAVE/LOAD REGISTRY
    // ===================================================================
    
    /// <summary>
    /// List of objects registered for save/load/destroy.
    /// Order is preserved - objects are saved in the order they were registered.
    /// </summary>
    private readonly List<SaveEntry> saveEntries = new();
    
    /// <summary>
    /// Session ID increments with each save.
    /// Objects use this to detect if they've already been saved this session.
    /// </summary>
    public int SessionId { get; private set; } = 0;
    
    // ===================================================================
    // PUBLIC PROPERTIES
    // ===================================================================
    
    public Place CurrentPlace => currentPlace;
    public Character Player => playerCharacter;
    public Party Party => playerParty;
    public Map Map => map;
    public bool IsRunning => isRunning;
    public Status Status => status;
    public Clock Clock => clock;
    public Sky Sky => sky;
    public Wind Wind => wind;
    public int GameClockMinutes => (int)clock.TimeOfDay;
    public int TimeAcceleration { get => timeAcceleration; set => timeAcceleration = Math.Max(1, value); }
    
    /// <summary>
    /// Get the current (top) key handler, or null if stack is empty.
    /// </summary>
    public IKeyHandler CurrentKeyHandler => 
        keyHandlers.Count > 0 ? keyHandlers.Peek() : null;

    /// <summary>
    /// True if there's an active key handler (not in normal input mode).
    /// </summary>
    public bool HasActiveKeyHandler => keyHandlers.Count > 0;
    
    /// <summary>
    /// Gets the current combat state.
    /// </summary>
    public CombatState CombatState => combatState;
    
    // ===================================================================
    // INITIALIZATION
    // ===================================================================
    
    /// <summary>
    /// Create a session with the given Place and player Character.
    /// This works for both main game sessions and agent simulation sessions.
    /// </summary>
    /// <param name="place">The Place (map) for this session. If null, creates a test map.</param>
    /// <param name="player">The player Character. If null, creates a test player.</param>
    public Session(Place place = null, Character player = null)
    {
        // Use provided objects or create test fallbacks.
        if (place != null)
        {
            currentPlace = place;
            Console.WriteLine($"[Session] Using provided Place: {currentPlace.Width}x{currentPlace.Height}");
        }
        else
        {
            // Fallback: create test map.
            currentPlace = new Place();
            currentPlace.GenerateTestMap();
            Console.WriteLine("[Session] No Place provided, created test map");
        }
        
        if (player != null)
        {
            playerCharacter = player;
            Console.WriteLine($"[Session] Using provided player: {playerCharacter.GetName()} at ({playerCharacter.GetX()}, {playerCharacter.GetY()})");
        }
        else
        {
            // Fallback: create test player.
            CreatePlayer();  // should be? playerCharacter = 
            //playerCharacter = new Character();
            //playerCharacter.GenerateTestPlayer();
            Console.WriteLine("[Session] No player provided, created test player");
        }
        
        // Create party and add player.
        playerParty = new Party();
        playerParty.AddMember(playerCharacter);
        
        // Create map rendering system.
        map = new Map(800, 600, 32);
        map.SetPlace(currentPlace);
            
        // Attach camera to player.
        if (playerCharacter != null && map != null)
        {
            map.AttachCamera(playerCharacter);
        }
        
        // Initialize time systems
        clock.Reset();
        sky.Init(clock);
        wind.Init();
    }
        
    private void CreatePlayer()
    {
        // Create the player party FIRST.
        playerParty = new Party();
        
        // Create a test player.
        playerCharacter = Character.CreateTestPlayer();
    
        // Add player to the party
        playerParty.AddMember(playerCharacter);
            
        // Find a good starting position (clear grass).
        int startX = 10;
        int startY = 10;
            
        // Make sure starting position is passable.
        for (int y = 0; y < currentPlace.Height; y++)
        {
            for (int x = 0; x < currentPlace.Width; x++)
            {
                var terrain = currentPlace.GetTerrain(x, y);
                if (terrain != null && terrain.IsPassable)
                {
                    startX = x;
                    startY = y;
                    goto FoundSpot; // Break out of nested loop
                }
            }
        }
        FoundSpot:
            
        // Place the party on the map.
        currentPlace.AddObject(playerCharacter, startX, startY);
    
        // Also track individual player position.
        playerCharacter.SetPosition(currentPlace, startX, startY);
            
        Console.WriteLine($"Party with player '{playerCharacter.GetName()}' placed at ({startX}, {startY})");
    }
    
    // ===================================================================
    // UI MESSAGE HELPERS
    // ===================================================================
    
    /// <summary>
    /// Display a message to the user in the command window.
    /// </summary>
    private void ShowMessage(string message)
    {
        MessageDisplayed?.Invoke(message);
        Console.WriteLine(message); // Also log to console.
    }
    
    /// <summary>
    /// Set the command prompt (e.g., "Talk-", "Ready-").
    /// </summary>
    private void SetPrompt(string prompt)
    {
        PromptChanged?.Invoke(prompt);
    }
    
    /// <summary>
    /// Log a message to the console view (multi-line, persistent).
    /// Use this for NPC dialog, combat results, game events.
    /// </summary>
    public void LogMessage(string format, params object[] args)
    {
        string message = args.Length > 0 ? string.Format(format, args) : format;
        ConsoleMessage?.Invoke(message);
        Console.WriteLine($"[LOG] {message}"); // Also log to debug console.
    }
    
    // ===================================================================
    // SAVE/LOAD REGISTRATION
    // ===================================================================
    
    /// <summary>
    /// Register an object for save/load/destroy tracking.
    /// 
    /// Objects are saved in the order they are registered.
    /// This is important for dependencies (e.g., if Place A references Place B).
    /// </summary>
    /// <param name="obj">The object to track</param>
    /// <param name="destructor">Optional callback when session is destroyed</param>
    /// <param name="saveAction">Optional callback when session is saved</param>
    /// <param name="postLoadAction">Optional callback after session is loaded</param>
    public void RegisterSaveableObject(
        object obj,
        Action<object>? destructor = null,
        Action<object, SaveWriter>? saveAction = null,
        Action<object>? postLoadAction = null)
    {
        var entry = new SaveEntry(obj, destructor, saveAction, postLoadAction);
        saveEntries.Add(entry);
        
        Console.WriteLine($"Registered saveable object: {obj.GetType().Name}");
    }
    
    /// <summary>
    /// Save the session to a Scheme file.
    /// </summary>
    public void Save(string filename)
    {
        var savePath = Path.Combine(
            Phantasma.Configuration["saved-games-dirname"],
            filename
        );
        
        try
        {
            using var writer = new SaveWriter(savePath);
            
            // Increment session ID (prevents duplicate saves).
            SessionId++;
            
            // Write header.
            writer.WriteComment($"{filename} -- a Phantasma session file");  // Might change to Nazghul session file
            writer.WriteComment($"Created: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            writer.WriteComment("Load the standard definitions file.");
            writer.WriteLine("(load \"naz.scm\")");
            writer.WriteLine("");
            
            // Save all registered objects.
            Console.WriteLine($"Saving {saveEntries.Count} registered objects...");
            foreach (var entry in saveEntries)
            {
                if (entry.SaveAction != null)
                {
                    try
                    {
                        entry.SaveAction(entry.Object, writer);
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"Error saving {entry.Object.GetType().Name}: {ex.Message}");
                        // Continue with other objects.
                    }
                }
            }
            
            // Save session-specific state.
            writer.WriteComment("--------------");
            writer.WriteComment("Miscellaneous");
            writer.WriteComment("--------------");
            SaveSessionState(writer);
            
            ShowMessage($"Game saved");
            Console.WriteLine($"Session saved to: {savePath}");
        }
        catch (Exception ex)
        {
            ShowMessage("Save failed!");
            Console.Error.WriteLine($"Failed to save session: {ex.Message}");
            throw;
        }
    }
    
    /// <summary>
    /// Load a session from a Scheme file.
    /// </summary>
    public void Load(string filename)
    {
        var savePath = Path.Combine(
            Phantasma.Configuration["saved-games-dirname"],
            filename
        );
        
        try
        {
            // Clear current state.
            ClearSaveRegistry();
            SessionId++;
            
            Console.WriteLine($"Loading session from: {savePath}");
            
            // Load via Scheme evaluation.
            // The Scheme file will call kern-mk-* functions which will
            // register objects with this session.
            Phantasma.Kernel.LoadSchemeFile(savePath);
            
            // Post-load initialization
            Console.WriteLine($"Running post-load initialization for {saveEntries.Count} objects...");
            foreach (var entry in saveEntries)
            {
                entry.PostLoadAction?.Invoke(entry.Object);
            }
            
            ShowMessage($"Game loaded.");
            Console.WriteLine($"Session loaded successfully from: {savePath}");
        }
        catch (Exception ex)
        {
            ShowMessage("Load failed!");
            Console.Error.WriteLine($"Failed to load session: {ex.Message}");
            throw;
        }
    }
    
    /// <summary>
    /// Save session-specific state (clock, flags, etc.).
    /// </summary>
    private void SaveSessionState(SaveWriter writer)
    {
        writer.WriteComment("Session State");
    
        // Save clock using full 6-parameter format.
        clock.Save(writer);
    
        // Save time acceleration.
        writer.WriteLine($"(kern-set-time-accel {timeAcceleration})");
    
        // Save wind.
        wind.Save(writer);
    
        // Save sky (astral bodies).
        sky.Save(writer);
        
        // Save player status effects if active.
        if (playerCharacter != null)
        {
            if (playerCharacter.RevealDuration > 0)
                writer.WriteLine($"(kern-add-reveal (kern-get-player) {playerCharacter.RevealDuration})");
            
            if (playerCharacter.QuickenDuration > 0)
                writer.WriteLine($"(kern-add-quicken (kern-get-player) {playerCharacter.QuickenDuration})");
            
            if (playerCharacter.TimeStopDuration > 0)
                writer.WriteLine($"(kern-add-time-stop (kern-get-player) {playerCharacter.TimeStopDuration})");
            
            if (playerCharacter.MagicNegatedDuration > 0)
                writer.WriteLine($"(kern-add-magic-negated (kern-get-player) {playerCharacter.MagicNegatedDuration})");
            
            if (playerCharacter.XrayVisionDuration > 0)
                writer.WriteLine($"(kern-add-xray-vision (kern-get-player) {playerCharacter.XrayVisionDuration})");
        }
        
        writer.WriteLine("");
    }
    
    /// <summary>
    /// Clear the save registry and call destructors.
    /// Called before loading a new session or when disposing.
    /// </summary>
    private void ClearSaveRegistry()
    {
        // Call destructors in reverse order (like C++ destructors).
        for (int i = saveEntries.Count - 1; i >= 0; i--)
        {
            var entry = saveEntries[i];
            try
            {
                entry.Destructor?.Invoke(entry.Object);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error in destructor for {entry.Object.GetType().Name}: {ex.Message}");
                // Continue with other destructors.
            }
        }
        
        saveEntries.Clear();
        Console.WriteLine("Save registry cleared.");
    }
    
    /// <summary>
    /// Dispose of the session and clean up all registered objects.
    /// </summary>
    public void Dispose()
    {
        // Clean up sky (deletes astral bodies).
        sky.EndSession();
        
        ClearSaveRegistry();
    }
    
    // ===================================================================
    // GAME LOOP
    // ===================================================================
    
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
        // Check if player moved to a different place.
        var playerPlace = playerCharacter?.Position?.Place;
        if (playerPlace != null && playerPlace != currentPlace)
        {
            Console.WriteLine($"[Session] Player moved to: {playerPlace.Name}");
            currentPlace = playerPlace;
            map?.SetPlace(currentPlace);
        }
        
        // Update camera to follow player.
        map?.UpdateCamera();
    
        // Advance time systems (scaled by time acceleration)
        for (int i = 0; i < timeAcceleration; i++)
        {
            clock.Advance(1);
            wind.AdvanceTurns();
        }
    
        // Update sky (only needs visual update once per tick, not per accel)
        bool skyVisible = currentPlace != null && !currentPlace.Underground;
        sky.Advance(skyVisible);
        
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
    
    // ===================================================================
    // INPUT HANDLING
    // ===================================================================
    
    /// <summary>
    /// Handle player movement input.
    /// Based on Nazghul's input handling approach.
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
            ShowMessage("Player waits.");
        }
        else if (playerCharacter.Move(dx, dy))
        {
            playerCharacter.DecreaseActionPoints(1);
            ShowMessage($"Player moved to ({playerCharacter.GetX()}, {playerCharacter.GetY()}) - AP: {playerCharacter.ActionPoints}");
        }
        else
        {
            ShowMessage("Can't move there!");
        }
    
        // Check if turn ended.
        if (playerCharacter.ActionPoints <= 0)
        {
            playerCharacter.EndTurn();
            ShowMessage("Player turn ended.");
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
            ShowMessage("Player waits.");
        }
        else if (playerCharacter.Move(dx, dy))
        {
            playerCharacter.DecreaseActionPoints(1);
            ShowMessage($"Player moved to ({playerCharacter.GetX()}, {playerCharacter.GetY()}) - AP: {playerCharacter.ActionPoints}");
        }
        else
        {
            ShowMessage("Can't move there!");
        }
    
        // Check if turn ended.
        if (playerCharacter.ActionPoints <= 0)
        {
            playerCharacter.EndTurn();
            ShowMessage("Player turn ended.");
        }
    }
    
    /// <summary>
    /// Handle keyboard input for game actions.
    /// </summary>
    public void HandleKeyDown(Key key)
    {
        // Check for save/load shortcuts.
        if (key == Key.F5)
        {
            // Quick Save
            Save("quicksave.scm");
            // TODO: Show notification "Quick Saved"
            return;
        }
        
        if (key == Key.F9)
        {
            // Quick Load
            var quickSavePath = Path.Combine(
                Phantasma.Configuration["saved-games-dirname"],
                "quicksave.scm"
            );
            
            if (File.Exists(quickSavePath))
            {
                Load("quicksave.scm");
                ShowMessage("Quick save loaded.");
            }
            else
            {
                ShowMessage("No quick save found.");
            }
            return;
        }
        
        // Other Game Keys
        switch (key)
        {
            case Key.G:
                // Get/pickup item.
                // TODO: Implement pickup
                break;
            case Key.I:
                // Show inventory.
                // TODO: Implement inventory UI
                break;
            case Key.Z:
                // Show character stats (Ztats)
                if (status != null)
                {
                    status.SetMode(StatusMode.Ztats);
                    status.SelectedCharacterIndex = 0;
                    ShowMessage("Stats view.");
                }
                break;
            case Key.Escape:
                // Toggle back to party view.
                if (status != null && status.Mode != StatusMode.ShowParty)
                {
                    status.SetMode(StatusMode.ShowParty);
                    ShowMessage("");
                }
                break;
        }
    }

    /// <summary>
    /// Push a key handler onto the stack.
    /// </summary>
    public void PushKeyHandler(IKeyHandler handler)
    {
        if (handler == null)
            throw new ArgumentNullException(nameof(handler));
    
        keyHandlers.Push(handler);
        Console.WriteLine($"[Session] Pushed key handler: {handler.GetType().Name} (stack depth: {keyHandlers.Count})");
    }

    /// <summary>
    /// Pop the top key handler from the stack.
    /// </summary>
    public void PopKeyHandler()
    {
        if (keyHandlers.Count > 0)
        {
            var handler = keyHandlers.Pop();
            Console.WriteLine($"[Session] Popped key handler: {handler.GetType().Name} (stack depth: {keyHandlers.Count})");
        }
    }

    /// <summary>
    /// Update the command input display (user's typed text).
    /// </summary>
    public void UpdateCommandInput(string text)
    {
        CommandInputChanged?.Invoke(text);
    }

    /// <summary>
    /// Set the command prompt text (e.g., "Say: ", "Direction?").
    /// </summary>
    public void SetCommandPrompt(string prompt)
    {
        Console.WriteLine($"[DEBUG] SetCommandPrompt called with: '{prompt}'");
        Console.WriteLine($"[DEBUG] PromptChanged has subscribers: {PromptChanged != null}");
        PromptChanged?.Invoke(prompt);
    }

    // ===================================================================
    // CONVERSATION HANDLING
    // ===================================================================
    
    /// <summary>
    /// Start a conversation with an NPC at the given location.
    /// </summary>
    public void StartConversation(int targetX, int targetY)
    {
        if (playerCharacter == null || currentPlace == null)
            return;
        
        // Find NPC at target location.
        var target = currentPlace.GetBeingAt(targetX, targetY);
        
        if (target == null)
        {
            LogMessage("Nobody there!");
            return;
        }
        
        // Check if target is a Character with a conversation.
        if (target is Character npc)
        {
            string personName = (npc.GetName() != "") ? npc.GetName() : "person";
            if (npc.Conversation == null)
            {
                LogMessage($"No response from {personName}.");
                return;
            }
            
            // Start conversation.
            LogMessage($"Talking to {personName}...");
            Conversation.Enter(this, npc, playerCharacter, npc.Conversation);
        }
        else
        {
            LogMessage("That's not a person!");
        }
    }

    // ===================================================================
    // TARGET HANDLING
    // ===================================================================
    
    /// <summary>
    /// Begin targeting mode with the crosshair.
    /// When complete, calls onComplete with (targetX, targetY, cancelled).
    /// </summary>
    public void BeginTargeting(int originX, int originY, int range, 
        int startX, int startY,
        Action<int, int, bool> onComplete)
    {
        IsTargeting = true;
        TargetOriginX = originX;
        TargetOriginY = originY;
        TargetRange = range;
        TargetX = startX;
        TargetY = startY;
    
        // Create and push handler.
        var handler = new TargetingKeyHandler(this, originX, originY, range, startX, startY);
    
        handler.OnComplete = (success, targetX, targetY) =>
        {
            IsTargeting = false;
            PopKeyHandler();
            SetCommandPrompt("");
            onComplete(targetX, targetY, !success);
        };
    
        PushKeyHandler(handler);
        SetCommandPrompt("<target>");
    }

    /// <summary>
    /// Move the targeting cursor (called by TargetingKeyHandler).
    /// </summary>
    public void MoveTarget(int dx, int dy)
    {
        if (!IsTargeting) return;
    
        int newX = TargetX + dx;
        int newY = TargetY + dy;
    
        // TODO: Validate range, bounds, etc.
    
        TargetX = newX;
        TargetY = newY;
    }
    
    /// <summary>
    /// Sets the combat state and plays appropriate sound.
    /// </summary>
    public void SetCombatState(CombatState newState)
    {
        if (combatState == newState)
            return;
        
        var oldState = combatState;
        combatState = newState;
        
        // Get combat sounds from Phantasma (game-wide config).
        var sounds = Phantasma.CombatSounds;
        
        // Play sounds based on transition.
        switch (oldState)
        {
            case CombatState.Done:
                if (newState == CombatState.Fighting)
                {
                    ShowMessage("*** COMBAT ***");
                    sounds.PlayEnterSound();
                }
                else if (newState == CombatState.Camping)
                {
                    ShowMessage("*** CAMPING ***");
                }
                break;
                
            case CombatState.Fighting:
                if (newState == CombatState.Looting)
                {
                    ShowMessage("*** VICTORY ***");
                    sounds.PlayVictorySound();
                }
                else if (newState == CombatState.Done)
                {
                    ShowMessage("*** DEFEAT ***");
                    sounds.PlayDefeatSound();
                }
                break;
                
            case CombatState.Looting:
                if (newState == CombatState.Fighting)
                {
                    // Hostiles entered during looting.
                    sounds.PlayEnterSound();
                }
                break;
                
            case CombatState.Camping:
                if (newState == CombatState.Fighting)
                {
                    // Ambush!
                    sounds.PlayEnterSound();
                }
                break;
        }
    }
}
