using BrickPrinterApp.Interfaces;

namespace BrickPrinterApp.Widgets.Conditional;

/// <summary>
/// Sample conditional widget that is always true (for demonstration purposes)
/// </summary>
public class AlwaysTrueConditionalWidget : IConditionalWidget
{
    private readonly object _widget;

    public AlwaysTrueConditionalWidget(object widget)
    {
        if (widget is not IWidget && widget is not IScriptWidget)
        {
            throw new ArgumentException("Widget must be either IWidget or IScriptWidget", nameof(widget));
        }

        _widget = widget;
    }

    public object Widget => _widget;

    public string ConditionDescription => "Always Active (Demo)";

    public bool IsConditionMet() => true;
}
