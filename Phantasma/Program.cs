using Avalonia;
using System;

namespace Phantasma;

sealed class Program
{
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
        
        Phantasma.Initialize(args);
        
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
                await Phantasma.Instance.InitializeAsync();  // Use singleton directly.
            });
}