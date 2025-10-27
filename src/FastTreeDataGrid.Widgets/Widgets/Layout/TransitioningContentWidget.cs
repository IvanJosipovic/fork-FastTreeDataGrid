using System;
using Avalonia;
using Avalonia.Media;
using FastTreeDataGrid.Control.Infrastructure;
using FastTreeDataGrid.Control.Theming;

namespace FastTreeDataGrid.Control.Widgets;

public sealed class TransitioningContentWidget : SurfaceWidget
{
    private Widget? _currentContent;
    private Widget? _previousContent;
    private DateTime _transitionStart;
    private bool _isAnimating;
    private WidgetTransitionDescriptor _transition = WidgetTransitionDescriptor.Fade(TimeSpan.FromMilliseconds(200));

    public WidgetTransitionDescriptor Transition
    {
        get => _transition;
        set => _transition = value;
    }

    public Widget? Content
    {
        get => _currentContent;
        set => SetContent(value);
    }

    public override void UpdateValue(IFastTreeDataGridValueProvider? provider, object? item)
    {
        base.UpdateValue(provider, item);

        if (provider is null || Key is null)
        {
            return;
        }

        var value = provider.GetValue(item, Key);
        ApplyValue(value);
    }

    public override void Arrange(Rect bounds)
    {
        base.Arrange(bounds);

        _previousContent?.Arrange(bounds);
        _currentContent?.Arrange(bounds);
    }

    public override void Draw(DrawingContext context)
    {
        using var clip = PushClip(context);

        if (!_isAnimating || _previousContent is null)
        {
            _previousContent = null;
            _isAnimating = false;
            _currentContent?.Draw(context);
            return;
        }

        var progress = GetProgress();
        DrawTransition(context, progress);

        if (progress >= 1)
        {
            CompleteTransition();
        }
        else
        {
            WidgetAnimationFrameScheduler.RequestFrame();
        }
    }

    private void ApplyValue(object? value)
    {
        switch (value)
        {
            case TransitioningContentWidgetValue descriptor:
                ApplyDescriptor(descriptor);
                break;
            case IWidgetTemplate template:
                SetContent(template.Build());
                break;
            case Func<Widget?> factory:
                SetContent(factory());
                break;
            case Widget widget:
                SetContent(widget);
                break;
            case string text:
                var body = WidgetFluentPalette.Current.Text.Typography.Body;
                var formatted = new FormattedTextWidget
                {
                    EmSize = body.FontSize > 0 ? body.FontSize : 12
                };
                if (body.FontFamily is not null)
                {
                    formatted.FontFamily = body.FontFamily;
                }

                formatted.FontWeight = body.FontWeight;
                formatted.SetText(text);
                SetContent(formatted);
                break;
            default:
                break;
        }
    }

    private void ApplyDescriptor(TransitioningContentWidgetValue descriptor)
    {
        if (descriptor.Transition is { } transition)
        {
            Transition = transition;
        }

        Widget? content = descriptor.Content;

        if (descriptor.ContentFactory is { } factory)
        {
            content = factory();
        }

        if (descriptor.ContentTemplate is not null)
        {
            content = descriptor.ContentTemplate.Build();
        }

        SetContent(content);
    }

    private void SetContent(Widget? widget)
    {
        if (ReferenceEquals(_currentContent, widget))
        {
            return;
        }

        var previous = _currentContent;
        _currentContent = widget;

        if (_currentContent is not null)
        {
            if (Children.Contains(_currentContent))
            {
                Children.Remove(_currentContent);
            }

            Children.Add(_currentContent);
        }

        if (previous is null || ReferenceEquals(previous, _currentContent))
        {
            _previousContent = null;
            _isAnimating = false;
            if (previous is not null && !ReferenceEquals(previous, _currentContent))
            {
                Children.Remove(previous);
            }
            return;
        }

        StartTransition(previous);
    }

    private void StartTransition(Widget previous)
    {
        _previousContent = previous;
        if (!Children.Contains(previous))
        {
            Children.Insert(0, previous);
        }

        var duration = Transition.Duration <= TimeSpan.Zero
            ? TimeSpan.Zero
            : Transition.Duration;

        if (duration == TimeSpan.Zero)
        {
            Children.Remove(previous);
            _previousContent = null;
            _isAnimating = false;
            return;
        }

        _transitionStart = DateTime.UtcNow;
        _isAnimating = true;
        WidgetAnimationFrameScheduler.RequestFrame();
    }

    private double GetProgress()
    {
        var duration = Transition.Duration;
        if (duration <= TimeSpan.Zero)
        {
            return 1;
        }

        var elapsed = DateTime.UtcNow - _transitionStart;
        var progress = elapsed.TotalMilliseconds / duration.TotalMilliseconds;
        return Math.Min(1, Math.Max(0, progress));
    }

    private void DrawTransition(DrawingContext context, double progress)
    {
        var eased = progress;

        switch (Transition.Kind)
        {
            case WidgetTransitionKind.Fade:
                DrawFade(context, eased);
                break;
            case WidgetTransitionKind.SlideLeft:
            case WidgetTransitionKind.SlideRight:
            case WidgetTransitionKind.SlideUp:
            case WidgetTransitionKind.SlideDown:
                DrawSlide(context, eased, Transition.Kind);
                break;
            default:
                DrawFade(context, eased);
                break;
        }
    }

    private void DrawFade(DrawingContext context, double progress)
    {
        if (_previousContent is not null)
        {
            using (context.PushOpacity(Math.Max(0, 1 - progress)))
            {
                _previousContent.Draw(context);
            }
        }

        if (_currentContent is not null)
        {
            using (context.PushOpacity(Math.Max(0, progress)))
            {
                _currentContent.Draw(context);
            }
        }
    }

    private void DrawSlide(DrawingContext context, double progress, WidgetTransitionKind kind)
    {
        if (_previousContent is not null)
        {
            using (context.PushTransform(CreateSlideTransform(kind, 1 - progress)))
            using (context.PushOpacity(Math.Max(0, 1 - progress)))
            {
                _previousContent.Draw(context);
            }
        }

        if (_currentContent is not null)
        {
            using (context.PushTransform(CreateSlideTransform(kind, progress - 1)))
            using (context.PushOpacity(Math.Max(0, progress)))
            {
                _currentContent.Draw(context);
            }
        }
    }

    private Matrix CreateSlideTransform(WidgetTransitionKind kind, double progressOffset)
    {
        var distance = Transition.SlideOffset;
        var offsetX = 0d;
        var offsetY = 0d;

        switch (kind)
        {
            case WidgetTransitionKind.SlideLeft:
                offsetX = -distance * progressOffset;
                break;
            case WidgetTransitionKind.SlideRight:
                offsetX = distance * progressOffset;
                break;
            case WidgetTransitionKind.SlideUp:
                offsetY = -distance * progressOffset;
                break;
            case WidgetTransitionKind.SlideDown:
                offsetY = distance * progressOffset;
                break;
        }

        return Matrix.CreateTranslation(offsetX, offsetY);
    }

    private void CompleteTransition()
    {
        if (_previousContent is not null)
        {
            Children.Remove(_previousContent);
        }

        _previousContent = null;
        _isAnimating = false;
    }
}
