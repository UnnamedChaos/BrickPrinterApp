using BrickPrinterApp.Forms;
using BrickPrinterApp.Interfaces;
using BrickPrinterApp.Services;
using BrickPrinterApp.Widgets;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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
        RegisterServices(builder);
        using var host = builder.Build();

        // Register widgets
        var widgetService = host.Services.GetRequiredService<WidgetService>();
        var textService = host.Services.GetRequiredService<ITextService>();
        var displayService = host.Services.GetRequiredService<IDisplayService>();
        widgetService.RegisterWidget(new SampleTextWidget(textService));
        widgetService.RegisterWidget(new LogoWidget(displayService));
        widgetService.RegisterScriptWidget(new LuaClockWidget());
        widgetService.RegisterScriptWidget(new CircularClockWidget());

        var mainForm = host.Services.GetRequiredService<BrickPrinter>();
        Application.Run(mainForm);
    }

    private static void RegisterServices(HostApplicationBuilder builder)
    {
        builder.Services.AddSingleton<SettingService>();
        builder.Services.AddSingleton<IDisplayService, DisplayService>();
        builder.Services.AddSingleton<ITextService, RawTextService>();
        builder.Services.AddSingleton<WidgetService>();

        // Register TransferService with typed HttpClient
        // Disable keep-alive - ESP32 handles single connections better
        builder.Services.AddHttpClient<ITransferService, TransferService>()
            .ConfigureHttpClient(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(10);
                client.DefaultRequestHeaders.ConnectionClose = true;
            });

        builder.Services.AddTransient<SettingsForm>();
        builder.Services.AddTransient<WidgetManagerForm>();
        builder.Services.AddTransient<BrickPrinter>();
    }
}
