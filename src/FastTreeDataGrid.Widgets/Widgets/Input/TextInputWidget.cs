using System;
using System.Globalization;
using System.Text;
using Avalonia;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using FastTreeDataGrid.Control.Infrastructure;

namespace FastTreeDataGrid.Control.Widgets;

public class TextInputWidget : SelectableTextWidget
{
    private static readonly TimeSpan CaretBlinkInterval = TimeSpan.FromMilliseconds(530);

    private string _text = string.Empty;
    private Rect _textBounds;
    private bool _isFocused;
    private bool _caretVisible = true;
    private DateTime _nextCaretToggle = DateTime.UtcNow + CaretBlinkInterval;
    private Thickness _padding = new Thickness(8, 6);
    private ImmutableSolidColorBrush? _normalBorderBrush;
    private ImmutableSolidColorBrush? _focusBorderBrush;

    static TextInputWidget()
    {
        WidgetStyleManager.Register(string.Empty, new WidgetStyleRule(
            typeof(TextInputWidget),
            WidgetVisualState.Normal,
            static (widget, theme) =>
            {
                if (widget is not TextInputWidget input)
                {
                    return;
                }

                var layout = theme.Palette.Layout;
                var textPalette = theme.Palette.Text;
                var border = theme.Palette.Border;

                input.Background ??= new ImmutableSolidColorBrush(Color.FromRgb(0xF9, 0xFA, 0xFB));

                var placeholder = textPalette.Placeholder.Get(WidgetVisualState.Normal);
                if (placeholder is not null && input.PlaceholderBrush is null)
                {
                    input.PlaceholderBrush = placeholder;
                }

                if (textPalette.CaretBrush is not null && input.CaretBrush is null)
                {
                    input.CaretBrush = textPalette.CaretBrush;
                }
                input.SelectionBrush = textPalette.SelectionHighlight ?? input.SelectionBrush;
                if (input.Padding == default)
                {
                    input.Padding = layout.ContentPadding;
                }

                input._normalBorderBrush = border.ControlBorder.Get(WidgetVisualState.Normal) ?? input._normalBorderBrush ?? new ImmutableSolidColorBrush(Color.FromRgb(0xCD, 0xCD, 0xCD));
                if (!input._isFocused)
                {
                    input.BorderBrush = input._normalBorderBrush;
                }
            }));

        WidgetStyleManager.Register(string.Empty, new WidgetStyleRule(
            typeof(TextInputWidget),
            WidgetVisualState.Focused,
            static (widget, theme) =>
            {
                if (widget is not TextInputWidget input)
                {
                    return;
                }

                var border = theme.Palette.Border;
                input._focusBorderBrush = border.FocusStroke ?? input._normalBorderBrush;
                input.BorderBrush = input._focusBorderBrush ?? input.BorderBrush;
            }));

        WidgetStyleManager.Register(string.Empty, new WidgetStyleRule(
            typeof(TextInputWidget),
            WidgetVisualState.Disabled,
            static (widget, theme) =>
            {
                if (widget is not TextInputWidget input)
                {
                    return;
                }

                var textPalette = theme.Palette.Text;
                input.PlaceholderBrush ??= textPalette.Placeholder.Get(WidgetVisualState.Disabled);
                input.BorderBrush = theme.Palette.Border.ControlBorder.Get(WidgetVisualState.Disabled) ?? input.BorderBrush;
            }));
    }

    public TextInputWidget()
    {
        IsInteractive = true;
        UpdateDisplayText();
    }

    public ImmutableSolidColorBrush? Background { get; set; }

    public ImmutableSolidColorBrush? BorderBrush { get; set; }

    public double BorderThickness { get; set; } = 1;

    public Thickness Padding
    {
        get => _padding;
        set
        {
            if (_padding == value)
            {
                return;
            }

            _padding = value;
            Invalidate();
        }
    }

    public new string Text
    {
        get => _text;
        set => SetTextInternal(value ?? string.Empty, true);
    }

    protected string InternalText => _text;

    public string? Placeholder { get; set; }

    public ImmutableSolidColorBrush? PlaceholderBrush { get; set; }

    public bool IsReadOnly { get; set; }

    public bool IsFocused => _isFocused;

    public event EventHandler<WidgetValueChangedEventArgs<string>>? TextChanged;

    public event EventHandler? Submitted;

    public override void UpdateValue(IFastTreeDataGridValueProvider? provider, object? item)
    {
        if (provider is not null && Key is not null)
        {
            var value = provider.GetValue(item, Key);
            if (ApplyValue(value))
            {
                return;
            }
        }

        if (ApplyValue(item))
        {
            return;
        }

        base.UpdateValue(provider, item);
        SetTextInternal(base.Text ?? string.Empty, false);
    }

    protected virtual bool ApplyValue(object? value)
    {
        switch (value)
        {
            case TextInputWidgetValue textValue:
                IsReadOnly = textValue.IsReadOnly;
                if (textValue.IsEnabled.HasValue)
                {
                    IsEnabled = textValue.IsEnabled.Value;
                }

                Placeholder = textValue.Placeholder ?? Placeholder;
                SetTextInternal(textValue.Text ?? string.Empty, false);

                if (textValue.SelectionStart.HasValue || textValue.SelectionLength.HasValue)
                {
                    var start = textValue.SelectionStart ?? 0;
                    var length = textValue.SelectionLength ?? 0;
                    SetSelection(start, length);
                }
                else if (textValue.CaretIndex.HasValue)
                {
                    SetCaretPosition(textValue.CaretIndex.Value, true);
                }

                return true;
        }

        return false;
    }

    public void Focus()
    {
        if (!IsEnabled || _isFocused)
        {
            return;
        }

        _isFocused = true;
        if (_focusBorderBrush is not null)
        {
            BorderBrush = _focusBorderBrush;
        }

        ResetCaretBlink();
    }

    public void Defocus()
    {
        if (!_isFocused)
        {
            return;
        }

        _isFocused = false;
        _caretVisible = false;
        if (_normalBorderBrush is not null)
        {
            BorderBrush = _normalBorderBrush;
        }

        Invalidate();
    }

    public override void Arrange(Rect bounds)
    {
        base.Arrange(bounds);
        _textBounds = bounds.Deflate(Padding);
        if (_textBounds.Width < 0 || _textBounds.Height < 0)
        {
            _textBounds = new Rect(bounds.X, bounds.Y, 0, 0);
        }
    }

    public override bool HandlePointerEvent(in WidgetPointerEvent e)
    {
        var handled = base.HandlePointerEvent(e);

        if (!IsEnabled)
        {
            return handled;
        }

        if (e.Kind == WidgetPointerEventKind.Pressed)
        {
            Focus();
            ResetCaretBlink();
            return true;
        }

        return handled;
    }

    public override bool HandleKeyboardEvent(in WidgetKeyboardEvent e)
    {
        if (!_isFocused || !IsEnabled)
        {
            return false;
        }

        var args = e.Args;
        var modifiers = args.KeyModifiers;
        var handled = false;

        switch (args.Key)
        {
            case Avalonia.Input.Key.Back:
                handled = Backspace();
                break;
            case Avalonia.Input.Key.Delete:
                handled = Delete();
                break;
            case Avalonia.Input.Key.Left:
                handled = MoveCaret(-1, (modifiers & KeyModifiers.Shift) != 0);
                break;
            case Avalonia.Input.Key.Right:
                handled = MoveCaret(1, (modifiers & KeyModifiers.Shift) != 0);
                break;
            case Avalonia.Input.Key.Home:
                handled = MoveCaretToBeginning((modifiers & KeyModifiers.Shift) != 0);
                break;
            case Avalonia.Input.Key.End:
                handled = MoveCaretToEnd((modifiers & KeyModifiers.Shift) != 0);
                break;
            case Avalonia.Input.Key.A when (modifiers & KeyModifiers.Control) != 0:
                SelectAll();
                handled = true;
                break;
            case Avalonia.Input.Key.C when (modifiers & KeyModifiers.Control) != 0:
                handled = CopySelectionToClipboard();
                break;
            case Avalonia.Input.Key.X when (modifiers & KeyModifiers.Control) != 0:
                handled = CutSelectionToClipboard();
                break;
            case Avalonia.Input.Key.V when (modifiers & KeyModifiers.Control) != 0:
                handled = PasteFromClipboard();
                break;
            case Avalonia.Input.Key.Enter:
                handled = true;
                Submitted?.Invoke(this, EventArgs.Empty);
                break;
            default:
                handled = TryHandleCharacterInput(args);
                break;
        }

        if (handled)
        {
            args.Handled = true;
            ResetCaretBlink();
        }

        return handled;
    }

    public override void Draw(DrawingContext context)
    {
        if (Background is { } background)
        {
            context.FillRectangle(background, Bounds);
        }

        if (BorderBrush is { } border && BorderThickness > 0)
        {
            var pen = new Pen(border, BorderThickness);
            context.DrawRectangle(null, pen, Bounds, CornerRadius.TopLeft, CornerRadius.TopLeft);
        }

        UpdateDisplayText();

        var formatted = EnsureFormattedText();
        if (formatted is null)
        {
            return;
        }

        if (_textBounds.Width <= 0 || _textBounds.Height <= 0)
        {
            return;
        }

        formatted.MaxTextWidth = Math.Max(0, _textBounds.Width);
        formatted.MaxTextHeight = Math.Max(0, _textBounds.Height);

        using var clip = context.PushClip(_textBounds);
        using var transform = PushRenderTransform(context);
        var origin = GetTextOrigin(formatted);

        var showPlaceholder = string.IsNullOrEmpty(_text) && !string.IsNullOrEmpty(Placeholder);
        if (showPlaceholder)
        {
            DrawPlaceholder(context, origin);
            DrawCaretIfNeeded(context, formatted, origin);
            return;
        }

        DrawSelection(context, formatted, origin);
        DrawFormattedText(context, formatted, origin);
        DrawCaretIfNeeded(context, formatted, origin);
    }

    protected virtual string GetDisplayText() => _text;

    protected void UpdateDisplayText()
    {
        base.SetText(GetDisplayText());
    }

    protected virtual void DrawPlaceholder(DrawingContext context, Point origin)
    {
        if (string.IsNullOrEmpty(Placeholder))
        {
            return;
        }

        var brush = PlaceholderBrush ?? new ImmutableSolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88));
        var formatted = new FormattedText(
            Placeholder,
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface(FontFamily, FontStyle, FontWeight, FontStretch),
            GetEffectiveEmSize(),
            brush);

        formatted.MaxTextWidth = Math.Max(0, _textBounds.Width);
        formatted.MaxTextHeight = Math.Max(0, _textBounds.Height);

        context.DrawText(formatted, origin);
    }

    protected void DrawCaretIfNeeded(DrawingContext context, FormattedText formatted, Point origin)
    {
        UpdateCaretBlink();
        if (!_isFocused || !_caretVisible)
        {
            return;
        }

        DrawCaret(context, formatted, origin);
    }

    protected override Point GetTextOrigin(FormattedText formatted)
    {
        var bounds = _textBounds;
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return base.GetTextOrigin(formatted);
        }

        var originY = bounds.Y + Math.Max(0, (bounds.Height - formatted.Height) / 2);
        return new Point(bounds.X, originY);
    }

    protected void ResetCaretBlink()
    {
        _caretVisible = true;
        _nextCaretToggle = DateTime.UtcNow + CaretBlinkInterval;
        WidgetAnimationFrameScheduler.RequestFrame();
    }

    private void UpdateCaretBlink()
    {
        if (!_isFocused)
        {
            _caretVisible = false;
            return;
        }

        var now = DateTime.UtcNow;
        if (now >= _nextCaretToggle)
        {
            _caretVisible = !_caretVisible;
            _nextCaretToggle = now + CaretBlinkInterval;
        }

        WidgetAnimationFrameScheduler.RequestFrame();
    }

    private void SetTextInternal(string value, bool raiseEvent)
    {
        var normalized = value ?? string.Empty;
        if (string.Equals(_text, normalized, StringComparison.Ordinal))
        {
            UpdateDisplayText();
            return;
        }

        var previous = _text;
        _text = normalized;

        var caret = Math.Clamp(_caretIndex, 0, _text.Length);
        SetSelectionInternal(caret, caret);
        UpdateDisplayText();
        Invalidate();
        ResetCaretBlink();

        if (raiseEvent)
        {
            TextChanged?.Invoke(this, new WidgetValueChangedEventArgs<string>(this, previous, _text));
        }
    }

    private bool Backspace()
    {
        if (IsReadOnly)
        {
            return false;
        }

        if (SelectionLength > 0)
        {
            ReplaceSelection(string.Empty);
            return true;
        }

        if (_caretIndex <= 0)
        {
            return false;
        }

        var previous = _text;
        _text = _text.Remove(_caretIndex - 1, 1);
        var caret = _caretIndex - 1;
        SetSelectionInternal(caret, caret);
        UpdateDisplayText();
        Invalidate();
        TextChanged?.Invoke(this, new WidgetValueChangedEventArgs<string>(this, previous, _text));
        ResetCaretBlink();
        return true;
    }

    private bool Delete()
    {
        if (IsReadOnly)
        {
            return false;
        }

        if (SelectionLength > 0)
        {
            ReplaceSelection(string.Empty);
            return true;
        }

        if (_caretIndex >= _text.Length)
        {
            return false;
        }

        var previous = _text;
        _text = _text.Remove(_caretIndex, 1);
        SetSelectionInternal(_caretIndex, _caretIndex);
        UpdateDisplayText();
        Invalidate();
        TextChanged?.Invoke(this, new WidgetValueChangedEventArgs<string>(this, previous, _text));
        ResetCaretBlink();
        return true;
    }

    private bool MoveCaret(int delta, bool extendSelection)
    {
        var newIndex = Math.Clamp(_caretIndex + delta, 0, _text.Length);
        if (extendSelection)
        {
            _selectionIndex = newIndex;
            _caretIndex = newIndex;
        }
        else
        {
            SetSelectionInternal(newIndex, newIndex);
        }

        Invalidate();
        ResetCaretBlink();
        return true;
    }

    private bool MoveCaretToBeginning(bool extendSelection)
    {
        if (extendSelection)
        {
            _selectionIndex = 0;
            _caretIndex = 0;
        }
        else
        {
            SetSelectionInternal(0, 0);
        }

        Invalidate();
        ResetCaretBlink();
        return true;
    }

    private bool MoveCaretToEnd(bool extendSelection)
    {
        var end = _text.Length;
        if (extendSelection)
        {
            _selectionIndex = end;
            _caretIndex = end;
        }
        else
        {
            SetSelectionInternal(end, end);
        }

        Invalidate();
        ResetCaretBlink();
        return true;
    }

    private void SelectAll()
    {
        _anchorIndex = 0;
        _selectionIndex = _text.Length;
        _caretIndex = _selectionIndex;
        Invalidate();
        ResetCaretBlink();
    }

    private bool TryHandleCharacterInput(KeyEventArgs args)
    {
        if (IsReadOnly)
        {
            return false;
        }

        if (TryGetCharacter(args, out var ch))
        {
            InsertText(ch.ToString());
            return true;
        }

        return false;
    }

    protected void InsertText(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        if (IsReadOnly)
        {
            return;
        }

        var previous = _text;
        var start = SelectionStart;
        var length = SelectionLength;

        var builder = new StringBuilder(_text);
        if (length > 0)
        {
            builder.Remove(start, length);
        }

        builder.Insert(start, text);
        _text = builder.ToString();

        var caret = start + text.Length;
        SetSelectionInternal(caret, caret);
        UpdateDisplayText();
        Invalidate();
        TextChanged?.Invoke(this, new WidgetValueChangedEventArgs<string>(this, previous, _text));
        ResetCaretBlink();
    }

    private void ReplaceSelection(string replacement)
    {
        if (IsReadOnly)
        {
            return;
        }

        var previous = _text;
        var start = SelectionStart;
        var length = SelectionLength;

        var builder = new StringBuilder(_text);
        builder.Remove(start, length);
        builder.Insert(start, replacement);
        _text = builder.ToString();

        var caret = start + replacement.Length;
        SetSelectionInternal(caret, caret);
        UpdateDisplayText();
        Invalidate();
        TextChanged?.Invoke(this, new WidgetValueChangedEventArgs<string>(this, previous, _text));
        ResetCaretBlink();
    }

    private bool CopySelectionToClipboard() => false;

    private bool CutSelectionToClipboard()
    {
        if (IsReadOnly || !HasSelection)
        {
            return false;
        }

        ReplaceSelection(string.Empty);
        return true;
    }

    private bool PasteFromClipboard() => false;

    private static bool TryGetCharacter(KeyEventArgs args, out char ch)
    {
        ch = '\0';
        if ((args.KeyModifiers & KeyModifiers.Control) != 0)
        {
            return false;
        }

        var shift = (args.KeyModifiers & KeyModifiers.Shift) != 0;

        if (args.Key == Avalonia.Input.Key.Space)
        {
            ch = ' ';
            return true;
        }

        if (args.Key >= Avalonia.Input.Key.A && args.Key <= Avalonia.Input.Key.Z)
        {
            ch = (char)('a' + (args.Key - Avalonia.Input.Key.A));
            if (shift)
            {
                ch = char.ToUpperInvariant(ch);
            }
            return true;
        }

        if (args.Key >= Avalonia.Input.Key.D0 && args.Key <= Avalonia.Input.Key.D9)
        {
            return TryMapDigit(args.Key, shift, out ch);
        }

        return TryMapSymbol(args.Key, shift, out ch);
    }

    private static bool TryMapDigit(Avalonia.Input.Key key, bool shift, out char ch)
    {
        ch = '\0';
        var index = key - Avalonia.Input.Key.D0;
        var digits = shift ? ")!@#$%^&*(" : "0123456789";
        ch = digits[index];
        return true;
    }

    private static bool TryMapSymbol(Avalonia.Input.Key key, bool shift, out char ch)
    {
        ch = key switch
        {
            Avalonia.Input.Key.OemMinus => shift ? '_' : '-',
            Avalonia.Input.Key.OemPlus => shift ? '+' : '=',
            Avalonia.Input.Key.OemComma => shift ? '<' : ',',
            Avalonia.Input.Key.OemPeriod => shift ? '>' : '.',
            Avalonia.Input.Key.Oem1 => shift ? ':' : ';',
            Avalonia.Input.Key.Oem2 => shift ? '?' : '/',
            Avalonia.Input.Key.Oem3 => shift ? '~' : '`',
            Avalonia.Input.Key.Oem4 => shift ? '{' : '[',
            Avalonia.Input.Key.Oem5 => shift ? '|' : '\\',
            Avalonia.Input.Key.Oem6 => shift ? '}' : ']',
            Avalonia.Input.Key.Oem7 => shift ? '"' : '\'',
            _ => '\0'
        };

        return ch != '\0';
    }

    protected override void DrawSelection(DrawingContext context, FormattedText formatted, Point origin)
    {
        if (!HasSelection || SelectionBrush is null)
        {
            return;
        }

        var geometry = formatted.BuildHighlightGeometry(origin, SelectionStart, SelectionLength);
        if (geometry is not null)
        {
            context.DrawGeometry(SelectionBrush, null, geometry);
        }
    }
}

public class MaskedTextBoxWidget : TextInputWidget
{
    public char MaskChar { get; set; } = '●';

    public bool RevealText { get; set; }

    protected override string GetDisplayText()
    {
        if (RevealText)
        {
            return InternalText;
        }

        return new string(MaskChar, InternalText.Length);
    }
}
