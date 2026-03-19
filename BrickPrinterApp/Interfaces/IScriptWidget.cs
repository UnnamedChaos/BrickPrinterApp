namespace BrickPrinterApp.Interfaces;

public interface IScriptWidget
{
    string Name { get; }
    string ScriptLanguage { get; }
    string GetScript();
}
