using BrickPrinterApp.Interfaces;
using BrickPrinterApp.Models;

namespace BrickPrinterApp.Forms;

/// <summary>
/// Conditional widget management logic for WidgetManagerForm.
/// This partial class handles conditional widget configuration.
/// </summary>
public partial class WidgetManagerForm
{
    private void LoadConditionalConfig(int screenId)
    {
        _conditionalLists[screenId].Items.Clear();
        int priority = 1;

        // Load custom conditional widgets (new system - highest priority)
        var customWidgets = _conditionalMonitor.GetConditionalWidgets(screenId);
        foreach (var customWidget in customWidgets)
        {
            var widgetName = customWidget.Widget switch
            {
                IWidget w => w.Name,
                IScriptWidget sw => $"[Lua] {sw.Name}",
                _ => "Unknown"
            };

            var item = new ListViewItem(priority.ToString());
            item.SubItems.Add("Custom");
            item.SubItems.Add(customWidget.ConditionDescription);
            item.SubItems.Add(widgetName);
            item.Tag = customWidget;
            item.ForeColor = Color.DarkBlue;
            _conditionalLists[screenId].Items.Add(item);
            priority++;
        }

        // Load process/window conditional widgets (old system - lower priority)
        if (_settingService.ConditionalConfigs.TryGetValue(screenId, out var configs))
        {
            foreach (var config in configs)
            {
                if (!config.IsEnabled && string.IsNullOrEmpty(config.WidgetName))
                    continue;

                var conditionText = BuildConditionText(config);

                var item = new ListViewItem(priority.ToString());
                item.SubItems.Add("Process");
                item.SubItems.Add(conditionText);
                item.SubItems.Add(config.WidgetName);
                item.Tag = config;
                item.ForeColor = config.IsEnabled ? Color.Black : Color.Gray;
                _conditionalLists[screenId].Items.Add(item);
                priority++;
            }
        }
    }

    private string BuildConditionText(ConditionalWidgetConfig config)
    {
        var parts = new List<string>();
        if (!string.IsNullOrEmpty(config.ProcessName))
            parts.Add($"Process: {config.ProcessName}");
        if (!string.IsNullOrEmpty(config.WindowTitleContains))
            parts.Add($"Title: {config.WindowTitleContains}");

        var separator = config.MatchBothConditions ? " AND " : " OR ";
        return parts.Count > 0 ? string.Join(separator, parts) : "No condition";
    }

    private void ApplyConditionalConfig(int screenId)
    {
        var configs = new List<ConditionalWidgetConfig>();

        foreach (ListViewItem item in _conditionalLists[screenId].Items)
        {
            if (item.Tag is ConditionalWidgetConfig config)
            {
                configs.Add(config);
            }
        }

        _settingService.ConditionalConfigs[screenId] = configs;
        _settingService.Save();
    }

    private void MoveConditionalUp(int screenId)
    {
        var listView = _conditionalLists[screenId];
        if (listView.SelectedItems.Count == 0) return;

        var selectedIndex = listView.SelectedIndices[0];
        if (selectedIndex == 0) return; // Already at top

        var selectedItem = listView.SelectedItems[0];

        // Check if both items are custom widgets (only custom widgets can be reordered)
        var customWidgets = _conditionalMonitor.GetConditionalWidgets(screenId).ToList();

        if (selectedIndex < customWidgets.Count && selectedIndex > 0)
        {
            // Both are custom widgets - swap in the monitor service
            var temp = customWidgets[selectedIndex];
            customWidgets[selectedIndex] = customWidgets[selectedIndex - 1];
            customWidgets[selectedIndex - 1] = temp;
            _conditionalMonitor.SetConditionalWidgets(screenId, customWidgets);
            _conditionalMonitor.SaveToSettings();

            LoadConditionalConfig(screenId);
            listView.Items[selectedIndex - 1].Selected = true;
        }
        else
        {
            MessageBox.Show("Only custom conditional widgets can be reordered. Process-based conditions are always lower priority.",
                "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    private void MoveConditionalDown(int screenId)
    {
        var listView = _conditionalLists[screenId];
        if (listView.SelectedItems.Count == 0) return;

        var selectedIndex = listView.SelectedIndices[0];
        var customWidgets = _conditionalMonitor.GetConditionalWidgets(screenId).ToList();

        if (selectedIndex < customWidgets.Count - 1)
        {
            // Both are custom widgets - swap in the monitor service
            var temp = customWidgets[selectedIndex];
            customWidgets[selectedIndex] = customWidgets[selectedIndex + 1];
            customWidgets[selectedIndex + 1] = temp;
            _conditionalMonitor.SetConditionalWidgets(screenId, customWidgets);
            _conditionalMonitor.SaveToSettings();

            LoadConditionalConfig(screenId);
            listView.Items[selectedIndex + 1].Selected = true;
        }
        else
        {
            MessageBox.Show("Only custom conditional widgets can be reordered within their group.",
                "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    private void ShowAddCustomConditionalDialog(int screenId)
    {
        if (ShowCustomConditionalDialog(screenId, out var conditionalWidget))
        {
            _conditionalMonitor.RegisterConditionalWidget(screenId, conditionalWidget);
            _conditionalMonitor.SaveToSettings();
            LoadConditionalConfig(screenId);
        }
    }

    private void ShowAddProcessConditionalDialog(int screenId)
    {
        var config = new ConditionalWidgetConfig { IsEnabled = true };
        if (ShowConditionalEditDialog(config, "Add Process/Window Condition"))
        {
            LoadConditionalConfig(screenId);
        }
    }

    private void RemoveConditionalWidget(int screenId)
    {
        if (_conditionalLists[screenId].SelectedItems.Count == 0)
        {
            MessageBox.Show("Please select a conditional widget to remove.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var item = _conditionalLists[screenId].SelectedItems[0];

        if (item.Tag is IConditionalWidget customWidget)
        {
            // Remove custom conditional widget
            var result = MessageBox.Show("Remove this custom conditional widget?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result == DialogResult.Yes)
            {
                _conditionalMonitor.RemoveConditionalWidget(screenId, customWidget);
                _conditionalMonitor.SaveToSettings();
                LoadConditionalConfig(screenId);
            }
        }
        else if (item.Tag is ConditionalWidgetConfig)
        {
            // Remove process/window conditional widget
            var result = MessageBox.Show("Remove this process/window condition?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result == DialogResult.Yes)
            {
                _conditionalLists[screenId].Items.Remove(item);
            }
        }
    }
}
