using Avalonia;
using System;

namespace Phantasma;

sealed class Program
{
    private static Binders.Phantasma phantasma;
    
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        Console.WriteLine("Starting Phantasma...  {0} args", args.Length);
        foreach (var arg in args)
        {
            Console.WriteLine(arg);
        }
        
        phantasma = new Binders.Phantasma(args);
        
        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace()
            .AfterSetup(async builder =>
            {
                // Initialize Phantasma after Avalonia is set up.
                if (phantasma != null)
                {
                    await phantasma.InitializeAsync();
                }
            });
}