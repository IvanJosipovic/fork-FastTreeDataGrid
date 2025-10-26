using Avalonia;
using Avalonia.Media;
using BenchmarkDotNet.Attributes;
using FastTreeDataGrid.Control.Widgets;

namespace FastTreeDataGrid.Benchmarks;

[MemoryDiagnoser]
public class WidgetInteractionBenchmarks
{
    private TabControlWidget _tabControl = null!;
    private MenuWidget _menu = null!;
    private ExpanderWidget _expander = null!;
    private ScrollViewerWidget _scrollViewer = null!;
    private BenchmarkVirtualizingHost _host = null!;

    [GlobalSetup]
    public void Setup()
    {
        _tabControl = new TabControlWidget();
        _tabControl.ApplyTemplate();

        var tabItems = new[] { "Overview", "Details", "Settings" };
        _tabControl.UpdateValue(null, new TabControlWidgetValue(
            Items: tabItems,
            SelectedIndex: 0,
            HeaderFactory: (_, item) => CreateHeader((string)item!),
            ContentFactory: (_, item) => CreateContent((string)item!)));

        _menu = new MenuWidget();
        _menu.UpdateValue(null, CreateMenuValue());

        _expander = new ExpanderWidget();
        _expander.Arrange(new Rect(0, 0, 220, 140));

        _scrollViewer = new ScrollViewerWidget
        {
            Padding = new Thickness(6),
        };
        _host = new BenchmarkVirtualizingHost();
        _scrollViewer.Children.Add(_host);
        _scrollViewer.Arrange(new Rect(0, 0, 320, 180));
    }

    [Benchmark]
    public void SwitchTabs()
    {
        for (var i = 0; i < 3; i++)
        {
            _tabControl.SetSelectedIndex(i);
        }
    }

    [Benchmark]
    public void ToggleExpander()
    {
        _expander.HandlePointerEvent(new WidgetPointerEvent(WidgetPointerEventKind.Released, new Point(8, 8), null));
        _expander.HandlePointerEvent(new WidgetPointerEvent(WidgetPointerEventKind.Released, new Point(8, 8), null));
    }

    [Benchmark]
    public void ScrollViewport()
    {
        for (var i = 0; i < 4; i++)
        {
            _scrollViewer.VerticalOffset = i * 16;
        }
    }

    [Benchmark]
    public void RefreshMenuItems()
    {
        _menu.UpdateValue(null, CreateMenuValue());
    }

    private static Widget CreateHeader(string text)
    {
        var header = new FormattedTextWidget
        {
            EmSize = 13,
            DesiredHeight = 20,
        };
        header.SetText(text);
        return header;
    }

    private static Widget CreateContent(string text)
    {
        var content = new FormattedTextWidget
        {
            EmSize = 12,
            DesiredHeight = 28,
            DesiredWidth = 240,
            Trimming = TextTrimming.CharacterEllipsis,
        };
        content.SetText($"Benchmark content for {text}");
        return content;
    }

    private static MenuWidgetValue CreateMenuValue()
    {
        return new MenuWidgetValue(new[]
        {
            new MenuItemWidgetValue("_New", GestureText: "Ctrl+N"),
            new MenuItemWidgetValue("_Open…", GestureText: "Ctrl+O"),
            new MenuItemWidgetValue("_Duplicate"),
            new MenuItemWidgetValue(string.Empty, IsSeparator: true),
            new MenuItemWidgetValue("_Delete", GestureText: "Shift+Delete"),
        });
    }

    private sealed class BenchmarkVirtualizingHost : Widget, IVirtualizingWidgetHost
    {
        public VirtualizingWidgetViewport LastViewport { get; private set; }

        public override void Draw(DrawingContext context)
        {
        }

        public void UpdateViewport(in VirtualizingWidgetViewport viewport)
        {
            LastViewport = viewport;
        }
    }
}
