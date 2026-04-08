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
        var activeWindowService = host.Services.GetRequiredService<IActiveWindowService>();
        widgetService.RegisterWidget(new YouTubeWidget(displayService, activeWindowService));
        var googleAuth = host.Services.GetRequiredService<GoogleAuthService>();
        // Restore saved Google authentication on startup
        googleAuth.VerifyConnectionAsync().GetAwaiter().GetResult();
        widgetService.RegisterWidget(new CalendarWidget(googleAuth));
        var settingService = host.Services.GetRequiredService<SettingService>();
        widgetService.RegisterWidget(new BambuLabWidget(settingService));
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

        // Initialize conditional widget manager (old system - process/window based)
        var conditionalManager = host.Services.GetRequiredService<ConditionalWidgetManagerService>();
        conditionalManager.Initialize();

        // Initialize conditional widget monitor (new system - custom logic based)
        var conditionalMonitor = host.Services.GetRequiredService<ConditionalWidgetMonitorService>();
        conditionalMonitor.Initialize();

        // ===== CUSTOM CONDITIONAL WIDGETS =====
        // Register conditional widgets with custom logic here (optional)
        // These will appear in Widget Manager's conditional section with "Custom" type
        // Priority: first registered = highest priority
        // You can also create them via the Widget Manager UI using "+ Add Custom" button

        // Example 1: Always true demo (for testing)
        // var alwaysTrue = new BrickPrinterApp.Widgets.Conditional.AlwaysTrueConditionalWidget(
        //     new LogoWidget(displayService));
        // conditionalMonitor.RegisterConditionalWidget(0, alwaysTrue);

        // Example 2: Show logo widget when "notepad" process is running
        // var notepadCondition = new BrickPrinterApp.Widgets.Conditional.ProcessRunningConditionalWidget(
        //     new LogoWidget(displayService), "notepad");
        // conditionalMonitor.RegisterConditionalWidget(0, notepadCondition);

        // Example 3: Show weather widget during work hours (9 AM - 5 PM)
        // var workHoursCondition = new BrickPrinterApp.Widgets.Conditional.TimeRangeConditionalWidget(
        //     new WeatherWidget(displayService),
        //     new TimeSpan(9, 0, 0),   // 9:00 AM
        //     new TimeSpan(17, 0, 0)); // 5:00 PM
        // conditionalMonitor.RegisterConditionalWidget(0, workHoursCondition);

        // Example 4: Show CPU widget on weekdays only
        // var weekdayCondition = new BrickPrinterApp.Widgets.Conditional.WeekdayConditionalWidget(
        //     new CpuSimpleWidget(displayService),
        //     DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday,
        //     DayOfWeek.Thursday, DayOfWeek.Friday);
        // conditionalMonitor.RegisterConditionalWidget(0, weekdayCondition);

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
        builder.Services.AddSingleton<ConditionalWidgetManagerService>();
        builder.Services.AddSingleton<ConditionalWidgetMonitorService>();
        builder.Services.AddSingleton<GoogleAuthService>();

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
