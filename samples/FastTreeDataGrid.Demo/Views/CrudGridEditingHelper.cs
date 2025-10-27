using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using FastTreeDataGrid.Control.Controls;
using FTGrid = FastTreeDataGrid.Control.Controls.FastTreeDataGrid;

namespace FastTreeDataGrid.Demo.Views;

internal static class CrudGridEditingHelper
{
    public static void Attach(FTGrid? grid)
    {
        if (grid is null)
        {
            return;
        }

        grid.AddHandler(InputElement.DoubleTappedEvent, OnGridDoubleTapped, RoutingStrategies.Bubble, handledEventsToo: false);
        grid.AddHandler(InputElement.KeyDownEvent, OnGridKeyDown, RoutingStrategies.Tunnel, handledEventsToo: false);
    }

    private static void OnGridDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is not FTGrid grid)
        {
            return;
        }

        // Rely on the grid to ignore edits for read-only cells or hierarchy toggles.
        if (!grid.IsKeyboardFocusWithin)
        {
            grid.Focus();
        }

        if (grid.BeginEdit())
        {
            e.Handled = true;
        }
    }

    private static void OnGridKeyDown(object? sender, KeyEventArgs e)
    {
        if (sender is not FTGrid grid || grid.IsEditing)
        {
            return;
        }

        var noModifiers = e.KeyModifiers == KeyModifiers.None;

        switch (e.Key)
        {
            case Key.F2:
                if (!grid.IsKeyboardFocusWithin)
                {
                    grid.Focus();
                }

                if (grid.BeginEdit())
                {
                    e.Handled = true;
                }
                break;
            case Key.Enter when noModifiers:
                if (!grid.IsKeyboardFocusWithin)
                {
                    grid.Focus();
                }

                if (grid.BeginEdit())
                {
                    e.Handled = true;
                }
                break;
        }
    }
}
