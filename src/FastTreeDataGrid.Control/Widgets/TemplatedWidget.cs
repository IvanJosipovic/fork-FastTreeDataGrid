using System;
using Avalonia;
using Avalonia.Media;
using FastTreeDataGrid.Control.Infrastructure;

namespace FastTreeDataGrid.Control.Widgets;

/// <summary>
/// Provides a lightweight templated widget similar to Avalonia's <see cref="Avalonia.Controls.TemplatedControl"/>.
/// </summary>
public abstract class TemplatedWidget : SurfaceWidget
{
    private Widget? _templateRoot;
    private bool _templateApplied;
    private Func<TemplatedWidget, Widget?>? _templateBuilder;

    public Widget? TemplateRoot => _templateRoot;

    public Func<TemplatedWidget, Widget?>? Template
    {
        get => _templateBuilder;
        set
        {
            if (ReferenceEquals(_templateBuilder, value))
            {
                return;
            }

            _templateBuilder = value;
            InvalidateTemplate();
        }
    }

    protected bool IsTemplateApplied => _templateApplied;

    protected void InvalidateTemplate()
    {
        if (_templateRoot is not null)
        {
            Children.Remove(_templateRoot);
            _templateRoot = null;
        }

        _templateApplied = false;
    }

    public bool ApplyTemplate()
    {
        var root = BuildTemplate();
        SetTemplateRoot(root);

        _templateApplied = true;
        OnTemplateApplied(_templateRoot);
        return _templateRoot is not null;
    }

    protected void SetTemplateRoot(Widget? root)
    {
        if (!ReferenceEquals(_templateRoot, root) && _templateRoot is not null)
        {
            Children.Remove(_templateRoot);
        }

        _templateRoot = root;

        if (_templateRoot is not null && !Children.Contains(_templateRoot))
        {
            Children.Add(_templateRoot);
        }

        if (_templateRoot is not null)
        {
            _templateRoot.DesiredWidth = DesiredWidth;
            _templateRoot.DesiredHeight = DesiredHeight;
        }
    }

    protected virtual Widget? CreateDefaultTemplate() => null;

    protected virtual void OnTemplateApplied(Widget? templateRoot)
    {
    }

    private Widget? BuildTemplate()
    {
        if (_templateBuilder is not null)
        {
            return _templateBuilder(this);
        }

        return CreateDefaultTemplate();
    }

    private void EnsureTemplate()
    {
        if (_templateApplied)
        {
            return;
        }

        ApplyTemplate();
    }

    public override void UpdateValue(IFastTreeDataGridValueProvider? provider, object? item)
    {
        EnsureTemplate();
        base.UpdateValue(provider, item);
    }

    public override void Arrange(Rect bounds)
    {
        EnsureTemplate();
        base.Arrange(bounds);

        if (_templateRoot is not null)
        {
            _templateRoot.DesiredWidth = DesiredWidth;
            _templateRoot.DesiredHeight = DesiredHeight;
        }

        _templateRoot?.Arrange(bounds);
    }

    public override void Draw(DrawingContext context)
    {
        EnsureTemplate();
        base.Draw(context);
    }

    public override bool HandlePointerEvent(in WidgetPointerEvent e)
    {
        EnsureTemplate();
        return base.HandlePointerEvent(e);
    }

    public override bool HandleKeyboardEvent(in WidgetKeyboardEvent e)
    {
        EnsureTemplate();
        return base.HandleKeyboardEvent(e);
    }
}
