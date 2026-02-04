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
        builder.Services.AddHttpClient();
        builder.Services.AddSingleton<IDisplayService, DisplayService>();
        builder.Services.AddSingleton<ITransferService, TransferService>();
        builder.Services.AddSingleton<SettingService>(); // Singleton: Eine Instanz für die ganze App
        builder.Services.AddTransient<SettingsForm>(); // Transient: Jedes Mal ein neues Fenster-Objekt
        builder.Services.AddTransient<BrickPrinter>();
    }
}