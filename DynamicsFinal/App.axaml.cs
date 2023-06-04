using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using DynamicsFinal.ViewModels;
using DynamicsFinal.Views;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;

namespace DynamicsFinal;

public partial class App : Application {
    public override void Initialize() {
        AvaloniaXamlLoader.Load(this);
        LiveCharts.Configure(config => config.AddSkiaSharp().AddDefaultMappers().AddLightTheme());
    }

    public override void OnFrameworkInitializationCompleted() {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) {
            desktop.MainWindow = new MainWindow {
                DataContext = new MainViewModel()
            };
        } else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform) {
            singleViewPlatform.MainView = new MainView {
                DataContext = new MainViewModel()
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}