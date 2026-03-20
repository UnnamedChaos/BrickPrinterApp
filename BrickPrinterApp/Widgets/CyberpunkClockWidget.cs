using System.Reflection;
using BrickPrinterApp.Interfaces;

namespace BrickPrinterApp.Widgets;

public class CyberpunkClockWidget : IScriptWidget
{
    public string Name => "Cyberpunk Clock";
    public string ScriptLanguage => "lua";
    public int IntervalMs => 500;

    public string GetScript()
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream("BrickPrinterApp.Scripts.CyberpunkClock.lua");
        if (stream == null)
            throw new InvalidOperationException("Could not find embedded resource: CyberpunkClock.lua");

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
