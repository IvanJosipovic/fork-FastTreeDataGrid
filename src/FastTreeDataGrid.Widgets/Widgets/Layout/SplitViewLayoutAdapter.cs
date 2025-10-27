using System;
using System.Collections.Generic;
using Avalonia;

namespace FastTreeDataGrid.Control.Widgets;

internal sealed class SplitViewLayoutAdapter : IPanelLayoutAdapter
{
    public double CompactPaneLength { get; set; } = 48;
    public double OpenPaneLength { get; set; } = 320;
    public SplitViewPanePlacement PanePlacement { get; set; } = SplitViewPanePlacement.Left;
    public bool IsPaneOpen { get; set; } = true;

    public void Arrange(IList<Widget> children, in PanelLayoutContext context)
    {
        if (children.Count == 0)
        {
            return;
        }

        Widget? pane = null;
        Widget? content = null;

        foreach (var child in children)
        {
            if (child.StyleKey == SplitViewLayoutWidget.PaneStyleKey)
            {
                pane = child;
            }
            else if (child.StyleKey == SplitViewLayoutWidget.ContentStyleKey)
            {
                content = child;
            }
        }

        var bounds = context.InnerBounds;

        if (pane is not null)
        {
            var paneLength = IsPaneOpen ? OpenPaneLength : CompactPaneLength;
            var paneRect = PanePlacement switch
            {
                SplitViewPanePlacement.Left => new Rect(bounds.X, bounds.Y, Math.Min(paneLength, bounds.Width), bounds.Height),
                SplitViewPanePlacement.Right => new Rect(bounds.Right - Math.Min(paneLength, bounds.Width), bounds.Y, Math.Min(paneLength, bounds.Width), bounds.Height),
                SplitViewPanePlacement.Top => new Rect(bounds.X, bounds.Y, bounds.Width, Math.Min(paneLength, bounds.Height)),
                SplitViewPanePlacement.Bottom => new Rect(bounds.X, bounds.Bottom - Math.Min(paneLength, bounds.Height), bounds.Width, Math.Min(paneLength, bounds.Height)),
                _ => new Rect(bounds.X, bounds.Y, Math.Min(paneLength, bounds.Width), bounds.Height)
            };

            pane.Arrange(paneRect);
            bounds = AdjustContentBounds(bounds, paneRect);
        }

        content?.Arrange(bounds);
    }

    private Rect AdjustContentBounds(Rect original, Rect paneRect)
    {
        return PanePlacement switch
        {
            SplitViewPanePlacement.Left => new Rect(paneRect.Right, original.Y, Math.Max(0, original.Right - paneRect.Right), original.Height),
            SplitViewPanePlacement.Right => new Rect(original.X, original.Y, Math.Max(0, paneRect.X - original.X), original.Height),
            SplitViewPanePlacement.Top => new Rect(original.X, paneRect.Bottom, original.Width, Math.Max(0, original.Bottom - paneRect.Bottom)),
            SplitViewPanePlacement.Bottom => new Rect(original.X, original.Y, original.Width, Math.Max(0, paneRect.Y - original.Y)),
            _ => new Rect(paneRect.Right, original.Y, Math.Max(0, original.Right - paneRect.Right), original.Height)
        };
    }
}

public enum SplitViewPanePlacement
{
    Left,
    Right,
    Top,
    Bottom
}
