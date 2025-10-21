using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace FastTreeDataGrid.Control.Widgets;

internal readonly record struct WidgetStyleKey(Type WidgetType, string? StyleKey, WidgetVisualState State);

public delegate void WidgetStyleApplicator(Widget widget);

public sealed class WidgetStyleRule
{
    public WidgetStyleRule(Type widgetType, WidgetVisualState state, WidgetStyleApplicator apply, string? styleKey = null)
    {
        WidgetType = widgetType;
        State = state;
        Apply = apply ?? throw new ArgumentNullException(nameof(apply));
        StyleKey = styleKey;
    }

    public Type WidgetType { get; }
    public string? StyleKey { get; }
    public WidgetVisualState State { get; }
    public WidgetStyleApplicator Apply { get; }
}

public static class WidgetStyleManager
{
    private const string DefaultThemeName = "Default";

    private static readonly ReaderWriterLockSlim ThemeLock = new();
    private static readonly Dictionary<string, Dictionary<WidgetStyleKey, WidgetStyleApplicator>> Themes = new(StringComparer.OrdinalIgnoreCase);
    private static string _currentTheme = DefaultThemeName;

    public static event Action<string>? ThemeChanged;

    static WidgetStyleManager()
    {
        Themes[_currentTheme] = new Dictionary<WidgetStyleKey, WidgetStyleApplicator>();
    }

    public static string CurrentTheme
    {
        get
        {
            ThemeLock.EnterReadLock();
            try
            {
                return _currentTheme;
            }
            finally
            {
                ThemeLock.ExitReadLock();
            }
        }
    }

    public static void SetTheme(string themeName)
    {
        if (string.IsNullOrWhiteSpace(themeName))
        {
            throw new ArgumentException("Theme name must not be null or empty.", nameof(themeName));
        }

        ThemeLock.EnterUpgradeableReadLock();
        try
        {
            if (string.Equals(_currentTheme, themeName, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            ThemeLock.EnterWriteLock();
            try
            {
                _currentTheme = themeName;
                if (!Themes.ContainsKey(themeName))
                {
                    Themes[themeName] = new Dictionary<WidgetStyleKey, WidgetStyleApplicator>();
                }
            }
            finally
            {
                ThemeLock.ExitWriteLock();
            }
        }
        finally
        {
            ThemeLock.ExitUpgradeableReadLock();
        }

        ThemeChanged?.Invoke(_currentTheme);
    }

    public static void Register(string themeName, WidgetStyleRule rule)
    {
        if (rule is null)
        {
            throw new ArgumentNullException(nameof(rule));
        }

        if (string.IsNullOrWhiteSpace(themeName))
        {
            themeName = DefaultThemeName;
        }

        ThemeLock.EnterWriteLock();
        try
        {
            if (!Themes.TryGetValue(themeName, out var theme))
            {
                theme = new Dictionary<WidgetStyleKey, WidgetStyleApplicator>();
                Themes[themeName] = theme;
            }

            var key = new WidgetStyleKey(rule.WidgetType, rule.StyleKey, rule.State);
            theme[key] = rule.Apply;
        }
        finally
        {
            ThemeLock.ExitWriteLock();
        }
    }

    internal static void Apply(Widget widget, WidgetVisualState state)
    {
        WidgetStyleApplicator? applicator = null;
        ThemeLock.EnterReadLock();
        try
        {
            if (Themes.TryGetValue(_currentTheme, out var theme))
            {
                applicator = ResolveApplicator(theme, widget, state);
            }

            if (applicator is null && Themes.TryGetValue(DefaultThemeName, out var defaultTheme))
            {
                applicator = ResolveApplicator(defaultTheme, widget, state);
            }
        }
        finally
        {
            ThemeLock.ExitReadLock();
        }

        applicator?.Invoke(widget);
    }

    public static void RefreshCurrentTheme()
    {
        string themeName;

        ThemeLock.EnterReadLock();
        try
        {
            themeName = _currentTheme;
        }
        finally
        {
            ThemeLock.ExitReadLock();
        }

        ThemeChanged?.Invoke(themeName);
    }

    private static WidgetStyleApplicator? ResolveApplicator(Dictionary<WidgetStyleKey, WidgetStyleApplicator> theme, Widget widget, WidgetVisualState state)
    {
        var type = widget.GetType();
        var styleKey = widget.StyleKey;

        while (type is not null)
        {
            if (styleKey is not null && theme.TryGetValue(new WidgetStyleKey(type, styleKey, state), out var withKey))
            {
                return withKey;
            }

            if (theme.TryGetValue(new WidgetStyleKey(type, null, state), out var withoutKey))
            {
                return withoutKey;
            }

            type = type.BaseType;
        }

        return null;
    }
}
