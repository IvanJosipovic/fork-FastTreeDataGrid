using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using FastTreeDataGrid.ExcelLikeDemo.Models;

namespace FastTreeDataGrid.ExcelLikeDemo.Services;

public sealed class ExcelWorkbookLoader
{
    public Task<WorkbookModel> LoadAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("File path cannot be null or whitespace.", nameof(filePath));
        }

        return Task.Run(() => LoadInternal(filePath, cancellationToken), cancellationToken);
    }

    private static WorkbookModel LoadInternal(string filePath, CancellationToken cancellationToken)
    {
        using var document = SpreadsheetDocument.Open(filePath, false);
        var workbookPart = document.WorkbookPart ?? throw new InvalidOperationException("The workbook part is missing.");
        var sharedStringTable = workbookPart.SharedStringTablePart?.SharedStringTable;
        var sheets = workbookPart.Workbook.Sheets?.Elements<Sheet>() ?? Enumerable.Empty<Sheet>();

        var worksheetModels = new List<WorksheetModel>();

        foreach (var sheet in sheets)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var relationId = sheet.Id?.Value;
            if (string.IsNullOrEmpty(relationId))
            {
                continue;
            }

            if (workbookPart.GetPartById(relationId) is not WorksheetPart worksheetPart)
            {
                continue;
            }

            var sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>();
            if (sheetData is null)
            {
                continue;
            }

            var rows = new List<WorksheetRowModel>();
            var maxColumn = 0;

            foreach (var row in sheetData.Elements<Row>())
            {
                cancellationToken.ThrowIfCancellationRequested();

                var index = row.RowIndex?.Value is null
                    ? rows.Count
                    : (int)Math.Max(0, row.RowIndex!.Value - 1);

                var rowModel = EnsureRow(rows, index);

                foreach (var cell in row.Elements<Cell>())
                {
                    var reference = cell.CellReference?.Value;
                    var columnIndex = reference is null ? 0 : ExcelAddressHelper.GetColumnIndex(reference);
                    maxColumn = Math.Max(maxColumn, columnIndex + 1);

                    var cellModel = rowModel.GetOrAddCell(columnIndex);
                    var (display, raw, formula) = ReadCellValue(cell, sharedStringTable);
                    cellModel.SetFromExcelValue(display, raw, formula);
                }
            }

            var columnHeaders = Enumerable.Range(0, maxColumn)
                .Select(ExcelAddressHelper.ToColumnName)
                .ToList();

            worksheetModels.Add(new WorksheetModel(sheet.Name?.Value ?? $"Sheet{worksheetModels.Count + 1}", rows, columnHeaders));
        }

        var workbookName = Path.GetFileNameWithoutExtension(filePath);
        return new WorkbookModel(string.IsNullOrEmpty(workbookName) ? "Workbook" : workbookName, worksheetModels);
    }

    private static WorksheetRowModel EnsureRow(List<WorksheetRowModel> rows, int index)
    {
        while (rows.Count <= index)
        {
            rows.Add(new WorksheetRowModel(rows.Count, new Dictionary<int, WorksheetCellModel>()));
        }

        return rows[index];
    }

    private static (string? Display, string? Raw, string? Formula) ReadCellValue(Cell cell, SharedStringTable? sharedStrings)
    {
        string? formula = cell.CellFormula?.Text;
        string? raw = null;
        string? display = null;

        if (cell.DataType?.Value == CellValues.SharedString)
        {
            var shared = GetSharedString(sharedStrings, cell.CellValue?.Text);
            display = shared;
            raw = shared;
        }
        else if (cell.DataType?.Value == CellValues.Boolean)
        {
            var boolean = cell.CellValue?.Text;
            display = boolean == "1" ? "TRUE" : "FALSE";
            raw = display;
        }
        else
        {
            raw = cell.CellValue?.Text;
            display = FormatCellValue(cell, raw);
        }

        if (!string.IsNullOrEmpty(formula))
        {
            // Prefer cached value for display if present, keep formula for editing.
            var cached = cell.CellValue?.Text;
            if (!string.IsNullOrEmpty(cached))
            {
                display = FormatCellValue(cell, cached);
            }
        }

        return (display, raw, formula);
    }

    private static string? GetSharedString(SharedStringTable? table, string? indexText)
    {
        if (table is null || string.IsNullOrEmpty(indexText))
        {
            return null;
        }

        if (!int.TryParse(indexText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var index))
        {
            return null;
        }

        if ((uint)index >= table.ChildElements.Count)
        {
            return null;
        }

        var item = table.ChildElements[index] as SharedStringItem;
        return item?.InnerText ?? item?.Text?.Text;
    }

    private static string? FormatCellValue(Cell cell, string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return null;
        }

        if (cell.DataType?.Value == CellValues.Number && double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var number))
        {
            return number.ToString("G", CultureInfo.InvariantCulture);
        }

        if (cell.DataType is null && DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            return date.ToString("u");
        }

        return value;
    }
}
