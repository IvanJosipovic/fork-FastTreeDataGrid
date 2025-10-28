using System;
using System.Collections;
using Avalonia;
using FastTreeDataGrid.Engine.Infrastructure;
using FastTreeDataGrid.Control.Theming;

namespace FastTreeDataGrid.Control.Widgets;

public sealed class ContextMenuWidget : BorderWidget
{
    private readonly MenuWidget _menu;
    private Rect? _currentAnchor;

    public ContextMenuWidget()
    {
        ClipToBounds = true;

        _menu = new MenuWidget();
        _menu.ItemInvoked += (sender, args) => ItemInvoked?.Invoke(this, args);
        Child = _menu;

        ApplyPalette(WidgetFluentPalette.Current);
    }

    public event EventHandler<MenuItemInvokedEventArgs>? ItemInvoked;

    public MenuWidget Menu => _menu;

    public bool IsOpen { get; private set; }

    public override void UpdateValue(IFastTreeDataGridValueProvider? provider, object? item)
    {
        base.UpdateValue(provider, item);

        object? payload;
        if (provider is not null && Key is not null)
        {
            payload = provider.GetValue(item, Key);
        }
        else
        {
            payload = item;
        }

        ApplyValue(payload);
    }

    private void ApplyValue(object? value)
    {
        switch (value)
        {
            case ContextMenuWidgetValue context:
                if (context.ItemsSource is not null)
                {
                    _menu.UpdateValue(null, new MenuWidgetValue(context.ItemsSource, context.Interaction));
                }

                IsEnabled = context.Interaction?.IsEnabled ?? true;

                if (!IsEnabled)
                {
                    Close();
                    break;
                }

                if (context.IsOpen)
                {
                    if (context.Anchor is { } anchorRect)
                    {
                        ShowAt(anchorRect);
                    }
                    else if (_currentAnchor is { } existing)
                    {
                        ShowAt(existing);
                    }
                    else
                    {
                        IsOpen = true;
                    }
                }
                else
                {
                    Close();
                }
                break;
            case IEnumerable enumerable and not string:
                _menu.UpdateValue(null, new MenuWidgetValue(enumerable));
                IsOpen = true;
                break;
        }
    }

    private void ApplyPalette(WidgetFluentPalette.PaletteData palette)
    {
        var menuPalette = palette.Menu;
        Background = menuPalette.PresenterBackground ?? Background;
        BorderBrush = menuPalette.PresenterBorder;
        BorderThickness = menuPalette.PresenterBorderThickness;
    }

    public void ShowAt(Rect anchor)
    {
        ShowOverlay(anchor);
    }

    public void Close()
    {
        CloseOverlay();
    }

    private void ShowOverlay(Rect anchor)
    {
        _currentAnchor = anchor;

        var options = new WidgetOverlayOptions(
            Owner: this,
            CloseOnPointerDownOutside: true,
            CloseOnEscape: true,
            Offset: new Thickness(0, 2, 0, 0),
            OnClosed: _ => OnOverlayClosed());

        WidgetOverlayManager.ShowOverlay(this, anchor, WidgetOverlayPlacement.BottomStart, options);
        IsOpen = true;
    }

    private void CloseOverlay()
    {
        if (!IsOpen)
        {
            return;
        }

        WidgetOverlayManager.HideOverlay(this);
        IsOpen = false;
    }

    private void OnOverlayClosed()
    {
        IsOpen = false;
        _currentAnchor = null;
    }
}
