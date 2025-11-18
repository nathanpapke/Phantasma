using Avalonia.Controls;
using Avalonia.Input;

using Phantasma.Models;

namespace Phantasma.Views;

public partial class MainWindow : Window
{
    private Session gameSession;
    private GameView gameView;
    
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
        // Create and start game session.
        gameSession = new Session();
            
        // Find the GameView control and set its session.
        gameView = this.GetControl<GameView>("GameViewControl");
        if (gameView != null)
        {
            gameView.GameSession = gameSession;
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
