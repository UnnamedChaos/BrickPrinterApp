using System.Reflection;
using BrickPrinterApp.Interfaces;

namespace BrickPrinterApp.Widgets;

public class LuaClockWidget : IScriptWidget
{
    public string Name => "Digital Clock";
    public string ScriptLanguage => "lua";

    public string GetScript()
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream("BrickPrinterApp.Scripts.DigitalClock.lua");
        if (stream == null)
            throw new InvalidOperationException("Could not find embedded resource: DigitalClock.lua");

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
