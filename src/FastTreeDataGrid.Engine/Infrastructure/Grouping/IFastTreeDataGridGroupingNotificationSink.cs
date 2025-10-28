namespace FastTreeDataGrid.Engine.Infrastructure;

public interface IFastTreeDataGridGroupingNotificationSink
{
    void OnGroupingStateChanged(FastTreeDataGridGroupingStateChangedEventArgs args);
}
