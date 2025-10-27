using System;
using Avalonia.Controls;
using Avalonia.Markup.Xaml.Templates;
using Avalonia.Metadata;

namespace FastTreeDataGrid.Control.Widgets;

/// <summary>
/// Provides a XAML-friendly template that can materialize <see cref="Widget"/> instances.
/// </summary>
public sealed class WidgetTemplate : IWidgetTemplate
{
    private object? _content;

    /// <summary>
    /// Gets or sets the template content.
    /// </summary>
    [Content]
    [TemplateContent(TemplateResultType = typeof(Widget))]
    public object? Content
    {
        get => _content;
        set => _content = value;
    }

    /// <inheritdoc />
    public Widget? Build()
    {
        if (_content is null)
        {
            return null;
        }

        if (_content is Func<Widget?> factory)
        {
            return factory();
        }

        if (_content is Widget)
        {
            throw new InvalidOperationException("WidgetTemplate cannot reuse a widget instance. Provide a template or use FuncWidgetTemplate instead.");
        }

        var (result, _) = TemplateContent.Load<Widget>(_content);
        return result;
    }
}
