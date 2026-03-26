using System.Net.Http;
using BrickPrinterApp.Forms;
using BrickPrinterApp.Interfaces;
using BrickPrinterApp.Services;
using BrickPrinterApp.Widgets;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BrickPrinterApp;

internal static class Program
{
    /// <summary>
    ///     The main entry point for the application.
    /// </summary>
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        var builder = Host.CreateApplicationBuilder();

        // Suppress System.Net info logging
        builder.Logging.AddFilter("System.Net.Http", Microsoft.Extensions.Logging.LogLevel.Warning);
        builder.Logging.AddFilter("System.Net.Sockets", Microsoft.Extensions.Logging.LogLevel.Warning);

        RegisterServices(builder);
        using var host = builder.Build();

        // Register widgets
        var widgetService = host.Services.GetRequiredService<WidgetService>();
        var textService = host.Services.GetRequiredService<ITextService>();
        var displayService = host.Services.GetRequiredService<IDisplayService>();
        widgetService.RegisterWidget(new SampleTextWidget(textService));
        widgetService.RegisterWidget(new LogoWidget(displayService));
        widgetService.RegisterWidget(new WeatherWidget(displayService));
        widgetService.RegisterWidget(new StockWidget(displayService));
        widgetService.RegisterWidget(new CpuHeatmapWidget(displayService));
        widgetService.RegisterWidget(new CpuSimpleWidget(displayService));
        widgetService.RegisterWidget(new GpuWidget(displayService));
        widgetService.RegisterScriptWidget(new LuaClockWidget());
        widgetService.RegisterScriptWidget(new CircularClockWidget());
        widgetService.RegisterScriptWidget(new CyberpunkClockWidget());
        widgetService.RegisterScriptWidget(new SolarSystemWidget());
        widgetService.RegisterScriptWidget(new SquareTimeWidget());

        // Load saved widget assignments
        widgetService.LoadSavedAssignments();

        // Load saved rotation configs
        var rotationManager = host.Services.GetRequiredService<RotationManagerService>();
        rotationManager.LoadSavedConfigs();

        var mainForm = host.Services.GetRequiredService<BrickPrinter>();
        Application.Run(mainForm);
    }

    private static void RegisterServices(HostApplicationBuilder builder)
    {
        builder.Services.AddSingleton<SettingService>();
        builder.Services.AddSingleton<IDisplayService, DisplayService>();
        builder.Services.AddSingleton<ITextService, RawTextService>();
        builder.Services.AddSingleton<IActiveWindowService, ActiveWindowService>();
        builder.Services.AddSingleton<ActiveWindowWatcherService>();
        builder.Services.AddSingleton<WidgetService>();
        builder.Services.AddSingleton<RotationManagerService>();

        // Register TransferService with typed HttpClient
        // Configure handler to avoid stale connection issues with ESP32
        builder.Services.AddHttpClient<ITransferService, TransferService>()
            .ConfigureHttpClient(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(10);
                client.DefaultRequestHeaders.ConnectionClose = true;
            })
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
            {
                // Don't pool connections - ESP32 closes them unpredictably
                PooledConnectionLifetime = TimeSpan.Zero,
                PooledConnectionIdleTimeout = TimeSpan.FromSeconds(1),
                // Faster connection timeout for embedded devices
                ConnectTimeout = TimeSpan.FromSeconds(5),
            });

        builder.Services.AddTransient<SettingsForm>();
        builder.Services.AddTransient<WidgetManagerForm>();
        builder.Services.AddTransient<BrickPrinter>();
    }
}
