using Avalonia;
using Avalonia.Controls;

namespace ControlCatalog.Pages;

public partial class SideBySideComparisonPage : UserControl
{
    public static readonly StyledProperty<Control?> LeftContentProperty =
        AvaloniaProperty.Register<SideBySideComparisonPage, Control?>(nameof(LeftContent));

    public static readonly StyledProperty<Control?> RightContentProperty =
        AvaloniaProperty.Register<SideBySideComparisonPage, Control?>(nameof(RightContent));

    public static readonly StyledProperty<string> LeftHeaderProperty =
        AvaloniaProperty.Register<SideBySideComparisonPage, string>(nameof(LeftHeader), "Avalonia Control");

    public static readonly StyledProperty<string> RightHeaderProperty =
        AvaloniaProperty.Register<SideBySideComparisonPage, string>(nameof(RightHeader), "Widget Preview");

    public SideBySideComparisonPage()
    {
        InitializeComponent();
    }

    public Control? LeftContent
    {
        get => GetValue(LeftContentProperty);
        set => SetValue(LeftContentProperty, value);
    }

    public Control? RightContent
    {
        get => GetValue(RightContentProperty);
        set => SetValue(RightContentProperty, value);
    }

    public string LeftHeader
    {
        get => GetValue(LeftHeaderProperty);
        set => SetValue(LeftHeaderProperty, value);
    }

    public string RightHeader
    {
        get => GetValue(RightHeaderProperty);
        set => SetValue(RightHeaderProperty, value);
    }
}
