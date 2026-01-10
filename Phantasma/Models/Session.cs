using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    private PassabilityTable passabilityTable;
    private DiplomacyTable diplomacyTable;
    private Map map;
    private DispatcherTimer gameTimer;
    private Status status;

    // Clock, Sky, and Wind System
    private readonly Clock clock = new();
    private readonly Sky sky = new();
    private readonly Wind wind = new();
    private int timeAcceleration = 1; // Time speed multiplier

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
    
    private object startProc;      // Scheme closure for game start
    private object campingProc;    // Scheme closure for camping callback
    
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
    
    public PassabilityTable PassabilityTable
    {
        get => passabilityTable;
        set => passabilityTable = value;
    }
    
    public DiplomacyTable DiplomacyTable
    {
        get => diplomacyTable;
        set => diplomacyTable = value;
    }
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
    
    /// <summary>
    /// The Scheme procedure to call when the game session starts.
    /// Called with the player party as an argument.
    /// </summary>
    public object StartProc => startProc;
    
    /// <summary>
    /// The Scheme procedure to call each turn while camping in the wilderness.
    /// </summary>
    public object CampingProc => campingProc;
    
    // ===================================================================
    // INITIALIZATION
    // ===================================================================

    /// <summary>
    /// Create a session with the given Place, player Character, and Party.
    /// This works for both main game sessions and agent simulation sessions.
    /// </summary>
    /// <param name="place">The Place (map) for this session. If null, creates a test map.</param>
    /// <param name="player">The player Character. If null, uses party leader or creates test player.</param>
    /// <param name="party">The player Party. If null, creates a new party with the player.</param>
    public Session(Place place = null, Character player = null, Party party = null)
    {
        Console.WriteLine($"[Session Constructor] place={place?.Name ?? "null"}, " +
                          $"player={player?.GetName() ?? "null"}, party={party?.Size ?? 0} members");
        
        // === PLACE SETUP ===
        if (place != null)
        {
            currentPlace = place;
            Console.WriteLine($"[Session] Using provided place: {place.Name}");
        }
        else
        {
            // Fallback: create test map.
            Console.WriteLine("[Session] No place provided, creating test map.");
            currentPlace = new Place();
            currentPlace.GenerateTestMap();
        }
        
        Console.WriteLine($"[Session] Place terrain check:");
        var testTerrain = currentPlace?.GetTerrain(0, 0);
        Console.WriteLine($"[Session]   Terrain at (0,0): {testTerrain?.Name ?? "NULL"}");
        Console.WriteLine($"[Session]   Has Sprite: {testTerrain?.Sprite != null}");
        Console.WriteLine($"[Session]   Has SourceImage: {testTerrain?.Sprite?.SourceImage != null}");
        
        // === PARTY AND PLAYER SETUP ===
        if (party != null && party.Size > 0)
        {
            // Use the loaded party.
            playerParty = party;
            
            // Use provided player or get from party leader.
            playerCharacter = player ?? party.GetLeader();
            
            Console.WriteLine($"[Session] Using loaded party with {party.Size} members, leader: " +
                              $"{playerCharacter?.GetName() ?? "none"}");
        }
        else if (player != null)
        {
            // Have a player but no party - create party for them.
            playerCharacter = player;
            playerParty = new Party();
            playerParty.AddMember(playerCharacter);
            Console.WriteLine($"[Session] Created party for single player: {player.GetName()}");
        }
        else
        {
            // No player and no party - create test player.
            Console.WriteLine("[Session] No player/party provided, creating test player.");
            CreatePlayer();
        }
        
        Console.WriteLine($"[Session] Player: {playerCharacter?.GetName() ?? "null"}");
        Console.WriteLine($"[Session] Player Position: {playerCharacter?.GetPosition()?.ToString() ?? "null"}");
        Console.WriteLine($"[Session] Player Place: {playerCharacter?.GetPosition()?.Place?.Name ?? "null"}");
        
        // === MAP RENDERING SYSTEM ===
        map = new Map(800, 600, 32);
        map.SetPlace(currentPlace);
        
        // Attach camera to player.
        if (playerCharacter != null && map != null)
        {
            map.AttachCamera(playerCharacter);
            Console.WriteLine($"[Session] Camera attached to {playerCharacter.GetName()}");
        }
        
        // === TIME SYSTEMS ===
        // Only reset clock if it hasn't been set by game data.
        if (!clock.IsSet)
        {
            // Default to 6 AM for better visibility during testing.
            clock.Set(0, 0, 0, 0, 6, 0);
            Console.WriteLine("[Session] Clock set to default 6:00 AM");
        }
        else
        {
            Console.WriteLine($"[Session] Clock already set to {clock.TimeHHMM}");
        }
        
        sky.Init(clock);
        wind.Init();
        
        Console.WriteLine($"[Session] Initialization complete.");
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
            
            // Load via Scheme evaluation.
            // The Scheme file will call kern-mk-* functions which will
            // register objects with this session.
            Phantasma.Kernel.LoadSchemeFile(savePath);
            
            // Post-load initialization.
            foreach (var entry in saveEntries)
            {
                entry.PostLoadAction?.Invoke(entry.Object);
            }
            
            ShowMessage($"Game loaded.");
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
            currentPlace = playerPlace;
            map?.SetPlace(currentPlace);
        }
        
        // Update camera to follow player.
        map?.UpdateCamera();
        
        // Time acceleration is handled in AdvanceTurn() now
        // Just update visual systems here.
        sky.Advance(!CurrentPlace.Underground);
    }
    
    /// <summary>
    /// Advance game time and other turn-based systems.
    /// </summary>
    private void AdvanceTurn()
    {
        int placeScale = currentPlace?.Scale ?? Common.NON_WILDERNESS_SCALE;
        int ticksPerTurn = placeScale * timeAcceleration;
        
        // Advance the clock.
        clock.Advance(ticksPerTurn);
        
        // Advance the wind.
        wind.AdvanceTurns();
        
        // Update the sky.
        bool skyVisible = currentPlace != null && !currentPlace.Underground;
        sky.Advance(skyVisible);
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
        
        // Wait action.
        if (dx == 0 && dy == 0)
        {
            playerCharacter.DecreaseActionPoints(1);
            ShowMessage("Player waits.");
        }
        else
        {
            // Calculate target position.
            int targetX = playerCharacter.GetX() + dx;
            int targetY = playerCharacter.GetY() + dy;
            
            // Get movement cost for the target tile.
            int movementCost = 1;
            if (currentPlace != null)
            {
                movementCost = currentPlace.GetMovementCost(targetX, targetY, playerCharacter);
                
                // Check for impassable (255 = PTABLE_IMPASSABLE).
                if (movementCost >= 255)
                {
                    ShowMessage("Can't move there!");
                    return;
                }
            }
            
            // Check if player has enough AP for this move.
            if (playerCharacter.ActionPoints < movementCost)
            {
                // Not enough AP - end turn and start fresh.
                playerCharacter.EndTurn();
                playerCharacter.StartTurn();
                ShowMessage($"Need {movementCost} AP to move there. Turn reset.");
                return;
            }
            
            // Execute the movement.
            if (playerCharacter.Move(dx, dy))
            {
                playerCharacter.DecreaseActionPoints(movementCost);
                ShowMessage($"Player moved to ({playerCharacter.GetX()}, {playerCharacter.GetY()}) - AP: {playerCharacter.ActionPoints}");
            }
            else
            {
                ShowMessage("Can't move there!");
                return;
            }
        }
        
        // Check if turn ended.
        if (playerCharacter.ActionPoints <= 0)
        {
            playerCharacter.EndTurn();
            playerCharacter.StartTurn();  // Immediately start new turn.
        }
    }
    
    /// <summary>
    /// Execute all non-player beings in the current place.
    /// This runs their AI/turns after the player's turn ends.
    /// </summary>
    private void HandleOtherBeings()
    {
        if (currentPlace == null)
            return;
        
        // Get all Characters (not just Beings) since only Characters have turns.
        var characters = currentPlace.GetAllBeings()
            .OfType<Character>()
            .Where(c => c != playerCharacter && !c.IsDead)
            .ToList();
        
        foreach (var npc in characters)
        {
            if (npc.IsDead)
                continue;
            
            npc.StartTurn();
            
            // Run AI if this character has one.
            if (npc.HasAI)  // Or: npc.AIBehavior != null
            {
                int lastPoints = 0;
                while (!npc.IsTurnEnded() && npc.ActionPoints != lastPoints)
                {
                    lastPoints = npc.ActionPoints;
                    try
                    {
                        // Execute AI behavior closure.
                        if (npc.AIBehavior is IronScheme.Runtime.Callable callable)
                        {
                            callable.Call(npc);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Session] NPC AI error for {npc.GetName()}: {ex.Message}");
                        break;
                    }
                }
            }
            
            npc.EndTurn();
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
    }

    /// <summary>
    /// Pop the top key handler from the stack.
    /// </summary>
    public void PopKeyHandler()
    {
        if (keyHandlers.Count > 0)
        {
            var handler = keyHandlers.Pop();
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
    
    // =====================================================================
    // SESSION PROC METHODS
    // =====================================================================

    /// <summary>
    /// Set the start procedure for this session.
    /// Called by kern-set-start-proc during game loading.
    /// </summary>
    public void SetStartProc(object proc)
    {
        startProc = proc;
    }

    /// <summary>
    /// Set the camping procedure for this session.
    /// Called by kern-set-camping-proc during game loading.
    /// </summary>
    public void SetCampingProc(object proc)
    {
        campingProc = proc;
    }

    /// <summary>
    /// Execute the start procedure if one is set.
    /// Called after game loading is complete.
    /// </summary>
    public void RunStartProc()
    {
        if (startProc == null)
        {
            Console.WriteLine("[Session] No start proc set");
            return;
        }
        
        Console.WriteLine($"[Session] StartProc type: {startProc.GetType().FullName}");
        
        try
        {
            // Try as IronScheme.Runtime.Callable.
            if (startProc is IronScheme.Runtime.Callable callable)
            {
                Console.WriteLine("[Session] Invoking as Callable");
                callable.Call(playerParty);
                return;
            }
            
            // Try invoking via reflection - IronScheme closures may not directly implement Callable.
            var procType = startProc.GetType();
            
            // Look for a Call method that takes no args.
            var callMethod = procType.GetMethod("Call", Type.EmptyTypes);
            if (callMethod != null)
            {
                Console.WriteLine("[Session] Invoking via reflection Call()");
                callMethod.Invoke(startProc, null);
                return;
            }
            
            // Look for a Call method that takes object[].
            callMethod = procType.GetMethod("Call", new Type[] { typeof(object[]) });
            if (callMethod != null)
            {
                Console.WriteLine("[Session] Invoking via reflection Call(object[])");
                callMethod.Invoke(startProc, new object[] { new object[] { playerParty } });
                return;
            }
            
            // Try using IronScheme's Eval to call it.
            Console.WriteLine("[Session] Trying to invoke via Scheme evaluation");
            var result = $"({startProc})".Eval();
            Console.WriteLine($"[Session] Start proc result: {result}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[Session] Error running start proc: {ex.Message}");
            Console.Error.WriteLine($"[Session] Stack trace: {ex.StackTrace}");
        }
    }

    /// <summary>
    /// Execute the camping procedure if one is set.
    /// Called each turn while the party is camping in the wilderness.
    /// </summary>
    public void RunCampingProc()
    {
        if (campingProc == null) return;
    
        try
        {
            if (campingProc is IronScheme.Runtime.Callable callable)
            {
                // Camping proc takes no arguments.
                callable.Call();
            }
            else
            {
                Console.WriteLine("[Session] CampingProc is not callable");
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[Session] Error running camping proc: {ex.Message}");
        }
    }
}
