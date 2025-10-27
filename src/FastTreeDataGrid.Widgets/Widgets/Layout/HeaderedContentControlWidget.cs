using Avalonia;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using FastTreeDataGrid.Control.Theming;

namespace FastTreeDataGrid.Control.Widgets;

public class HeaderedContentControlWidget : ContentControlWidget
{
    private readonly StackLayoutWidget _root;
    private readonly SurfaceWidget _headerHost;
    private readonly SurfaceWidget _bodyHost;
    private Widget? _header;
    private Widget? _content;
    private FormattedTextWidget? _headerTextWidget;

    static HeaderedContentControlWidget()
    {
        WidgetStyleManager.Register(string.Empty, new WidgetStyleRule(
            typeof(HeaderedContentControlWidget),
            WidgetVisualState.Normal,
            static (widget, theme) =>
            {
                if (widget is not HeaderedContentControlWidget headered)
                {
                    return;
                }

                var textPalette = theme.Palette.Text;
                if (headered._headerTextWidget is not null && headered._headerTextWidget.Foreground is null)
                {
                    headered._headerTextWidget.Foreground = textPalette.Foreground.Get(WidgetVisualState.Normal);
                }
            }));
    }

    public HeaderedContentControlWidget()
    {
        _headerHost = new SurfaceWidget();
        _bodyHost = new SurfaceWidget();
        _root = new StackLayoutWidget
        {
            Orientation = Orientation.Vertical,
        };

        _root.Children.Add(_headerHost);
        _root.Children.Add(_bodyHost);
        base.Content = _root;
    }

    public Widget? Header
    {
        get => _header;
        set
        {
            _headerHost.Children.Clear();
            _header = value;
            if (value is not null)
            {
                _headerHost.Children.Add(value);
            }
        }
    }

    public string? HeaderText
    {
        get => _headerTextWidget?.Text;
        set
        {
            if (string.IsNullOrEmpty(value))
            {
                _headerTextWidget = null;
                Header = null;
                return;
            }

            var typography = WidgetFluentPalette.Current.Text.Typography.Header;
            _headerTextWidget ??= new FormattedTextWidget
            {
                DesiredHeight = 20,
            };
            _headerTextWidget.FontFamily = typography.FontFamily;
            _headerTextWidget.FontWeight = typography.FontWeight;
            if (typography.FontSize > 0)
            {
                _headerTextWidget.EmSize = typography.FontSize;
            }
            _headerTextWidget.SetText(value);
            Header = _headerTextWidget;
        }
    }

    public new Widget? Content
    {
        get => _content;
        set
        {
            _content = value;
            _bodyHost.Children.Clear();
            if (value is not null)
            {
                _bodyHost.Children.Add(value);
            }
        }
    }

    protected SurfaceWidget HeaderHost => _headerHost;

    protected SurfaceWidget BodyHost => _bodyHost;

    internal FormattedTextWidget? HeaderTextWidget => _headerTextWidget;
}
