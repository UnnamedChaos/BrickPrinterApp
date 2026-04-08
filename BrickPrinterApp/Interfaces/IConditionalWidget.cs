namespace BrickPrinterApp.Interfaces;

/// <summary>
/// Interface for widgets that should be displayed conditionally based on custom logic.
/// </summary>
public interface IConditionalWidget
{
    /// <summary>
    /// The widget to display (either IWidget or IScriptWidget)
    /// </summary>
    object Widget { get; }

    /// <summary>
    /// Human-readable description of the condition (shown in UI)
    /// </summary>
    string ConditionDescription { get; }

    /// <summary>
    /// Checks if the condition is currently met
    /// </summary>
    /// <returns>True if the widget should be displayed</returns>
    bool IsConditionMet();
}
