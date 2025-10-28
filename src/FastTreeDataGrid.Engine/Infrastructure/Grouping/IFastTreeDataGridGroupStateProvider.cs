namespace FastTreeDataGrid.Engine.Infrastructure;

/// <summary>
/// Allows querying and updating group expansion state.
/// </summary>
public interface IFastTreeDataGridGroupStateProvider
{
    FastTreeDataGridGroupState GetState(string path);

    void SetExpanded(string path, bool isExpanded);

    void Clear();
}
