using Avalonia.Controls;

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
        
    protected override void OnClosed(System.EventArgs e)
    {
        base.OnClosed(e);
            
        // Clean up game session.
        gameSession?.Stop();
    }
}
