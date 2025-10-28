using System;

namespace FastTreeDataGrid.ExcelDemo.Models;

public sealed record SalesRecord(
    int TransactionId,
    string Region,
    string Country,
    string Segment,
    string Product,
    string Salesperson,
    DateTime Date,
    double Sales,
    double Cost,
    int Units);
