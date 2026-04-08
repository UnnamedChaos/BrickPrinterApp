using BrickPrinterApp.Interfaces;
using System.Diagnostics;

namespace BrickPrinterApp.Widgets.Conditional;

/// <summary>
/// Conditional widget that activates when a specific process is running
/// </summary>
public class ProcessRunningConditionalWidget : IConditionalWidget
{
    private readonly object _widget;
    private readonly string _processName;

    public ProcessRunningConditionalWidget(object widget, string processName)
    {
        if (widget is not IWidget && widget is not IScriptWidget)
        {
            throw new ArgumentException("Widget must be either IWidget or IScriptWidget", nameof(widget));
        }

        _widget = widget;
        _processName = processName;
    }

    public object Widget => _widget;

    public string ConditionDescription => $"When {_processName} is running";

    public bool IsConditionMet()
    {
        try
        {
            var processes = Process.GetProcessesByName(_processName);
            return processes.Length > 0;
        }
        catch
        {
            return false;
        }
    }
}
