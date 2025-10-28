using System;
using System.Windows.Input;
using FastTreeDataGrid.Engine.Infrastructure;

namespace FastTreeDataGrid.Control.Widgets;

/// <summary>
/// Shared base for button-like widgets. Aligns with Avalonia's <c>ButtonBase</c> by
/// providing click events and command execution without introducing additional layout cost.
/// </summary>
public abstract class ButtonWidgetBase : InteractiveTemplatedWidget
{
    private ICommand? _command;
    private object? _commandParameter;
    private bool _isEnabledFromData = true;
    private WidgetAutomationSettings? _automationSettings;

    public event EventHandler<WidgetEventArgs>? Click;

    public ICommand? Command
    {
        get => _command;
        set
        {
            if (ReferenceEquals(_command, value))
            {
                return;
            }

            if (_command is not null)
            {
                _command.CanExecuteChanged -= OnCommandCanExecuteChanged;
            }

            _command = value;

            if (_command is not null)
            {
                _command.CanExecuteChanged += OnCommandCanExecuteChanged;
            }

            UpdateEffectiveIsEnabled();
        }
    }

    public object? CommandParameter
    {
        get => _commandParameter;
        set
        {
            if (Equals(_commandParameter, value))
            {
                return;
            }

            _commandParameter = value;
            UpdateEffectiveIsEnabled();
        }
    }

    /// <summary>
    /// Allows derived widgets to update the data-driven enabled state before command evaluation.
    /// </summary>
    /// <param name="enabled">True when the widget should be enabled independent of command state.</param>
    protected void SetIsEnabledFromData(bool enabled)
    {
        if (_isEnabledFromData == enabled)
        {
            return;
        }

        _isEnabledFromData = enabled;
        UpdateEffectiveIsEnabled();
    }

    protected void SetAutomationSettings(WidgetAutomationSettings? settings)
    {
        _automationSettings = settings;
    }

    protected void UpdateAutomationMetadata(string? fallbackName, string? fallbackCommandLabel, string? fallbackAccessKey)
    {
        var automation = Automation;
        var resolvedName = _automationSettings?.Name ?? fallbackName ?? fallbackCommandLabel;
        var resolvedLabel = _automationSettings?.CommandLabel ?? fallbackCommandLabel ?? fallbackName;
        var resolvedAccessKey = _automationSettings?.AccessKey ?? fallbackAccessKey;

        automation.Name = resolvedName;
        automation.CommandLabel = resolvedLabel;
        automation.AccessKey = resolvedAccessKey;
    }

    protected void UpdateAutomationFromText(string rawText, AccessTextWidget? accessTextWidget)
    {
        var source = rawText ?? string.Empty;
        var parsedText = AccessTextWidget.ParseAccessText(source, out var parsedAccessKey, out _);
        var displayText = accessTextWidget?.Text ?? parsedText;

        string? accessKey = null;
        if (accessTextWidget?.AccessKey is { } key && key != 0)
        {
            accessKey = key.ToString();
        }
        else if (parsedAccessKey.HasValue)
        {
            accessKey = parsedAccessKey.Value.ToString();
        }

        UpdateAutomationMetadata(displayText, displayText, accessKey);
    }

    protected override void OnClick()
    {
        if (!IsEnabled)
        {
            return;
        }

        var args = new WidgetEventArgs(this);
        OnClick(args);
        ExecuteCommand();
    }

    /// <summary>
    /// Called when the widget is clicked. Derived classes can extend behavior by overriding this method.
    /// </summary>
    /// <param name="args">Widget event data.</param>
    protected virtual void OnClick(WidgetEventArgs args)
    {
        Click?.Invoke(this, args);
    }

    protected bool ExecuteCommand()
    {
        if (_command?.CanExecute(_commandParameter) == true)
        {
            _command.Execute(_commandParameter);
            return true;
        }

        return false;
    }

    private void OnCommandCanExecuteChanged(object? sender, EventArgs e)
    {
        UpdateEffectiveIsEnabled();
    }

    private void UpdateEffectiveIsEnabled()
    {
        base.IsEnabled = _isEnabledFromData && (_command?.CanExecute(_commandParameter) ?? true);
    }
}
