using System;
using Avalonia.Controls;
using Avalonia.Input;

using Phantasma.Models;

namespace Phantasma.Views;

public partial class MainWindow : Window
{
    private Session gameSession;
    private GameView gameView;
    private Command command;
    
    public MainWindow()
    {
        InitializeComponent();
        InitializeGame();
    
        // Hook up keyboard input.
        this.KeyDown += OnKeyDown;
        
        WindowState = WindowState.FullScreen;
        SystemDecorations = SystemDecorations.None;
    }
        
    private void InitializeGame()
    {
        // Get the main session that was created during startup.
        gameSession = Phantasma.MainSession;
        
        if (gameSession == null)
        {
            // Fallback: if mainSession wasn't created for some reason, create one.
            Console.WriteLine("[MainWindow] Warning: MainSession is null; creating fallback session.");
            var phantasma = Phantasma.Instance;
            var place = phantasma.GetSchemeObject<Place>("test-place");
            var player = phantasma.GetSchemeObject<Character>("player");
            gameSession = new Session(place, player);
        }
        
        // Initialize command system.
        command = new Command(gameSession);
            
        // Find the GameView control and set its session.
        gameView = this.GetControl<GameView>("GameViewControl");
        if (gameView != null)
        {
            gameView.GetScreen().BindToSession(gameSession);
        }
        
        // Status View
        var statusView = this.FindControl<StatusView>("StatusViewControl");
        if (statusView != null)
        {
            var statusBinder = statusView.GetBinder();
            statusBinder.Initialize(gameSession.Status, gameSession.Party);
            statusView.SubscribeToChanges();
        }
        else
        {
            Console.WriteLine("ERROR: StatusView not found!");  // Debug
        }
        
        // Console View
        var consoleView = this.FindControl<ConsoleView>("ConsoleViewControl");
        if (consoleView != null)
        {
            var consoleBinder = consoleView.GetBinder();
            consoleView.SubscribeToChanges();
            
            // Connect Session's ConsoleMessage event to the console binder.
            gameSession.ConsoleMessage += (msg) => consoleBinder.PrintLine(msg);
            
            // Welcome Message
            consoleBinder.PrintLine("Welcome to Phantasma!");
            consoleBinder.PrintLine("Use arrow keys or numpad to move.");
            consoleBinder.PrintLine("");
        }
        else
        {
            Console.WriteLine("ERROR: ConsoleView not found!");
        }
    
        // Command Window
        var cmdView = this.FindControl<CommandWindow>("CommandWindowControl");
        if (cmdView != null)
        {
            var cmdBinder = cmdView.GetBinder();
            cmdBinder.Initialize(gameSession);
        }
        else
        {
            Console.WriteLine("ERROR: CommandWindowView not found!");  // Debug
        } 
        
        // Command Prompt
        if (cmdView != null)
        {
            var cmdBinder = cmdView.GetBinder();
    
            gameSession.PromptChanged += (prompt) =>
            {
                cmdBinder.Clear();
                if (!string.IsNullOrEmpty(prompt))
                {
                    cmdBinder.Print(prompt);
                    cmdBinder.Mark();  // Mark position so we don't backspace into prompt.
                    cmdBinder.ShowCursor = true;
                }
                else
                {
                    cmdBinder.ShowCursor = false;
                }
            };
    
            gameSession.CommandInputChanged += (text) =>
            {
                // Erase back to mark (the prompt), then print new text.
                cmdBinder.EraseBackToMark();
                cmdBinder.Print(text);
            };
        }
        
        // Sky View
        var skyView = this.FindControl<SkyView>("SkyViewControl");
        if (skyView != null)
        {
            var skyBinder = skyView.GetBinder();
            skyBinder.BindToSession(gameSession);
            skyView.SubscribeToChanges();
        }
        else
        {
            Console.WriteLine("ERROR: SkyView not found!");
        }
        
        // Start the game.
        gameSession.Start();
    }
    
    /// <summary>
    /// Handle keyboard input.
    /// Checks key handler stack first.
    /// </summary>
    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (gameSession == null)
            return;
        
        // ===================================================================
        // GLOBAL KEYS - Work even during text input/targeting.
        // ===================================================================
        switch (e.Key)
        {
            case Key.F5:
                gameSession.Save("quicksave.scm");
                gameSession.LogMessage("Quick saved.");
                e.Handled = true;
                return;
                
            case Key.F9:
                var quickSavePath = System.IO.Path.Combine(
                    Phantasma.Configuration["saved-games-dirname"],
                    "quicksave.scm"
                );
                if (System.IO.File.Exists(quickSavePath))
                {
                    gameSession.Load("quicksave.scm");
                    gameSession.LogMessage("Quick save loaded.");
                }
                else
                {
                    gameSession.LogMessage("No quick save found.");
                }
                e.Handled = true;
                return;
        }
        
        // ===================================================================
        // CHECK FOR ACTIVE KEY HANDLER (text input, targeting, yes/no, etc.)
        // ===================================================================
        var handler = gameSession.CurrentKeyHandler;
        if (handler != null)
        {
            bool done = handler.HandleKey(e.Key, e.KeySymbol);
            
            if (done)
            {
                gameSession.PopKeyHandler();
            }
            
            e.Handled = true;
            return;  // Don't fall through to command processing
        }
        
        // ===================================================================
        // NO ACTIVE HANDLER - Process normal game commands
        // ===================================================================
        if (gameSession.Player == null)
            return;
        
        switch (e.Key)
        {
            // =============================================================
            // MOVEMENT KEYS
            // =============================================================
            case Key.Up:
            case Key.NumPad8:
                gameSession.HandlePlayerMove(0, -1);
                break;
            case Key.Down:
            case Key.NumPad2:
                gameSession.HandlePlayerMove(0, 1);
                break;
            case Key.Left:
            case Key.NumPad4:
                gameSession.HandlePlayerMove(-1, 0);
                break;
            case Key.Right:
            case Key.NumPad6:
                gameSession.HandlePlayerMove(1, 0);
                break;
            case Key.NumPad7:
                gameSession.HandlePlayerMove(-1, -1);
                break;
            case Key.NumPad9:
                gameSession.HandlePlayerMove(1, -1);
                break;
            case Key.NumPad1:
                gameSession.HandlePlayerMove(-1, 1);
                break;
            case Key.NumPad3:
                gameSession.HandlePlayerMove(1, 1);
                break;
            case Key.NumPad5:
            case Key.Space:
                // Pass turn / wait
                gameSession.HandlePlayerMove(0, 0);
                break;
            
            // =============================================================
            // COMBAT COMMANDS
            // =============================================================
            case Key.A:
                // Attack
                command.Attack();
                break;
            case Key.F:
                // Fire (vehicle weapons)
                command.Fire();
                break;
            
            // =============================================================
            // MAGIC COMMANDS
            // =============================================================
            case Key.C:
                // Cast spell
                command.CastSpell();
                break;
            case Key.M:
                // Mix reagents
                command.MixReagents();
                break;
            case Key.Y:
                // Yuse (special abilities)
                command.Yuse();
                break;
            
            // =============================================================
            // INVENTORY/EQUIPMENT COMMANDS
            // =============================================================
            case Key.G:
                // Get/pickup items
                command.Get(scoopAll: true);
                break;
            case Key.I:
                // Show inventory
                command.Inventory();
                break;
            case Key.R:
                // Ready (equip) items
                command.Ready();
                break;
            case Key.U:
                // Use item
                command.Use();
                break;
            case Key.D:
                // Drop item (or Descend/Dismount)
                command.Drop();
                break;
            
            // =============================================================
            // INTERACTION COMMANDS
            // =============================================================
            case Key.T:
                // Talk to NPC
                command.Talk();
                break;
            case Key.O:
                // Open (door, chest, etc.)
                command.Open();
                break;
            case Key.H:
                // Handle (mechanism)
                command.Handle();
                break;
            case Key.S:
                // Search
                command.Search();
                break;
            case Key.X:
                // Examine
                command.Examine();
                break;
            case Key.L:
                // Look
                command.Look();
                break;
            
            // =============================================================
            // NAVIGATION COMMANDS
            // =============================================================
            case Key.K:
                // Klimb (ladders, etc.) or Camp
                command.Klimb();
                break;
            case Key.E:
                // Enter (portal, building)
                command.Enter();
                break;
            case Key.B:
                // Board vehicle
                command.Board();
                break;
            case Key.OemPeriod:  // '>'
                // Zoom in / enter combat
                command.ZoomIn();
                break;
            case Key.OemComma:  // '<'
                // Zoom out / exit combat
                command.ZoomOut();
                break;
            
            // =============================================================
            // PARTY MANAGEMENT
            // =============================================================
            case Key.N:
                // New order (party formation)
                command.NewOrder();
                break;
            case Key.Z:
                // Ztats (character statistics)
                command.Ztats();
                break;
            
            // =============================================================
            // GAME COMMANDS
            // =============================================================
            case Key.Q:
                // Quit
                command.Quit();
                break;
            case Key.OemQuestion:  // '?'
                // Help
                command.Help();
                break;
            
            // =============================================================
            // SPECIAL/DEBUG
            // =============================================================
            case Key.Escape:
                // Cancel current action or exit modal
                if (gameSession.Status != null && gameSession.Status.Mode != StatusMode.ShowParty)
                {
                    gameSession.Status.SetMode(StatusMode.ShowParty);
                    gameSession.LogMessage("");
                }
                break;
            
            // Solo mode keys (1-9 to control individual party members)
            case Key.D1:
            case Key.D2:
            case Key.D3:
            case Key.D4:
            case Key.D5:
            case Key.D6:
            case Key.D7:
            case Key.D8:
            case Key.D9:
                int memberIndex = e.Key - Key.D1;
                command.EnterSoloMode(memberIndex);
                break;
            case Key.D0:
                // Exit solo mode
                command.ExitSoloMode();
                break;
        }
        
        e.Handled = true;
    }
        
    protected override void OnClosed(System.EventArgs e)
    {
        base.OnClosed(e);
            
        // Clean up game session.
        gameSession?.Stop();
    }
}
