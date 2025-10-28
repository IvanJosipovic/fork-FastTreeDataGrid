using System.Collections.Generic;

namespace FastTreeDataGrid.GroupingDemo.ViewModels;

public sealed class MainWindowViewModel
{
    public GroupingShowcaseViewModel Showcase { get; } = new GroupingShowcaseViewModel();

    public IReadOnlyList<GroupPreset> Presets => Showcase.Presets;
}
