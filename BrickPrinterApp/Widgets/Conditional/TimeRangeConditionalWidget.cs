using BrickPrinterApp.Interfaces;

namespace BrickPrinterApp.Widgets.Conditional;

/// <summary>
/// Conditional widget that activates during a specific time range
/// </summary>
public class TimeRangeConditionalWidget : IConditionalWidget
{
    private readonly object _widget;
    private readonly TimeSpan _startTime;
    private readonly TimeSpan _endTime;

    public TimeRangeConditionalWidget(object widget, TimeSpan startTime, TimeSpan endTime)
    {
        if (widget is not IWidget && widget is not IScriptWidget)
        {
            throw new ArgumentException("Widget must be either IWidget or IScriptWidget", nameof(widget));
        }

        _widget = widget;
        _startTime = startTime;
        _endTime = endTime;
    }

    public object Widget => _widget;

    public string ConditionDescription =>
        $"Active {_startTime:hh\\:mm} - {_endTime:hh\\:mm}";

    public bool IsConditionMet()
    {
        var now = DateTime.Now.TimeOfDay;

        // Handle cases where range crosses midnight
        if (_startTime <= _endTime)
        {
            return now >= _startTime && now < _endTime;
        }
        else
        {
            return now >= _startTime || now < _endTime;
        }
    }
}
