namespace BrickPrinterApp.Interfaces;

public interface IScriptWidget
{
    string Name { get; }
    string ScriptLanguage { get; }
    int IntervalMs => 1000;
    string GetScript();
}
