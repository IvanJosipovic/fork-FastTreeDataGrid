namespace FastTreeDataGrid.Control.Widgets;

public sealed class WidgetAutomationProperties
{
    public string? Name { get; set; }

    public string? CommandLabel { get; set; }

    public string? AccessKey { get; set; }
}

public sealed record WidgetAutomationSettings(
    string? Name = null,
    string? CommandLabel = null,
    string? AccessKey = null);
