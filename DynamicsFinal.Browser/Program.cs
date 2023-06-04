using System.Runtime.Versioning;
using Avalonia;
using Avalonia.Browser;
using Avalonia.ReactiveUI;
using DynamicsFinal;
using System.Threading.Tasks;

[assembly: SupportedOSPlatform("browser")]

internal partial class Program {
    private static async Task Main(string[] args) => await BuildAvaloniaApp()
        .UseReactiveUI()
        .StartBrowserAppAsync("out");

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>();
}