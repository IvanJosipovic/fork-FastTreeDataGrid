using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using FastTreeDataGrid.Demo.ViewModels.Widgets;

namespace FastTreeDataGrid.Demo.ViewModels;

public sealed class WidgetsGalleryScenario
{
    public WidgetsGalleryScenario(string title, string summary, IEnumerable<string> highlights, IEnumerable<WidgetBoard> boards)
    {
        Title = title;
        Summary = summary;
        Highlights = highlights.ToImmutableArray();
        Boards = boards.ToImmutableArray();
    }

    public string Title { get; }

    public string Summary { get; }

    public IReadOnlyList<string> Highlights { get; }

    public IReadOnlyList<WidgetBoard> Boards { get; }
}
