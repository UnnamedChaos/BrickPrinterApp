using BrickPrinterApp.Forms;
using BrickPrinterApp.Interfaces;
using BrickPrinterApp.Services;
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
        var mainForm = host.Services.GetRequiredService<BrickPrinter>();
        Application.Run(mainForm);
    }

    private static void RegisterServices(HostApplicationBuilder builder)
    {
        builder.Services.AddSingleton<SettingService>();
        builder.Services.AddSingleton<IDisplayService, DisplayService>();
        builder.Services.AddSingleton<ITextService, RawTextService>();

        // Register TransferService with typed HttpClient for keep-alive support
        builder.Services.AddHttpClient<ITransferService, TransferService>()
            .ConfigureHttpClient(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(10);
                client.DefaultRequestHeaders.ConnectionClose = false;
            });

        builder.Services.AddTransient<SettingsForm>();
        builder.Services.AddTransient<BrickPrinter>();
    }
}