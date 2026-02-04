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

        // HostBuilder für DI und Background-Services
        var builder = Host.CreateApplicationBuilder();

        // Services registrieren
        builder.Services.AddHttpClient();
        builder.Services.AddSingleton<IDisplayService, DisplayService>();
        builder.Services.AddSingleton<SettingService>(); // Singleton: Eine Instanz für die ganze App
        builder.Services.AddTransient<SettingsForm>(); // Transient: Jedes Mal ein neues Fenster-Objekt

        // Das Hauptfenster selbst als Service registrieren
        builder.Services.AddTransient<BrickPrinter>();

        using var host = builder.Build();

        // 3. Die App über den Service-Provider starten
        var mainForm = host.Services.GetRequiredService<BrickPrinter>();
        Application.Run(mainForm);
    }
}