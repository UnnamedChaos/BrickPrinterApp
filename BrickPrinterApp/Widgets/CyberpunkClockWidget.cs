namespace BrickPrinterApp.Widgets;

public class CyberpunkClockWidget : LuaScriptWidgetBase
{
    public override string Name => "Cyberpunk Clock";
    public override int IntervalMs => 500;
    protected override string ScriptResourceName => "CyberpunkClock.lua";
}
