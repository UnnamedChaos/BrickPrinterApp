using System.Reflection;
using BrickPrinterApp.Interfaces;

namespace BrickPrinterApp.Widgets;

public class CircularClockWidget : IScriptWidget
{
    public string Name => "Circular Clock";
    public string ScriptLanguage => "lua";

    public string GetScript()
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream("BrickPrinterApp.Scripts.CircularClock.lua");
        if (stream == null)
            throw new InvalidOperationException("Could not find embedded resource: CircularClock.lua");

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
