using System;

namespace FastTreeDataGrid.Control.Infrastructure;

public sealed class FastTreeDataGridLoadingStateEventArgs : EventArgs
{
    public FastTreeDataGridLoadingStateEventArgs(int requestId, int inFlightCount, int completedCount, int targetCount, double progress)
    {
        RequestId = requestId;
        InFlightCount = inFlightCount;
        CompletedCount = completedCount;
        TargetCount = targetCount;
        Progress = progress;
    }

    public int RequestId { get; }

    public int InFlightCount { get; }

    public int CompletedCount { get; }

    public int TargetCount { get; }

    public double Progress { get; }

    public bool IsLoading => InFlightCount > 0;
}
