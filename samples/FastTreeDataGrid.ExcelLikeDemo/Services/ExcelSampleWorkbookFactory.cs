using System;
using System.Collections.Generic;
using System.Globalization;
using FastTreeDataGrid.ExcelLikeDemo.Models;

namespace FastTreeDataGrid.ExcelLikeDemo.Services;

public static class ExcelSampleWorkbookFactory
{
    public static WorkbookModel CreateSampleWorkbook(string name = "Sample Workbook", int worksheetCount = 3, int rowsPerSheet = 1500, int columns = 26)
    {
        var worksheets = new List<WorksheetModel>(worksheetCount);
        var random = new Random(1337);

        for (var sheetIndex = 0; sheetIndex < worksheetCount; sheetIndex++)
        {
            var rows = new List<WorksheetRowModel>(rowsPerSheet);

            for (var rowIndex = 0; rowIndex < rowsPerSheet; rowIndex++)
            {
                var cells = new Dictionary<int, WorksheetCellModel>();
                var row = new WorksheetRowModel(rowIndex, cells);

                for (var columnIndex = 0; columnIndex < columns; columnIndex++)
                {
                    var cell = row.GetOrAddCell(columnIndex);
                    var value = GenerateValue(random, rowIndex, columnIndex, sheetIndex);
                    cell.SetFromExcelValue(value.Display, value.Raw, formula: null);
                }

                rows.Add(row);
            }

            var headers = new List<string>(columns);
            for (var columnIndex = 0; columnIndex < columns; columnIndex++)
            {
                headers.Add(ExcelAddressHelper.ToColumnName(columnIndex));
            }

            worksheets.Add(new WorksheetModel($"Sheet {sheetIndex + 1}", rows, headers));
        }

        return new WorkbookModel(name, worksheets);
    }

    private static (string Display, string Raw) GenerateValue(Random random, int rowIndex, int columnIndex, int sheetIndex)
    {
        var selector = (rowIndex + columnIndex + sheetIndex) % 4;

        switch (selector)
        {
            case 0:
                var numeric = Math.Round(random.NextDouble() * 10000, 2);
                var formatted = numeric.ToString("N2", CultureInfo.InvariantCulture);
                return (formatted, numeric.ToString(CultureInfo.InvariantCulture));
            case 1:
                var date = DateTime.UtcNow.Date.AddDays(-random.Next(0, 3650));
                var dateText = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                return (dateText, dateText);
            case 2:
                var words = SampleWords[random.Next(SampleWords.Length)];
                return (words, words);
            default:
                var percentage = random.NextDouble();
                var text = percentage.ToString("P1", CultureInfo.InvariantCulture);
                return (text, percentage.ToString(CultureInfo.InvariantCulture));
        }
    }

    private static readonly string[] SampleWords =
    {
        "Earnings",
        "Inventory",
        "Forecast",
        "Momentum",
        "Strategy",
        "Customer",
        "Engagement",
        "Pipeline",
        "Capacity",
        "Schedule",
        "Operations",
        "Quality",
        "Variance",
        "Launch",
        "Sustainability",
        "Compliance",
        "Productivity",
        "Velocity",
        "Analytics",
        "Baseline",
    };
}
