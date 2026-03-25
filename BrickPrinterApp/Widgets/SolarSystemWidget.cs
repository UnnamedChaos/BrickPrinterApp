namespace BrickPrinterApp.Widgets;

public class SolarSystemWidget : LuaScriptWidgetBase
{
    public override string Name => "Solar System";
    public override int IntervalMs => 200;
    protected override string ScriptResourceName => "SolarSystem.lua";
}
