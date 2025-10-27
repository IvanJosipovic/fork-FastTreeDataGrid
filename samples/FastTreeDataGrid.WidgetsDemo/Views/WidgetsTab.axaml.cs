using System;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using FastTreeDataGrid.Control.Controls;
using FastTreeDataGrid.Control.Models;
using FastTreeDataGrid.Control.Widgets;
using FastTreeDataGrid.Control.Widgets.Samples;
using GridControl = FastTreeDataGrid.Control.Controls.FastTreeDataGrid;

namespace FastTreeDataGrid.WidgetsDemo.Views;

public partial class WidgetsTab : UserControl
{
    public WidgetsTab()
    {
        InitializeComponent();

        if (this.FindControl<GridControl>("WidgetsGrid") is { } grid)
        {
            ConfigureWidgetColumns(grid);
        }
    }

    private static void ConfigureWidgetColumns(GridControl grid)
    {
        grid.Columns.Clear();
        grid.IndentWidth = 18;
        grid.RowHeight = 36;

        grid.Columns.Add(new FastTreeDataGridColumn
        {
            Header = "Name",
            SizingMode = ColumnSizingMode.Pixel,
            PixelWidth = 200,
            MinWidth = 160,
            ValueKey = WidgetGalleryNode.KeyName,
            IsHierarchy = true,
        });

        grid.Columns.Add(new FastTreeDataGridColumn
        {
            Header = "Description",
            SizingMode = ColumnSizingMode.Star,
            MinWidth = 220,
            ValueKey = WidgetGalleryNode.KeyDescription,
        });

        grid.Columns.Add(new FastTreeDataGridColumn
        {
            Header = "Icon",
            SizingMode = ColumnSizingMode.Pixel,
            PixelWidth = 80,
            MinWidth = 70,
            ValueKey = WidgetGalleryNode.KeyIcon,
            WidgetFactory = (_, _) => new IconWidget
            {
                Key = WidgetGalleryNode.KeyIcon,
                Foreground = new ImmutableSolidColorBrush(Color.FromRgb(49, 130, 206)),
                Padding = 10,
            },
        });

        grid.Columns.Add(new FastTreeDataGridColumn
        {
            Header = "Geometry",
            SizingMode = ColumnSizingMode.Pixel,
            PixelWidth = 100,
            MinWidth = 90,
            ValueKey = WidgetGalleryNode.KeyGeometry,
            WidgetFactory = (_, _) => new GeometryWidget
            {
                Key = WidgetGalleryNode.KeyGeometry,
                Foreground = new ImmutableSolidColorBrush(Color.FromRgb(28, 124, 172)),
                Padding = 10,
            },
        });

        grid.Columns.Add(new FastTreeDataGridColumn
        {
            Header = "Button",
            SizingMode = ColumnSizingMode.Pixel,
            PixelWidth = 120,
            MinWidth = 110,
            ValueKey = WidgetGalleryNode.KeyButton,
            WidgetFactory = (_, _) => new ButtonWidget
            {
                Key = WidgetGalleryNode.KeyButton,
                Foreground = new ImmutableSolidColorBrush(Color.FromRgb(40, 40, 40)),
                Background = new ImmutableSolidColorBrush(Color.FromRgb(242, 242, 242)),
            },
        });

        grid.Columns.Add(new FastTreeDataGridColumn
        {
            Header = "CheckBox",
            SizingMode = ColumnSizingMode.Pixel,
            PixelWidth = 90,
            MinWidth = 80,
            ValueKey = WidgetGalleryNode.KeyCheckBox,
            WidgetFactory = (_, _) => new CheckBoxWidget
            {
                Key = WidgetGalleryNode.KeyCheckBox,
                Foreground = new ImmutableSolidColorBrush(Color.FromRgb(49, 130, 206)),
            },
        });

        grid.Columns.Add(new FastTreeDataGridColumn
        {
            Header = "Toggle",
            SizingMode = ColumnSizingMode.Pixel,
            PixelWidth = 110,
            MinWidth = 100,
            ValueKey = WidgetGalleryNode.KeyToggle,
            WidgetFactory = (_, item) =>
            {
                var toggle = new ToggleSwitchWidget
                {
                    Key = WidgetGalleryNode.KeyToggle,
                };

                if (item is WidgetGalleryNode node)
                {
                    toggle.Toggled += (_, args) =>
                    {
                        var isOn = args.NewValue;
                        if (node.ToggleValue is ToggleSwitchWidgetValue current)
                        {
                            node.ToggleValue = current with { IsOn = isOn };
                        }
                        else
                        {
                            node.ToggleValue = new ToggleSwitchWidgetValue(isOn);
                        }
                    };
                }

                return toggle;
            },
        });

        grid.Columns.Add(new FastTreeDataGridColumn
        {
            Header = "Numeric",
            SizingMode = ColumnSizingMode.Pixel,
            PixelWidth = 110,
            MinWidth = 100,
            ValueKey = WidgetGalleryNode.KeyNumeric,
            WidgetFactory = (_, _) => new NumericUpDownWidget
            {
                Key = WidgetGalleryNode.KeyNumeric,
                DesiredWidth = 100,
            },
        });

        grid.Columns.Add(new FastTreeDataGridColumn
        {
            Header = "Calendar",
            SizingMode = ColumnSizingMode.Pixel,
            PixelWidth = 220,
            MinWidth = 200,
            ValueKey = WidgetGalleryNode.KeyCalendar,
            WidgetFactory = (_, _) => new CalendarWidget
            {
                Key = WidgetGalleryNode.KeyCalendar,
                DesiredWidth = 220,
                DesiredHeight = 220,
            },
        });

        grid.Columns.Add(new FastTreeDataGridColumn
        {
            Header = "DatePicker",
            SizingMode = ColumnSizingMode.Pixel,
            PixelWidth = 200,
            MinWidth = 180,
            ValueKey = WidgetGalleryNode.KeyDatePicker,
            WidgetFactory = (_, _) => new DatePickerWidget
            {
                Key = WidgetGalleryNode.KeyDatePicker,
                DesiredWidth = 180,
            },
        });

        grid.Columns.Add(new FastTreeDataGridColumn
        {
            Header = "ScrollBar",
            SizingMode = ColumnSizingMode.Pixel,
            PixelWidth = 90,
            MinWidth = 80,
            ValueKey = WidgetGalleryNode.KeyScrollBar,
            WidgetFactory = (_, _) => new ScrollBarWidget
            {
                Key = WidgetGalleryNode.KeyScrollBar,
                DesiredWidth = 22,
                DesiredHeight = 140,
            },
        });

        grid.Columns.Add(new FastTreeDataGridColumn
        {
            Header = "CalendarDatePicker",
            SizingMode = ColumnSizingMode.Pixel,
            PixelWidth = 200,
            MinWidth = 180,
            ValueKey = WidgetGalleryNode.KeyCalendarDatePicker,
            WidgetFactory = (_, _) => new CalendarDatePickerWidget
            {
                Key = WidgetGalleryNode.KeyCalendarDatePicker,
                DesiredWidth = 180,
            },
        });

        grid.Columns.Add(new FastTreeDataGridColumn
        {
            Header = "TimePicker",
            SizingMode = ColumnSizingMode.Pixel,
            PixelWidth = 180,
            MinWidth = 160,
            ValueKey = WidgetGalleryNode.KeyTimePicker,
            WidgetFactory = (_, _) => new TimePickerWidget
            {
                Key = WidgetGalleryNode.KeyTimePicker,
                DesiredWidth = 160,
            },
        });

        grid.Columns.Add(new FastTreeDataGridColumn
        {
            Header = "AutoComplete",
            SizingMode = ColumnSizingMode.Pixel,
            PixelWidth = 200,
            MinWidth = 180,
            ValueKey = WidgetGalleryNode.KeyAutoComplete,
            WidgetFactory = (_, _) => new AutoCompleteBoxWidget
            {
                Key = WidgetGalleryNode.KeyAutoComplete,
                DesiredWidth = 200,
            },
        });

        grid.Columns.Add(new FastTreeDataGridColumn
        {
            Header = "ComboBox",
            SizingMode = ColumnSizingMode.Pixel,
            PixelWidth = 200,
            MinWidth = 180,
            ValueKey = WidgetGalleryNode.KeyComboBox,
            WidgetFactory = (_, _) => new ComboBoxWidget
            {
                Key = WidgetGalleryNode.KeyComboBox,
                DesiredWidth = 200,
            },
        });

        grid.Columns.Add(new FastTreeDataGridColumn
        {
            Header = "Badge",
            SizingMode = ColumnSizingMode.Pixel,
            PixelWidth = 120,
            MinWidth = 100,
            ValueKey = WidgetGalleryNode.KeyBadge,
            WidgetFactory = (_, _) => new BadgeWidget
            {
                Key = WidgetGalleryNode.KeyBadge,
            },
        });

        grid.Columns.Add(new FastTreeDataGridColumn
        {
            Header = "Progress",
            SizingMode = ColumnSizingMode.Pixel,
            PixelWidth = 140,
            MinWidth = 120,
            ValueKey = WidgetGalleryNode.KeyProgress,
            WidgetFactory = (_, _) => new ProgressWidget
            {
                Key = WidgetGalleryNode.KeyProgress,
                Foreground = new ImmutableSolidColorBrush(Color.FromRgb(49, 130, 206)),
            },
        });

        grid.Columns.Add(new FastTreeDataGridColumn
        {
            Header = "Custom Draw",
            SizingMode = ColumnSizingMode.Pixel,
            PixelWidth = 140,
            MinWidth = 120,
            ValueKey = WidgetGalleryNode.KeyCustom,
            WidgetFactory = (_, _) => new CustomDrawWidget
            {
                Key = WidgetGalleryNode.KeyCustom,
            },
        });

        grid.Columns.Add(new FastTreeDataGridColumn
        {
            Header = "Layout",
            SizingMode = ColumnSizingMode.Pixel,
            PixelWidth = 220,
            MinWidth = 200,
            ValueKey = WidgetGalleryNode.KeyLayout,
            WidgetFactory = (provider, item) =>
            {
                if (item is WidgetGalleryNode node && node.LayoutFactory is not null)
                {
                    return node.LayoutFactory();
                }

                if (provider?.GetValue(item, WidgetGalleryNode.KeyLayout) is Func<Widget> builder && builder is not null)
                {
                    return builder();
                }

                return null;
            },
        });
    }
}
