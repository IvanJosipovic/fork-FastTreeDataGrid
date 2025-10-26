using System.Globalization;

namespace FastTreeDataGrid.Control.Widgets;

public sealed class CalendarDatePickerWidget : DatePickerWidget
{
    public CalendarDatePickerWidget()
    {
        FormatString = "MMMM dd, yyyy";
    }

    public void SetCulture(CultureInfo culture)
    {
        Culture = culture;
    }
}
