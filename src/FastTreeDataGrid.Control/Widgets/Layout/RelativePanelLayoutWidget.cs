using System.Collections.Generic;
using Avalonia;

namespace FastTreeDataGrid.Control.Widgets;

public sealed class RelativePanelLayoutWidget : PanelLayoutWidget
{
    private readonly Dictionary<Widget, RelativePlacement> _placements = new();
    private readonly RelativePanelLayoutAdapter _adapter;

    public RelativePanelLayoutWidget()
    {
        _adapter = new RelativePanelLayoutAdapter(_placements);
    }

    protected override IPanelLayoutAdapter Adapter => _adapter;

    protected override bool SupportsSpacing => false;

    public void SetAlignLeftWith(Widget child, Widget? target) => UpdatePlacement(child, placement => placement with { AlignLeftWith = target, AlignLeftWithPanel = false });
    public void SetAlignLeftWithPanel(Widget child, bool value) => UpdatePlacement(child, placement => placement with { AlignLeftWithPanel = value, AlignLeftWith = value ? null : placement.AlignLeftWith });

    public void SetAlignTopWith(Widget child, Widget? target) => UpdatePlacement(child, placement => placement with { AlignTopWith = target, AlignTopWithPanel = false });
    public void SetAlignTopWithPanel(Widget child, bool value) => UpdatePlacement(child, placement => placement with { AlignTopWithPanel = value, AlignTopWith = value ? null : placement.AlignTopWith });

    public void SetAlignRightWith(Widget child, Widget? target) => UpdatePlacement(child, placement => placement with { AlignRightWith = target, AlignRightWithPanel = false });
    public void SetAlignRightWithPanel(Widget child, bool value) => UpdatePlacement(child, placement => placement with { AlignRightWithPanel = value, AlignRightWith = value ? null : placement.AlignRightWith });

    public void SetAlignBottomWith(Widget child, Widget? target) => UpdatePlacement(child, placement => placement with { AlignBottomWith = target, AlignBottomWithPanel = false });
    public void SetAlignBottomWithPanel(Widget child, bool value) => UpdatePlacement(child, placement => placement with { AlignBottomWithPanel = value, AlignBottomWith = value ? null : placement.AlignBottomWith });

    public void SetLeftOf(Widget child, Widget? target) => UpdatePlacement(child, placement => placement with { LeftOf = target });
    public void SetRightOf(Widget child, Widget? target) => UpdatePlacement(child, placement => placement with { RightOf = target });
    public void SetAbove(Widget child, Widget? target) => UpdatePlacement(child, placement => placement with { Above = target });
    public void SetBelow(Widget child, Widget? target) => UpdatePlacement(child, placement => placement with { Below = target });

    public void SetLeft(Widget child, double? value) => UpdatePlacement(child, placement => placement with { Left = value });
    public void SetTop(Widget child, double? value) => UpdatePlacement(child, placement => placement with { Top = value });
    public void SetRight(Widget child, double? value) => UpdatePlacement(child, placement => placement with { Right = value });
    public void SetBottom(Widget child, double? value) => UpdatePlacement(child, placement => placement with { Bottom = value });

    public void SetMargin(Widget child, double value) => UpdatePlacement(child, placement => placement with { Margin = value });

    private void UpdatePlacement(Widget child, System.Func<RelativePlacement, RelativePlacement> updater)
    {
        var current = _placements.TryGetValue(child, out var placement) ? placement : default;
        _placements[child] = updater(current);
    }
}

internal readonly record struct RelativePlacement(
    double? Left = null,
    double? Top = null,
    double? Right = null,
    double? Bottom = null,
    Widget? AlignLeftWith = null,
    Widget? AlignTopWith = null,
    Widget? AlignRightWith = null,
    Widget? AlignBottomWith = null,
    bool AlignLeftWithPanel = false,
    bool AlignTopWithPanel = false,
    bool AlignRightWithPanel = false,
    bool AlignBottomWithPanel = false,
    Widget? LeftOf = null,
    Widget? RightOf = null,
    Widget? Above = null,
    Widget? Below = null,
    double Margin = 0);
