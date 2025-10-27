using Avalonia.Controls;
using Avalonia.Media;
using FastTreeDataGrid.Control.Widgets;
using FastTreeDataGrid.Control.Widgets.Hosting;
using AvaloniaControl = Avalonia.Controls.Control;

namespace FastTreeDataGrid.WidgetsDemo.ViewModels.Widgets;

public sealed class WidgetBoard
{
    public string Title { get; }

    public string Description { get; }

    public AvaloniaControl Board { get; }

    private WidgetBoard(string title, string description, AvaloniaControl board)
    {
        Title = title;
        Description = description;
        Board = board;
    }

    public static WidgetBoard Create(string title, string description, Widget root)
    {
        var surface = WidgetHost.Create(root, width: 400, height: 220, background: Brushes.Transparent);
        return new WidgetBoard(title, description, surface);
    }
}
