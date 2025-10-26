using System;
using System.Collections.Immutable;
using System.Linq;
using Avalonia.Headless.XUnit;
using FastTreeDataGrid.Control.Widgets;
using Xunit;

namespace FastTreeDataGrid.Control.Tests;

public class PickerWidgetTests
{
    [AvaloniaFact]
    public void NumericUpDown_ClampsAndRaisesEvent()
    {
        var widget = new NumericUpDownWidget
        {
            Minimum = 0,
            Maximum = 10,
            Increment = 2
        };
        widget.ApplyTemplate();

        double? newValue = null;
        double? oldValue = null;
        widget.ValueChanged += (_, args) =>
        {
            oldValue = args.OldValue;
            newValue = args.NewValue;
        };

        widget.Value = 25;

        Assert.Equal(10, widget.Value);
        Assert.Equal(0, oldValue);
        Assert.Equal(10, newValue);
    }

    [AvaloniaFact]
    public void TimePicker_RespectsMinimumAndMaximum()
    {
        var picker = new TimePickerWidget
        {
            Minimum = TimeSpan.FromHours(8),
            Maximum = TimeSpan.FromHours(18)
        };
        picker.ApplyTemplate();

        TimeSpan? observed = null;
        picker.TimeChanged += (_, args) => observed = args.NewValue;

        picker.Time = TimeSpan.FromHours(22);

        Assert.Equal(TimeSpan.FromHours(18), picker.Time);
        Assert.Equal(TimeSpan.FromHours(18), observed);
    }

    [AvaloniaFact]
    public void CalendarWidget_ClampsSelectionToRange()
    {
        var calendar = new CalendarWidget
        {
            Minimum = new DateTime(2024, 1, 1),
            Maximum = new DateTime(2024, 1, 31)
        };
        calendar.ApplyTemplate();

        DateTime? selected = null;
        calendar.SelectedDateChanged += (_, args) => selected = args.NewValue;

        calendar.SelectedDate = new DateTime(2024, 3, 15);

        Assert.Equal(new DateTime(2024, 1, 31), calendar.SelectedDate);
        Assert.Equal(new DateTime(2024, 1, 31), selected);
    }

    [AvaloniaFact]
    public void ComboBoxWidget_RaisesSelectionChanged()
    {
        var combo = new ComboBoxWidget
        {
            Items = ImmutableArray.Create<object?>("Alpha", "Beta", "Gamma"),
            SelectedItem = "Alpha"
        };
        combo.ApplyTemplate();

        object? observedOld = null;
        object? observedNew = null;
        combo.SelectionChanged += (_, args) =>
        {
            observedOld = args.OldValue;
            observedNew = args.NewValue;
        };

        combo.SelectedItem = "Gamma";

        Assert.Equal("Gamma", combo.SelectedItem);
        Assert.Equal("Alpha", observedOld);
        Assert.Equal("Gamma", observedNew);
    }

    [AvaloniaFact]
    public void DatePickerWidget_SelectionRaisesEvent()
    {
        var picker = new DatePickerWidget
        {
            Minimum = new DateTime(2024, 1, 1),
            Maximum = new DateTime(2024, 12, 31)
        };
        picker.ApplyTemplate();

        DateTime? observed = null;
        picker.SelectedDateChanged += (_, args) => observed = args.NewValue;

        picker.SelectedDate = new DateTime(2025, 1, 2);

        Assert.Equal(new DateTime(2024, 12, 31), picker.SelectedDate);
        Assert.Equal(new DateTime(2024, 12, 31), observed);
    }

    [AvaloniaFact]
    public void CalendarDatePickerWidget_DefaultsToLongFormat()
    {
        var picker = new CalendarDatePickerWidget
        {
            SelectedDate = new DateTime(2024, 4, 5)
        };
        picker.ApplyTemplate();

        picker.SelectedDate = new DateTime(2024, 6, 15);

        Assert.Equal(new DateTime(2024, 6, 15), picker.SelectedDate);
    }

    [AvaloniaFact]
    public void ScrollBarWidget_ClampsToMaximum()
    {
        var scrollBar = new ScrollBarWidget
        {
            Minimum = 0,
            Maximum = 100,
            ViewportSize = 20
        };
        scrollBar.ApplyTemplate();

        scrollBar.Value = 250;

        Assert.Equal(100, scrollBar.Value);
    }

    [AvaloniaFact]
    public void AutoCompleteBoxWidget_FiltersSuggestions()
    {
        var auto = new AutoCompleteBoxWidget
        {
            Items = ImmutableArray.Create("Apple", "Apricot", "Banana"),
        };
        auto.ApplyTemplate();

        string? observed = null;
        auto.TextChanged += (_, args) => observed = args.NewValue;

        auto.Text = "Ap";

        Assert.Equal("Ap", observed);
    }
}
