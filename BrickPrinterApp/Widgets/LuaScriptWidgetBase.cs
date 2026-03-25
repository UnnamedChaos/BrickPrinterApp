using System.Reflection;
using BrickPrinterApp.Interfaces;

namespace BrickPrinterApp.Widgets;

public abstract class LuaScriptWidgetBase : IScriptWidget
{
    public abstract string Name { get; }
    public string ScriptLanguage => "lua";
    public virtual int IntervalMs => 1000;

    protected abstract string ScriptResourceName { get; }

    public string GetScript()
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream($"BrickPrinterApp.Scripts.{ScriptResourceName}");
        if (stream == null)
            throw new InvalidOperationException($"Could not find embedded resource: {ScriptResourceName}");

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
