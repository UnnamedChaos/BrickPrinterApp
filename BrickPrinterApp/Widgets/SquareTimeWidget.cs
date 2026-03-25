namespace BrickPrinterApp.Widgets;

public class SquareTimeWidget : LuaScriptWidgetBase
{
    public override string Name => "Square Time";
    public override int IntervalMs => 500;
    protected override string ScriptResourceName => "SquareTime.lua";
}
