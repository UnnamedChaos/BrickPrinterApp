using System.Reflection;
using BrickPrinterApp.Interfaces;

namespace BrickPrinterApp.Widgets;

public class SolarSystemWidget : IScriptWidget
{
    public string Name => "Solar System";
    public string ScriptLanguage => "lua";
    public int IntervalMs => 200;

    public string GetScript()
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream("BrickPrinterApp.Scripts.SolarSystem.lua");
        if (stream == null)
            throw new InvalidOperationException("Could not find embedded resource: SolarSystem.lua");

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
