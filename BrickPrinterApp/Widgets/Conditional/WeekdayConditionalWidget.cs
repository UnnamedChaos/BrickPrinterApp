using BrickPrinterApp.Interfaces;

namespace BrickPrinterApp.Widgets.Conditional;

/// <summary>
/// Conditional widget that activates on specific days of the week
/// </summary>
public class WeekdayConditionalWidget : IConditionalWidget
{
    private readonly object _widget;
    private readonly DayOfWeek[] _activeDays;

    public WeekdayConditionalWidget(object widget, params DayOfWeek[] activeDays)
    {
        if (widget is not IWidget && widget is not IScriptWidget)
        {
            throw new ArgumentException("Widget must be either IWidget or IScriptWidget", nameof(widget));
        }

        _widget = widget;
        _activeDays = activeDays;
    }

    public object Widget => _widget;

    public string ConditionDescription
    {
        get
        {
            var dayNames = _activeDays.Select(d => d.ToString().Substring(0, 3));
            return $"Active on: {string.Join(", ", dayNames)}";
        }
    }

    public bool IsConditionMet()
    {
        return _activeDays.Contains(DateTime.Now.DayOfWeek);
    }
}
