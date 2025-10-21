using System;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Styling;
using FastTreeDataGrid.Control.Widgets;

namespace FastTreeDataGrid.Control.Theming;

internal static class WidgetFluentPalette
{
    private static readonly object Sync = new();
    private static bool _initialized;
    private static bool _themeHooked;
    private static PaletteData _current = PaletteData.CreateFallback();

    public static PaletteData Current
    {
        get
        {
            EnsureInitialized();
            return _current;
        }
    }

    public static void EnsureInitialized()
    {
        if (_initialized)
        {
            return;
        }

        lock (Sync)
        {
            if (_initialized)
            {
                return;
            }

            _current = BuildPalette();
            HookThemeChange();
            _initialized = true;
        }
    }

    private static void HookThemeChange()
    {
        if (_themeHooked)
        {
            return;
        }

        if (Application.Current is { } app)
        {
            app.ActualThemeVariantChanged += (_, _) =>
            {
                lock (Sync)
                {
                    _current = BuildPalette();
                }

                WidgetStyleManager.RefreshCurrentTheme();
            };

            _themeHooked = true;
        }
    }

    private static PaletteData BuildPalette()
    {
        var accessor = new ResourceAccessor(Application.Current);
        var controlCornerRadius = accessor.GetCornerRadius("ControlCornerRadius") ?? new CornerRadius(4);

        return new PaletteData(
            controlCornerRadius,
            ButtonPalette.Create(accessor, controlCornerRadius),
            CheckBoxPalette.Create(accessor, controlCornerRadius),
            RadioButtonPalette.Create(accessor),
            ToggleSwitchPalette.Create(accessor),
            SliderPalette.Create(accessor),
            ProgressPalette.Create(accessor),
            ChartPalette.Create(accessor),
            BadgePalette.Create(accessor, controlCornerRadius));
    }

    internal sealed class ResourceAccessor
    {
        private readonly Application? _application;

        public ResourceAccessor(Application? application)
        {
            _application = application;
        }

        public object? Get(string key)
        {
            if (_application is null)
            {
                return null;
            }

            if (_application.TryGetResource(key, _application.ActualThemeVariant, out var themed))
            {
                return themed;
            }

            return _application.TryGetResource(key, ThemeVariant.Default, out var value) ? value : null;
        }

        public ImmutableSolidColorBrush? GetBrush(string key, ImmutableSolidColorBrush? fallback = null)
        {
            if (Get(key) is IBrush brush)
            {
                return ToImmutable(brush);
            }

            if (Get(key) is Color color)
            {
                return new ImmutableSolidColorBrush(color);
            }

            return fallback;
        }

        public Thickness? GetThickness(string key)
        {
            return Get(key) is Thickness thickness ? thickness : null;
        }

        public CornerRadius? GetCornerRadius(string key)
        {
            return Get(key) is CornerRadius corner ? corner : null;
        }

        public Geometry? GetGeometry(string key)
        {
            return Get(key) as Geometry;
        }

        private static ImmutableSolidColorBrush? ToImmutable(IBrush brush)
        {
            return brush switch
            {
                ImmutableSolidColorBrush immutable => immutable,
                SolidColorBrush solid => new ImmutableSolidColorBrush(solid.Color),
                _ => null
            };
        }
    }

    internal readonly record struct BrushState(
        ImmutableSolidColorBrush? Normal,
        ImmutableSolidColorBrush? PointerOver,
        ImmutableSolidColorBrush? Pressed,
        ImmutableSolidColorBrush? Disabled,
        ImmutableSolidColorBrush? Focused = null)
    {
        public ImmutableSolidColorBrush? Get(WidgetVisualState state)
        {
            return state switch
            {
                WidgetVisualState.PointerOver => PointerOver ?? Normal,
                WidgetVisualState.Pressed => Pressed ?? Normal,
                WidgetVisualState.Disabled => Disabled ?? Normal,
                WidgetVisualState.Focused => Focused ?? PointerOver ?? Normal,
                _ => Normal,
            };
        }
    }

    internal sealed record PaletteData(
        CornerRadius ControlCornerRadius,
        ButtonPalette Button,
        CheckBoxPalette CheckBox,
        RadioButtonPalette RadioButton,
        ToggleSwitchPalette ToggleSwitch,
        SliderPalette Slider,
        ProgressPalette Progress,
        ChartPalette Chart,
        BadgePalette Badge)
    {
        public static PaletteData CreateFallback()
        {
            var radius = new CornerRadius(4);
            return new PaletteData(
                radius,
                ButtonPalette.CreateFallback(radius),
                CheckBoxPalette.CreateFallback(radius),
                RadioButtonPalette.CreateFallback(),
                ToggleSwitchPalette.CreateFallback(),
                SliderPalette.CreateFallback(),
                ProgressPalette.CreateFallback(),
                ChartPalette.CreateFallback(),
                BadgePalette.CreateFallback(radius));
        }
    }

    internal sealed record ButtonPalette(
        ButtonVariantPalette Standard,
        ButtonVariantPalette Accent,
        Thickness Padding,
        double BorderThickness,
        CornerRadius CornerRadius)
    {
        public static ButtonPalette Create(ResourceAccessor accessor, CornerRadius defaultCornerRadius)
        {
            var padding = accessor.GetThickness("ButtonPadding") ?? new Thickness(8, 5, 8, 6);
            var borderThickness = accessor.GetThickness("ButtonBorderThemeThickness") ?? new Thickness(1);
            var cornerRadius = accessor.GetCornerRadius("ControlCornerRadius") ?? defaultCornerRadius;

            return new ButtonPalette(
                ButtonVariantPalette.Create(accessor, "Button"),
                ButtonVariantPalette.Create(accessor, "AccentButton"),
                padding,
                Math.Max(0, borderThickness.Left),
                cornerRadius);
        }

        public static ButtonPalette CreateFallback(CornerRadius defaultCornerRadius)
        {
            var padding = new Thickness(8, 5, 8, 6);
            return new ButtonPalette(
                ButtonVariantPalette.CreateFallback(accent: false),
                ButtonVariantPalette.CreateFallback(accent: true),
                padding,
                1,
                defaultCornerRadius);
        }
    }

    internal sealed record ButtonVariantPalette(
        BrushState Background,
        BrushState Border,
        BrushState Foreground)
    {
        public static ButtonVariantPalette Create(ResourceAccessor accessor, string prefix)
        {
            return new ButtonVariantPalette(
                BuildBrushState(accessor, $"{prefix}Background"),
                BuildBrushState(accessor, $"{prefix}BorderBrush"),
                BuildBrushState(accessor, $"{prefix}Foreground"));
        }

        public static ButtonVariantPalette CreateFallback(bool accent)
        {
            ImmutableSolidColorBrush normalBackground;
            ImmutableSolidColorBrush pointerBackground;
            ImmutableSolidColorBrush pressedBackground;
            ImmutableSolidColorBrush disabledBackground;

            ImmutableSolidColorBrush normalBorder;
            ImmutableSolidColorBrush pointerBorder;
            ImmutableSolidColorBrush pressedBorder;
            ImmutableSolidColorBrush disabledBorder;

            ImmutableSolidColorBrush normalForeground;
            ImmutableSolidColorBrush pointerForeground;
            ImmutableSolidColorBrush pressedForeground;
            ImmutableSolidColorBrush disabledForeground;

            if (accent)
            {
                normalBackground = new ImmutableSolidColorBrush(Color.FromRgb(0x31, 0x82, 0xCE));
                pointerBackground = new ImmutableSolidColorBrush(Color.FromRgb(0x42, 0x8E, 0xD8));
                pressedBackground = new ImmutableSolidColorBrush(Color.FromRgb(0x24, 0x62, 0x9C));
                disabledBackground = new ImmutableSolidColorBrush(Color.FromRgb(0xB2, 0xD6, 0xF2));

                normalBorder = new ImmutableSolidColorBrush(Color.FromRgb(0x24, 0x62, 0x9C));
                pointerBorder = new ImmutableSolidColorBrush(Color.FromRgb(0x31, 0x82, 0xCE));
                pressedBorder = new ImmutableSolidColorBrush(Color.FromRgb(0x1F, 0x52, 0x80));
                disabledBorder = new ImmutableSolidColorBrush(Color.FromRgb(0x9E, 0xB9, 0xD4));

                normalForeground = new ImmutableSolidColorBrush(Colors.White);
                pointerForeground = new ImmutableSolidColorBrush(Colors.White);
                pressedForeground = new ImmutableSolidColorBrush(Colors.White);
                disabledForeground = new ImmutableSolidColorBrush(Color.FromRgb(0xE5, 0xE5, 0xE5));
            }
            else
            {
                normalBackground = new ImmutableSolidColorBrush(Color.FromRgb(0xF2, 0xF2, 0xF2));
                pointerBackground = new ImmutableSolidColorBrush(Color.FromRgb(0xEE, 0xEE, 0xEE));
                pressedBackground = new ImmutableSolidColorBrush(Color.FromRgb(0xDD, 0xDD, 0xDD));
                disabledBackground = new ImmutableSolidColorBrush(Color.FromRgb(0xEB, 0xEB, 0xEB));

                normalBorder = new ImmutableSolidColorBrush(Color.FromRgb(0xCD, 0xCD, 0xCD));
                pointerBorder = new ImmutableSolidColorBrush(Color.FromRgb(0xB5, 0xB5, 0xB5));
                pressedBorder = new ImmutableSolidColorBrush(Color.FromRgb(0x96, 0x96, 0x96));
                disabledBorder = new ImmutableSolidColorBrush(Color.FromRgb(0xD2, 0xD2, 0xD2));

                normalForeground = new ImmutableSolidColorBrush(Color.FromRgb(0x33, 0x33, 0x33));
                pointerForeground = normalForeground;
                pressedForeground = normalForeground;
                disabledForeground = new ImmutableSolidColorBrush(Color.FromRgb(0x99, 0x99, 0x99));
            }

            return new ButtonVariantPalette(
                new BrushState(normalBackground, pointerBackground, pressedBackground, disabledBackground),
                new BrushState(normalBorder, pointerBorder, pressedBorder, disabledBorder),
                new BrushState(normalForeground, pointerForeground, pressedForeground, disabledForeground));
        }
    }

    internal sealed record CheckBoxPalette(
        Thickness Padding,
        double BoxSize,
        double StrokeThickness,
        CheckBoxValuePalette Unchecked,
        CheckBoxValuePalette Checked,
        CheckBoxValuePalette Indeterminate,
        CornerRadius CornerRadius)
    {
        public static CheckBoxPalette Create(ResourceAccessor accessor, CornerRadius cornerRadius)
        {
            var padding = new Thickness(4);
            var strokeThickness = accessor.GetThickness("CheckBoxBorderThemeThickness")?.Left ?? 1;
            var checkMarkGeometry = accessor.GetGeometry("CheckMarkPathData") ?? StreamGeometry.Parse("M5.5 10.586 1.707 6.793A1 1 0 0 0 .293 8.207l4.5 4.5a1 1 0 0 0 1.414 0l11-11A1 1 0 0 0 15.793.293L5.5 10.586Z");
            var indeterminateGeometry = StreamGeometry.Parse("M1536 1536v-1024h-1024v1024h1024z");

            return new CheckBoxPalette(
                padding,
                20,
                Math.Max(0, strokeThickness),
                CheckBoxValuePalette.Create(accessor, "Unchecked", glyphGeometry: null, glyphWidth: 0),
                CheckBoxValuePalette.Create(accessor, "Checked", checkMarkGeometry, 9),
                CheckBoxValuePalette.Create(accessor, "Indeterminate", indeterminateGeometry, 7),
                cornerRadius);
        }

        public static CheckBoxPalette CreateFallback(CornerRadius cornerRadius)
        {
            var normalBorder = new ImmutableSolidColorBrush(Color.FromRgb(0x60, 0x60, 0x60));
            var checkedFill = new ImmutableSolidColorBrush(Color.FromRgb(0x31, 0x82, 0xCE));
            var disabledBorder = new ImmutableSolidColorBrush(Color.FromRgb(0xBE, 0xBE, 0xBE));
            var disabledFill = new ImmutableSolidColorBrush(Color.FromRgb(0xEB, 0xEB, 0xEB));

            var uncheckedPalette = CheckBoxValuePalette.CreateFallback(
                background: new ImmutableSolidColorBrush(Colors.Transparent),
                border: normalBorder,
                boxFill: new ImmutableSolidColorBrush(Colors.White),
                boxStroke: normalBorder,
                glyph: new ImmutableSolidColorBrush(Color.FromRgb(0x33, 0x33, 0x33)));

            var checkedPalette = CheckBoxValuePalette.CreateFallback(
                background: new ImmutableSolidColorBrush(Colors.Transparent),
                border: checkedFill,
                boxFill: checkedFill,
                boxStroke: checkedFill,
                glyph: new ImmutableSolidColorBrush(Colors.White),
                glyphGeometry: StreamGeometry.Parse("M5.5 10.586 1.707 6.793A1 1 0 0 0 .293 8.207l4.5 4.5a1 1 0 0 0 1.414 0l11-11A1 1 0 0 0 15.793.293L5.5 10.586Z"),
                glyphWidth: 9);

            var indeterminatePalette = CheckBoxValuePalette.CreateFallback(
                background: new ImmutableSolidColorBrush(Colors.Transparent),
                border: normalBorder,
                boxFill: new ImmutableSolidColorBrush(Color.FromRgb(0xD8, 0xD8, 0xD8)),
                boxStroke: normalBorder,
                glyph: new ImmutableSolidColorBrush(Color.FromRgb(0x33, 0x33, 0x33)),
                glyphGeometry: StreamGeometry.Parse("M1536 1536v-1024h-1024v1024h1024z"),
                glyphWidth: 7);

            return new CheckBoxPalette(
                new Thickness(4),
                20,
                1,
                uncheckedPalette,
                checkedPalette,
                indeterminatePalette,
                cornerRadius);
        }
    }

    internal sealed record CheckBoxValuePalette(
        BrushState Background,
        BrushState Border,
        BrushState BoxFill,
        BrushState BoxStroke,
        BrushState Glyph,
        Geometry? GlyphGeometry,
        double GlyphWidth)
    {
        public static CheckBoxValuePalette Create(ResourceAccessor accessor, string state, Geometry? glyphGeometry, double glyphWidth)
        {
            return new CheckBoxValuePalette(
                BuildBrushState(accessor, $"CheckBoxBackground{state}"),
                BuildBrushState(accessor, $"CheckBoxBorderBrush{state}"),
                BuildBrushState(accessor, $"CheckBoxCheckBackgroundFill{state}"),
                BuildBrushState(accessor, $"CheckBoxCheckBackgroundStroke{state}"),
                BuildBrushState(accessor, $"CheckBoxCheckGlyphForeground{state}"),
                glyphGeometry,
                glyphWidth);
        }

        public static CheckBoxValuePalette CreateFallback(
            ImmutableSolidColorBrush background,
            ImmutableSolidColorBrush border,
            ImmutableSolidColorBrush boxFill,
            ImmutableSolidColorBrush boxStroke,
            ImmutableSolidColorBrush glyph,
            Geometry? glyphGeometry = null,
            double glyphWidth = 0)
        {
            var value = new BrushState(background, background, background, background);
            var borderValue = new BrushState(border, border, border, border);
            var fillValue = new BrushState(boxFill, boxFill, boxFill, boxFill);
            var strokeValue = new BrushState(boxStroke, boxStroke, boxStroke, boxStroke);
            var glyphValue = new BrushState(glyph, glyph, glyph, glyph);

            return new CheckBoxValuePalette(value, borderValue, fillValue, strokeValue, glyphValue, glyphGeometry, glyphWidth);
        }
    }

    internal sealed record RadioButtonPalette(
        BrushState Background,
        BrushState Border,
        BrushState OuterEllipseFill,
        BrushState OuterEllipseStroke,
        BrushState CheckedEllipseFill,
        BrushState CheckedEllipseStroke,
        BrushState GlyphFill,
        BrushState GlyphStroke,
        Thickness Padding,
        double BorderThickness)
    {
        public static RadioButtonPalette Create(ResourceAccessor accessor)
        {
            var padding = new Thickness(8, 0, 0, 0);
            var borderThickness = accessor.GetThickness("RadioButtonBorderThemeThickness")?.Left ?? 1;

            return new RadioButtonPalette(
                BuildBrushState(accessor, "RadioButtonBackground"),
                BuildBrushState(accessor, "RadioButtonBorderBrush"),
                BuildBrushState(accessor, "RadioButtonOuterEllipseFill"),
                BuildBrushState(accessor, "RadioButtonOuterEllipseStroke"),
                BuildBrushState(accessor, "RadioButtonOuterEllipseCheckedFill"),
                BuildBrushState(accessor, "RadioButtonOuterEllipseCheckedStroke"),
                BuildBrushState(accessor, "RadioButtonCheckGlyphFill"),
                BuildBrushState(accessor, "RadioButtonCheckGlyphStroke"),
                padding,
                Math.Max(0, borderThickness));
        }

        public static RadioButtonPalette CreateFallback()
        {
            var transparent = new ImmutableSolidColorBrush(Colors.Transparent);
            var accent = new ImmutableSolidColorBrush(Color.FromRgb(0x31, 0x82, 0xCE));
            var stroke = new ImmutableSolidColorBrush(Color.FromRgb(0x60, 0x60, 0x60));
            var disabled = new ImmutableSolidColorBrush(Color.FromRgb(0xC0, 0xC0, 0xC0));

            return new RadioButtonPalette(
                new BrushState(transparent, transparent, transparent, transparent),
                new BrushState(transparent, transparent, transparent, transparent),
                new BrushState(transparent, transparent, transparent, transparent),
                new BrushState(stroke, stroke, stroke, disabled),
                new BrushState(accent, accent, accent, disabled),
                new BrushState(accent, accent, accent, disabled),
                new BrushState(new ImmutableSolidColorBrush(Colors.White), new ImmutableSolidColorBrush(Colors.White), new ImmutableSolidColorBrush(Colors.White), disabled),
                new BrushState(transparent, transparent, transparent, transparent),
                new Thickness(8, 0, 0, 0),
                1);
        }
    }

    internal sealed record ToggleSwitchPalette(
        BrushState ContainerBackground,
        ToggleSwitchStatePalette Off,
        ToggleSwitchStatePalette On,
        double OuterStrokeThickness,
        double OnStrokeThickness)
    {
        public static ToggleSwitchPalette Create(ResourceAccessor accessor)
        {
            var off = ToggleSwitchStatePalette.Create(accessor, "Off");
            var on = ToggleSwitchStatePalette.Create(accessor, "On");
            var outerThickness = accessor.GetThickness("ToggleSwitchOuterBorderStrokeThickness")?.Left ?? 1;
            var onThickness = accessor.GetThickness("ToggleSwitchOnStrokeThickness")?.Left ?? 0;

            return new ToggleSwitchPalette(
                BuildBrushState(accessor, "ToggleSwitchContainerBackground"),
                off,
                on,
                Math.Max(0, outerThickness),
                Math.Max(0, onThickness));
        }

        public static ToggleSwitchPalette CreateFallback()
        {
            return new ToggleSwitchPalette(
                new BrushState(new ImmutableSolidColorBrush(Colors.Transparent), new ImmutableSolidColorBrush(Colors.Transparent), new ImmutableSolidColorBrush(Colors.Transparent), new ImmutableSolidColorBrush(Colors.Transparent)),
                ToggleSwitchStatePalette.CreateFallback(false),
                ToggleSwitchStatePalette.CreateFallback(true),
                1,
                0);
        }
    }

    internal sealed record ToggleSwitchStatePalette(
        BrushState TrackFill,
        BrushState TrackStroke,
        BrushState KnobFill)
    {
        public static ToggleSwitchStatePalette Create(ResourceAccessor accessor, string mode)
        {
            return new ToggleSwitchStatePalette(
                BuildBrushState(accessor, $"ToggleSwitchFill{mode}"),
                BuildBrushState(accessor, $"ToggleSwitchStroke{mode}"),
                BuildBrushState(accessor, $"ToggleSwitchKnobFill{mode}"));
        }

        public static ToggleSwitchStatePalette CreateFallback(bool isOn)
        {
            if (isOn)
            {
                var accent = new ImmutableSolidColorBrush(Color.FromRgb(0x31, 0x82, 0xCE));
                return new ToggleSwitchStatePalette(
                new BrushState(accent, accent, accent, new ImmutableSolidColorBrush(Color.FromRgb(0xC0, 0xC0, 0xC0))),
                new BrushState(accent, accent, accent, new ImmutableSolidColorBrush(Color.FromRgb(0xA0, 0xA0, 0xA0))),
                new BrushState(new ImmutableSolidColorBrush(Colors.White), new ImmutableSolidColorBrush(Colors.White), new ImmutableSolidColorBrush(Colors.White), new ImmutableSolidColorBrush(Color.FromRgb(0xF0, 0xF0, 0xF0))));
            }

            var neutral = new ImmutableSolidColorBrush(Color.FromRgb(0xBD, 0xBD, 0xBD));
            var neutralBorder = new ImmutableSolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88));
            var knob = new ImmutableSolidColorBrush(Colors.White);

            return new ToggleSwitchStatePalette(
                new BrushState(neutral, neutral, neutral, new ImmutableSolidColorBrush(Color.FromRgb(0xDD, 0xDD, 0xDD))),
                new BrushState(neutralBorder, neutralBorder, neutralBorder, new ImmutableSolidColorBrush(Color.FromRgb(0xB0, 0xB0, 0xB0))),
                new BrushState(knob, knob, knob, new ImmutableSolidColorBrush(Color.FromRgb(0xE5, 0xE5, 0xE5))));
        }
    }

    internal sealed record SliderPalette(
        BrushState TrackFill,
        BrushState ValueFill,
        BrushState ThumbFill,
        BrushState ThumbBorder)
    {
        public static SliderPalette Create(ResourceAccessor accessor)
        {
            return new SliderPalette(
                BuildBrushState(accessor, "SliderTrackFill"),
                BuildBrushState(accessor, "SliderTrackValueFill"),
                BuildBrushState(accessor, "SliderThumbBackground"),
                BuildBrushState(accessor, "SliderThumbBorderBrush"));
        }

        public static SliderPalette CreateFallback()
        {
            var track = new ImmutableSolidColorBrush(Color.FromRgb(0xDC, 0xDC, 0xDC));
            var value = new ImmutableSolidColorBrush(Color.FromRgb(0x31, 0x82, 0xCE));
            var thumb = new ImmutableSolidColorBrush(Colors.White);
            var thumbBorder = new ImmutableSolidColorBrush(Color.FromRgb(0xC8, 0xC8, 0xC8));

            return new SliderPalette(
                new BrushState(track, track, track, new ImmutableSolidColorBrush(Color.FromRgb(0xE5, 0xE5, 0xE5))),
                new BrushState(value, value, value, new ImmutableSolidColorBrush(Color.FromRgb(0xB2, 0xD6, 0xF2))),
                new BrushState(thumb, thumb, thumb, new ImmutableSolidColorBrush(Color.FromRgb(0xF5, 0xF5, 0xF5))),
                new BrushState(thumbBorder, thumbBorder, thumbBorder, new ImmutableSolidColorBrush(Color.FromRgb(0xD0, 0xD0, 0xD0))));
        }
    }

    internal sealed record ProgressPalette(
        ImmutableSolidColorBrush Track,
        ImmutableSolidColorBrush Value,
        ImmutableSolidColorBrush IndeterminateSegment)
    {
        public static ProgressPalette Create(ResourceAccessor accessor)
        {
            var track = accessor.GetBrush("SystemControlBackgroundBaseLowBrush") ?? new ImmutableSolidColorBrush(Color.FromRgb(0xE6, 0xE6, 0xE6));
            var value = accessor.GetBrush("SystemControlHighlightAccentBrush") ?? new ImmutableSolidColorBrush(Color.FromRgb(0x31, 0x82, 0xCE));
            var indeterminate = accessor.GetBrush("SystemControlHighlightAltAccentBrush") ?? value;

            return new ProgressPalette(
                track,
                value,
                indeterminate);
        }

        public static ProgressPalette CreateFallback()
        {
            return new ProgressPalette(
                new ImmutableSolidColorBrush(Color.FromRgb(0xE6, 0xE6, 0xE6)),
                new ImmutableSolidColorBrush(Color.FromRgb(0x31, 0x82, 0xCE)),
                new ImmutableSolidColorBrush(Color.FromRgb(0x31, 0x82, 0xCE)));
        }
    }

    internal sealed record ChartPalette(
        ImmutableSolidColorBrush Line,
        ImmutableSolidColorBrush Fill,
        ImmutableSolidColorBrush Baseline)
    {
        public static ChartPalette Create(ResourceAccessor accessor)
        {
            var fallback = CreateFallback();
            var line = accessor.GetBrush("FastTreeDataGridChartLineBrush", fallback.Line) ?? fallback.Line;
            var fill = accessor.GetBrush("FastTreeDataGridChartFillBrush", fallback.Fill) ?? fallback.Fill;
            var baseline = accessor.GetBrush("FastTreeDataGridChartBaselineBrush", fallback.Baseline) ?? fallback.Baseline;
            return new ChartPalette(line, fill, baseline);
        }

        public static ChartPalette CreateFallback()
        {
            return new ChartPalette(
                new ImmutableSolidColorBrush(Color.FromRgb(0x31, 0x82, 0xCE)),
                new ImmutableSolidColorBrush(Color.FromArgb(0x40, 0x31, 0x82, 0xCE)),
                new ImmutableSolidColorBrush(Color.FromRgb(0x80, 0x80, 0x80)));
        }
    }

    internal sealed record BadgePalette(
        ImmutableSolidColorBrush Background,
        ImmutableSolidColorBrush Foreground,
        double CornerRadius,
        double Padding)
    {
        public static BadgePalette Create(ResourceAccessor accessor, CornerRadius controlCornerRadius)
        {
            var background = accessor.GetBrush("SystemControlHighlightAccentBrush") ?? new ImmutableSolidColorBrush(Color.FromRgb(49, 130, 206));
            var foreground = accessor.GetBrush("SystemControlForegroundChromeWhiteBrush") ?? new ImmutableSolidColorBrush(Colors.White);
            var cornerRadius = accessor.GetCornerRadius("ControlCornerRadius")?.TopLeft ?? controlCornerRadius.TopLeft;
            var paddingThickness = accessor.GetThickness("BadgePadding") ?? default;
            var padding = paddingThickness == default ? 6 : paddingThickness.Left;

            return new BadgePalette(background, foreground, cornerRadius, padding);
        }

        public static BadgePalette CreateFallback(CornerRadius controlCornerRadius)
        {
            return new BadgePalette(
                new ImmutableSolidColorBrush(Color.FromRgb(49, 130, 206)),
                new ImmutableSolidColorBrush(Colors.White),
                controlCornerRadius.TopLeft > 0 ? controlCornerRadius.TopLeft : 8,
                6);
        }
    }

    private static BrushState BuildBrushState(ResourceAccessor accessor, string keyPrefix)
    {
        var normal = accessor.GetBrush(keyPrefix);
        var pointer = accessor.GetBrush($"{keyPrefix}PointerOver");
        var pressed = accessor.GetBrush($"{keyPrefix}Pressed");
        var disabled = accessor.GetBrush($"{keyPrefix}Disabled");
        var focused = accessor.GetBrush($"{keyPrefix}Focused");

        return new BrushState(normal, pointer, pressed, disabled, focused);
    }
}
