using BrickPrinterApp.Interfaces;

namespace BrickPrinterApp.Widgets.Conditional;

/// <summary>
/// Conditional widget for BambuLab 3D printer - active when a print is running
/// </summary>
public class BambuLabConditionalWidget : IConditionalWidget
{
    private readonly object _widget;
    private bool _lastConditionState = false;

    public BambuLabConditionalWidget(object widget)
    {
        if (widget is not IWidget && widget is not IScriptWidget)
        {
            throw new ArgumentException("Widget must be either IWidget or IScriptWidget", nameof(widget));
        }

        _widget = widget;

        // Initialize BambuLabWidget when used as conditional widget
        if (_widget is Widgets.BambuLabWidget bambuWidget)
        {
            Console.WriteLine("BambuLab: Initialized for conditional use");
            bambuWidget.Initialize();
        }
    }

    public object Widget => _widget;

    public string ConditionDescription => "When Printing (BambuLab)";

    public bool IsConditionMet()
    {
        // Check if the widget is a BambuLabWidget and if a print is active
        if (_widget is Widgets.BambuLabWidget bambuWidget)
        {
            var result = bambuWidget.IsPrintActive;

            // Log only when state changes
            if (result != _lastConditionState)
            {
                _lastConditionState = result;
                Console.WriteLine($"BambuLab: Condition changed to {(result ? "ACTIVE (print running)" : "INACTIVE (no print)")}");
            }

            return result;
        }

        return false;
    }
}
