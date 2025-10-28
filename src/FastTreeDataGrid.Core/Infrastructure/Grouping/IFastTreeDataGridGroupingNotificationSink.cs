namespace FastTreeDataGrid.Control.Infrastructure;

public interface IFastTreeDataGridGroupingNotificationSink
{
    void OnGroupingStateChanged(FastTreeDataGridGroupingStateChangedEventArgs args);
}
