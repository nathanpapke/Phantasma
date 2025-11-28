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
            gameView.GameSession = gameSession;
        }
        // Status View
        var statusView = this.FindControl<StatusView>("StatusViewControl");
        if (statusView != null)
        {
            var statusBinder = statusView.GetBinder();
            statusBinder.Initialize(gameSession.Status, gameSession.Party);
            statusView.SubscribeToChanges();
            Console.WriteLine("StatusView initialized.");  // Debug
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
            
            Console.WriteLine("ConsoleView initialized.");
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
            Console.WriteLine("CommandWindowView initialized.");  // Debug
        }
        else
        {
            Console.WriteLine("ERROR: CommandWindowView not found!");  // Debug
        }
        
        // Start the game.
        gameSession.Start();
    }
    
    /// <summary>
    /// Handle keyboard input for player movement.
    /// </summary>
    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (gameSession == null || gameSession.Player == null)
            return;
        
        // Handle arrow keys for movement.
        // TODO: Handle input with Session.
        switch (e.Key)
        {
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
                gameSession.HandlePlayerMove(-1, -1); // Northwest
                break;
            case Key.NumPad9:
                gameSession.HandlePlayerMove(1, -1); // Northeast
                break;
            case Key.NumPad1:
                gameSession.HandlePlayerMove(-1, 1); // Southwest
                break;
            case Key.NumPad3:
                gameSession.HandlePlayerMove(1, 1); // Southeast
                break;
            case Key.NumPad5:
                gameSession.HandlePlayerMove(0, 0); // Wait/Rest
                break;
            
            // Command Keys
            case Key.G:
                command.Get(scoopAll: true);
                break;
            case Key.O:
                command.Open();
                break;
            case Key.I:
                command.Inventory();
                break;
            case Key.T:
                command.Talk();
                break;
            /*
            case Key.A:
                command.Attack();
                break;
            case Key.C:
                command.CastSpell();
                break;
            case Key.U:
                command.Use();
                break;
            case Key.R:
                command.Ready();
                break;
            case Key.Z:
                command.Ztats();
                break;
            */
            case Key.Escape:
                this.Close();
                break;
        }
    
        // Prevent further processing.
        e.Handled = true;
    }
        
    protected override void OnClosed(System.EventArgs e)
    {
        base.OnClosed(e);
            
        // Clean up game session.
        gameSession?.Stop();
    }
}
