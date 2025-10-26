using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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
    private static IReadOnlyList<string> _missingResources = Array.Empty<string>();

    public static PaletteData Current
    {
        get
        {
            EnsureInitialized();
            return _current;
        }
    }

    public static IReadOnlyList<string> MissingResources
    {
        get
        {
            EnsureInitialized();
            return _missingResources;
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

        _missingResources = WidgetFluentResourceProbe.Probe(accessor);

        return new PaletteData(
            controlCornerRadius,
            ButtonPalette.Create(accessor, controlCornerRadius),
            RepeatButtonPalette.Create(accessor),
            SpinnerPalette.Create(accessor),
            CheckBoxPalette.Create(accessor, controlCornerRadius),
            RadioButtonPalette.Create(accessor),
            ToggleSwitchPalette.Create(accessor),
            RangeBasePalette.Create(accessor),
            SliderPalette.Create(accessor),
            ProgressPalette.Create(accessor),
            ScrollBarPalette.Create(accessor),
            ChartPalette.Create(accessor),
            BadgePalette.Create(accessor, controlCornerRadius),
            TextPalette.Create(accessor),
            BorderPalette.Create(accessor),
            SelectionPalette.Create(accessor),
            TabPalette.Create(accessor, controlCornerRadius),
            MenuPalette.Create(accessor),
            CalendarPalette.Create(accessor),
            PickerPalette.Create(accessor),
            FlyoutPalette.Create(accessor),
            ShapePalette.Create(accessor),
            ItemsPalette.Create(accessor),
            LayoutPalette.Create(accessor, controlCornerRadius));
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

        public double? GetDouble(string key)
        {
            var value = Get(key);
            return value switch
            {
                double d => d,
                float f => f,
                int i => i,
                long l => l,
                null => null,
                _ => null
            };
        }

        public FontFamily? GetFontFamily(string key)
        {
            return Get(key) as FontFamily;
        }

        public FontWeight? GetFontWeight(string key)
        {
            return Get(key) is FontWeight weight ? weight : null;
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

        public bool HasAnyValue =>
            Normal is not null
            || PointerOver is not null
            || Pressed is not null
            || Disabled is not null
            || Focused is not null;

        public BrushState WithFallback(BrushState fallback)
        {
            var normal = Normal ?? fallback.Normal;
            var pointer = PointerOver ?? fallback.PointerOver ?? normal;
            var pressed = Pressed ?? fallback.Pressed ?? pointer;
            var disabled = Disabled ?? fallback.Disabled ?? normal;
            var focused = Focused ?? fallback.Focused ?? pointer ?? normal;

            return new BrushState(normal, pointer, pressed, disabled, focused);
        }
    }

    internal sealed record PaletteData(
        CornerRadius ControlCornerRadius,
        ButtonPalette Button,
        RepeatButtonPalette RepeatButton,
        SpinnerPalette Spinner,
        CheckBoxPalette CheckBox,
        RadioButtonPalette RadioButton,
        ToggleSwitchPalette ToggleSwitch,
        RangeBasePalette Range,
        SliderPalette Slider,
        ProgressPalette Progress,
        ScrollBarPalette ScrollBar,
        ChartPalette Chart,
        BadgePalette Badge,
        TextPalette Text,
        BorderPalette Border,
        SelectionPalette Selection,
        TabPalette Tab,
        MenuPalette Menu,
        CalendarPalette Calendar,
        PickerPalette Picker,
        FlyoutPalette Flyout,
        ShapePalette Shape,
        ItemsPalette Items,
        LayoutPalette Layout)
    {
        public static PaletteData CreateFallback()
        {
            var radius = new CornerRadius(4);
            return new PaletteData(
                radius,
                ButtonPalette.CreateFallback(radius),
                RepeatButtonPalette.CreateFallback(),
                SpinnerPalette.CreateFallback(),
                CheckBoxPalette.CreateFallback(radius),
                RadioButtonPalette.CreateFallback(),
                ToggleSwitchPalette.CreateFallback(),
                RangeBasePalette.CreateFallback(),
                SliderPalette.CreateFallback(),
                ProgressPalette.CreateFallback(),
                ScrollBarPalette.CreateFallback(),
                ChartPalette.CreateFallback(),
                BadgePalette.CreateFallback(radius),
                TextPalette.CreateFallback(),
                BorderPalette.CreateFallback(),
                SelectionPalette.CreateFallback(),
                TabPalette.CreateFallback(),
                MenuPalette.CreateFallback(),
                CalendarPalette.CreateFallback(),
                PickerPalette.CreateFallback(),
                FlyoutPalette.CreateFallback(),
                ShapePalette.CreateFallback(),
                ItemsPalette.CreateFallback(),
                LayoutPalette.CreateFallback(radius));
        }
    }

    internal sealed record ButtonPalette(
        ButtonVariantPalette Standard,
        ButtonVariantPalette Accent,
        ButtonVariantPalette Subtle,
        ButtonVariantPalette Destructive,
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
                ButtonVariantPalette.Create(accessor, "Button", ButtonWidgetVariant.Standard),
                ButtonVariantPalette.Create(accessor, "AccentButton", ButtonWidgetVariant.Accent),
                ButtonVariantPalette.Create(accessor, "SubtleButton", ButtonWidgetVariant.Subtle),
                ButtonVariantPalette.Create(accessor, "DestructiveButton", ButtonWidgetVariant.Destructive),
                padding,
                Math.Max(0, borderThickness.Left),
                cornerRadius);
        }

        public static ButtonPalette CreateFallback(CornerRadius defaultCornerRadius)
        {
            var padding = new Thickness(8, 5, 8, 6);
            return new ButtonPalette(
                ButtonVariantPalette.CreateFallback(ButtonWidgetVariant.Standard),
                ButtonVariantPalette.CreateFallback(ButtonWidgetVariant.Accent),
                ButtonVariantPalette.CreateFallback(ButtonWidgetVariant.Subtle),
                ButtonVariantPalette.CreateFallback(ButtonWidgetVariant.Destructive),
                padding,
                1,
                defaultCornerRadius);
        }

        public ButtonVariantPalette GetVariant(ButtonWidgetVariant variant)
        {
            return variant switch
            {
                ButtonWidgetVariant.Accent => Accent,
                ButtonWidgetVariant.Subtle => Subtle,
                ButtonWidgetVariant.Destructive => Destructive,
                _ => Standard
            };
        }
    }

    internal sealed record RepeatButtonPalette(
        BrushState Background,
        BrushState Foreground,
        BrushState Border)
    {
        public static RepeatButtonPalette Create(ResourceAccessor accessor)
        {
            return new RepeatButtonPalette(
                BuildBrushState(accessor, "RepeatButtonBackground"),
                BuildBrushState(accessor, "RepeatButtonForeground"),
                BuildBrushState(accessor, "RepeatButtonBorderBrush"));
        }

        public static RepeatButtonPalette CreateFallback()
        {
            var background = new BrushState(
                new ImmutableSolidColorBrush(Color.FromRgb(0xF5, 0xF5, 0xF5)),
                new ImmutableSolidColorBrush(Color.FromRgb(0xE5, 0xE5, 0xE5)),
                new ImmutableSolidColorBrush(Color.FromRgb(0xD5, 0xD5, 0xD5)),
                new ImmutableSolidColorBrush(Color.FromRgb(0xEE, 0xEE, 0xEE)));

            var foreground = new BrushState(
                new ImmutableSolidColorBrush(Color.FromRgb(0x33, 0x33, 0x33)),
                new ImmutableSolidColorBrush(Color.FromRgb(0x24, 0x62, 0x9C)),
                new ImmutableSolidColorBrush(Color.FromRgb(0x1C, 0x4E, 0x79)),
                new ImmutableSolidColorBrush(Color.FromRgb(0x99, 0x99, 0x99)));

            var border = new BrushState(
                new ImmutableSolidColorBrush(Color.FromArgb(0x00, 0x00, 0x00, 0x00)),
                new ImmutableSolidColorBrush(Color.FromArgb(0x00, 0x00, 0x00, 0x00)),
                new ImmutableSolidColorBrush(Color.FromArgb(0x33, 0x24, 0x62, 0x9C)),
                new ImmutableSolidColorBrush(Color.FromArgb(0x00, 0x00, 0x00, 0x00)));

            return new RepeatButtonPalette(background, foreground, border);
        }
    }

    internal sealed record SpinnerPalette(
        ImmutableSolidColorBrush? Background,
        ImmutableSolidColorBrush? Foreground,
        ImmutableSolidColorBrush? DisabledForeground,
        ImmutableSolidColorBrush? BorderBrush,
        Thickness BorderThickness,
        ImmutableSolidColorBrush? TrackBrush,
        ImmutableSolidColorBrush? TrackStrokeBrush,
        double TrackStrokeThickness,
        Geometry? IncreaseGlyph,
        Geometry? DecreaseGlyph)
    {
        public static SpinnerPalette Create(ResourceAccessor accessor)
        {
            var fallback = CreateFallback();

            var borderThickness = accessor.GetThickness("TextControlBorderThemeThickness") ?? fallback.BorderThickness;
            var trackStrokeThickness = borderThickness.Left > 0 ? borderThickness.Left : fallback.TrackStrokeThickness;

            return new SpinnerPalette(
                accessor.GetBrush("TextControlBackground") ?? fallback.Background,
                accessor.GetBrush("TextControlForeground") ?? fallback.Foreground,
                accessor.GetBrush("TextControlForegroundDisabled") ?? fallback.DisabledForeground,
                accessor.GetBrush("TextControlBorderBrush") ?? fallback.BorderBrush,
                borderThickness,
                accessor.GetBrush("TextControlButtonBackground") ?? fallback.TrackBrush,
                accessor.GetBrush("TextControlButtonBorderBrush") ?? fallback.TrackStrokeBrush,
                trackStrokeThickness,
                accessor.GetGeometry("ButtonSpinnerIncreaseButtonIcon") ?? fallback.IncreaseGlyph,
                accessor.GetGeometry("ButtonSpinnerDecreaseButtonIcon") ?? fallback.DecreaseGlyph);
        }

        public static SpinnerPalette CreateFallback()
        {
            return new SpinnerPalette(
                new ImmutableSolidColorBrush(Color.FromRgb(0xF5, 0xF5, 0xF5)),
                new ImmutableSolidColorBrush(Color.FromRgb(0x33, 0x33, 0x33)),
                new ImmutableSolidColorBrush(Color.FromRgb(0x99, 0x99, 0x99)),
                new ImmutableSolidColorBrush(Color.FromRgb(0xCD, 0xCD, 0xCD)),
                new Thickness(1),
                new ImmutableSolidColorBrush(Color.FromRgb(0xF8, 0xF8, 0xF8)),
                new ImmutableSolidColorBrush(Color.FromRgb(0xD0, 0xD0, 0xD0)),
                1,
                StreamGeometry.Parse("M0,8 L10,-1 20,8 18,10 10,3 2,10 z"),
                StreamGeometry.Parse("M0,1 L10,10 20,1 18,-1 10,6 2,-1 z"));
        }
    }

    internal sealed record ButtonVariantPalette(
        BrushState Background,
        BrushState Border,
        BrushState Foreground)
    {
        public static ButtonVariantPalette Create(ResourceAccessor accessor, string prefix, ButtonWidgetVariant variant)
        {
            var fallback = CreateFallback(variant);

            var background = BuildBrushState(accessor, $"{prefix}Background");
            var border = BuildBrushState(accessor, $"{prefix}BorderBrush");
            var foreground = BuildBrushState(accessor, $"{prefix}Foreground");

            var hasThemeValues = background.HasAnyValue || border.HasAnyValue || foreground.HasAnyValue;

            background = background.WithFallback(fallback.Background);
            border = border.WithFallback(fallback.Border);
            foreground = foreground.WithFallback(fallback.Foreground);

            return hasThemeValues
                ? new ButtonVariantPalette(background, border, foreground)
                : fallback;
        }

        public static ButtonVariantPalette CreateFallback(ButtonWidgetVariant variant)
        {
            return variant switch
            {
                ButtonWidgetVariant.Accent => CreateAccentFallback(),
                ButtonWidgetVariant.Subtle => CreateSubtleFallback(),
                ButtonWidgetVariant.Destructive => CreateDestructiveFallback(),
                _ => CreateStandardFallback()
            };
        }

        private static ButtonVariantPalette CreateStandardFallback()
        {
            var background = new BrushState(
                new ImmutableSolidColorBrush(Color.FromRgb(0xF2, 0xF2, 0xF2)),
                new ImmutableSolidColorBrush(Color.FromRgb(0xEE, 0xEE, 0xEE)),
                new ImmutableSolidColorBrush(Color.FromRgb(0xDD, 0xDD, 0xDD)),
                new ImmutableSolidColorBrush(Color.FromRgb(0xEB, 0xEB, 0xEB)));

            var border = new BrushState(
                new ImmutableSolidColorBrush(Color.FromRgb(0xCD, 0xCD, 0xCD)),
                new ImmutableSolidColorBrush(Color.FromRgb(0xB5, 0xB5, 0xB5)),
                new ImmutableSolidColorBrush(Color.FromRgb(0x96, 0x96, 0x96)),
                new ImmutableSolidColorBrush(Color.FromRgb(0xD2, 0xD2, 0xD2)));

            var foreground = new BrushState(
                new ImmutableSolidColorBrush(Color.FromRgb(0x33, 0x33, 0x33)),
                new ImmutableSolidColorBrush(Color.FromRgb(0x21, 0x21, 0x21)),
                new ImmutableSolidColorBrush(Color.FromRgb(0x21, 0x21, 0x21)),
                new ImmutableSolidColorBrush(Color.FromRgb(0x99, 0x99, 0x99)));

            return new ButtonVariantPalette(background, border, foreground);
        }

        private static ButtonVariantPalette CreateAccentFallback()
        {
            var background = new BrushState(
                new ImmutableSolidColorBrush(Color.FromRgb(0x31, 0x82, 0xCE)),
                new ImmutableSolidColorBrush(Color.FromRgb(0x42, 0x8E, 0xD8)),
                new ImmutableSolidColorBrush(Color.FromRgb(0x24, 0x62, 0x9C)),
                new ImmutableSolidColorBrush(Color.FromRgb(0xB2, 0xD6, 0xF2)));

            var border = new BrushState(
                new ImmutableSolidColorBrush(Color.FromRgb(0x24, 0x62, 0x9C)),
                new ImmutableSolidColorBrush(Color.FromRgb(0x31, 0x82, 0xCE)),
                new ImmutableSolidColorBrush(Color.FromRgb(0x1F, 0x52, 0x80)),
                new ImmutableSolidColorBrush(Color.FromRgb(0x9E, 0xB9, 0xD4)));

            var foreground = new BrushState(
                new ImmutableSolidColorBrush(Colors.White),
                new ImmutableSolidColorBrush(Colors.White),
                new ImmutableSolidColorBrush(Colors.White),
                new ImmutableSolidColorBrush(Color.FromRgb(0xE5, 0xE5, 0xE5)));

            return new ButtonVariantPalette(background, border, foreground);
        }

        private static ButtonVariantPalette CreateSubtleFallback()
        {
            var transparent = new ImmutableSolidColorBrush(Color.FromArgb(0x00, 0x00, 0x00, 0x00));

            var background = new BrushState(
                transparent,
                new ImmutableSolidColorBrush(Color.FromArgb(0x1A, 0x00, 0x00, 0x00)),
                new ImmutableSolidColorBrush(Color.FromArgb(0x26, 0x00, 0x00, 0x00)),
                transparent);

            var border = new BrushState(
                transparent,
                transparent,
                transparent,
                transparent);

            var foreground = new BrushState(
                new ImmutableSolidColorBrush(Color.FromRgb(0x33, 0x33, 0x33)),
                new ImmutableSolidColorBrush(Color.FromRgb(0x24, 0x24, 0x24)),
                new ImmutableSolidColorBrush(Color.FromRgb(0x24, 0x24, 0x24)),
                new ImmutableSolidColorBrush(Color.FromRgb(0x99, 0x99, 0x99)));

            return new ButtonVariantPalette(background, border, foreground);
        }

        private static ButtonVariantPalette CreateDestructiveFallback()
        {
            var background = new BrushState(
                new ImmutableSolidColorBrush(Color.FromRgb(0xFD, 0xE7, 0xE9)),
                new ImmutableSolidColorBrush(Color.FromRgb(0xF9, 0xD5, 0xD9)),
                new ImmutableSolidColorBrush(Color.FromRgb(0xF5, 0xB3, 0xBA)),
                new ImmutableSolidColorBrush(Color.FromRgb(0xF7, 0xEC, 0xED)));

            var border = new BrushState(
                new ImmutableSolidColorBrush(Color.FromRgb(0xD4, 0x5D, 0x64)),
                new ImmutableSolidColorBrush(Color.FromRgb(0xC7, 0x52, 0x59)),
                new ImmutableSolidColorBrush(Color.FromRgb(0xB5, 0x42, 0x48)),
                new ImmutableSolidColorBrush(Color.FromRgb(0xE0, 0xB7, 0xBA)));

            var foreground = new BrushState(
                new ImmutableSolidColorBrush(Color.FromRgb(0x7A, 0x11, 0x17)),
                new ImmutableSolidColorBrush(Color.FromRgb(0x5C, 0x0C, 0x10)),
                new ImmutableSolidColorBrush(Color.FromRgb(0x5C, 0x0C, 0x10)),
                new ImmutableSolidColorBrush(Color.FromRgb(0xB3, 0x7C, 0x80)));

            return new ButtonVariantPalette(background, border, foreground);
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

    internal sealed record RangeBasePalette(
        BrushState TrackFill,
        BrushState ValueFill,
        BrushState ThumbFill,
        BrushState ThumbBorder,
        double TrackThickness,
        double MinimumThumbLength)
    {
        public static RangeBasePalette Create(ResourceAccessor accessor)
        {
            var track = BuildBrushState(accessor, "SliderTrackFill");
            var value = BuildBrushState(accessor, "SliderTrackValueFill");
            var thumb = BuildBrushState(accessor, "SliderThumbBackground");
            var thumbBorder = BuildBrushState(accessor, "SliderThumbBorderBrush");
            var accentBorder = accessor.GetBrush("AccentControlBorderBrush") ?? new ImmutableSolidColorBrush(Color.FromRgb(0x66, 0x66, 0x66));
            thumbBorder = new BrushState(
                thumbBorder.Normal ?? accentBorder,
                thumbBorder.PointerOver ?? accentBorder,
                thumbBorder.Pressed ?? accentBorder,
                thumbBorder.Disabled ?? new ImmutableSolidColorBrush(Color.FromRgb(0xC0, 0xC0, 0xC0)),
                thumbBorder.Focused ?? accentBorder);

            var trackThickness = accessor.GetDouble("SliderTrackThemeHeight") ?? 2;
            var minimumThumb = accessor.GetDouble("SliderThumbMinLength") ?? 14;

            return new RangeBasePalette(
                track,
                value,
                thumb,
                thumbBorder,
                Math.Max(1, trackThickness),
                Math.Max(6, minimumThumb));
        }

        public static RangeBasePalette CreateFallback()
        {
            var track = new ImmutableSolidColorBrush(Color.FromRgb(0xDC, 0xDC, 0xDC));
            var value = new ImmutableSolidColorBrush(Color.FromRgb(0x31, 0x82, 0xCE));
            var thumb = new ImmutableSolidColorBrush(Colors.White);
            var thumbBorder = new ImmutableSolidColorBrush(Color.FromRgb(0xC8, 0xC8, 0xC8));

            return new RangeBasePalette(
                new BrushState(track, track, track, new ImmutableSolidColorBrush(Color.FromRgb(0xE5, 0xE5, 0xE5))),
                new BrushState(value, value, value, new ImmutableSolidColorBrush(Color.FromRgb(0xB2, 0xD6, 0xF2))),
                new BrushState(thumb, thumb, thumb, new ImmutableSolidColorBrush(Color.FromRgb(0xF5, 0xF5, 0xF5))),
                new BrushState(thumbBorder, thumbBorder, thumbBorder, new ImmutableSolidColorBrush(Color.FromRgb(0xD0, 0xD0, 0xD0))),
                2,
                14);
        }
    }

    internal sealed record SliderPalette(RangeBasePalette Range)
    {
        public static SliderPalette Create(ResourceAccessor accessor)
        {
            return new SliderPalette(RangeBasePalette.Create(accessor));
        }

        public static SliderPalette CreateFallback()
        {
            return new SliderPalette(RangeBasePalette.CreateFallback());
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

    internal sealed record ScrollBarPalette(
        BrushState TrackFill,
        BrushState TrackStroke,
        double TrackBorderThickness,
        BrushState ThumbFill,
        BrushState ThumbBorder,
        double Thickness,
        double MinimumThumbLength)
    {
        public static ScrollBarPalette Create(ResourceAccessor accessor)
        {
            var rangeFallback = RangeBasePalette.CreateFallback();

            var trackFill = BuildBrushState(accessor, "ScrollBarTrackFill").WithFallback(rangeFallback.TrackFill);
            var trackStroke = BuildBrushState(accessor, "ScrollBarTrackStroke");

            var thumbRaw = BuildBrushState(accessor, "ScrollBarThumbFill");
            var thumbNormal = thumbRaw.Normal
                              ?? accessor.GetBrush("ScrollBarThumbBackgroundColor")
                              ?? rangeFallback.ThumbFill.Normal
                              ?? new ImmutableSolidColorBrush(Color.FromRgb(0x9C, 0x9C, 0x9C));
            var thumbPointer = thumbRaw.PointerOver ?? rangeFallback.ThumbFill.PointerOver ?? thumbNormal;
            var thumbPressed = thumbRaw.Pressed ?? rangeFallback.ThumbFill.Pressed ?? thumbPointer;
            var thumbDisabled = thumbRaw.Disabled ?? rangeFallback.ThumbFill.Disabled ?? new ImmutableSolidColorBrush(Color.FromArgb(0x80, thumbNormal.Color.R, thumbNormal.Color.G, thumbNormal.Color.B));
            var thumbFocused = thumbRaw.Focused ?? thumbPointer;
            var thumbFill = new BrushState(thumbNormal, thumbPointer, thumbPressed, thumbDisabled, thumbFocused);

            var thumbBorderRaw = BuildBrushState(accessor, "ScrollBarThumbBorderBrush");
            var accentBorder = accessor.GetBrush("AccentControlBorderBrush") ?? rangeFallback.ThumbBorder.Normal ?? new ImmutableSolidColorBrush(Color.FromRgb(0x66, 0x66, 0x66));
            var thumbBorder = thumbBorderRaw.HasAnyValue
                ? new BrushState(
                    thumbBorderRaw.Normal ?? accentBorder,
                    thumbBorderRaw.PointerOver ?? accentBorder,
                    thumbBorderRaw.Pressed ?? accentBorder,
                    thumbBorderRaw.Disabled ?? accentBorder,
                    thumbBorderRaw.Focused ?? accentBorder)
                : new BrushState(accentBorder, accentBorder, accentBorder, accentBorder, accentBorder);

            var thickness = accessor.GetDouble("ScrollBarSize") ?? 16;
            var minimumThumb = accessor.GetDouble("ScrollBarMinimumThumbLength") ?? Math.Max(8, thickness);

            var trackBorderThickness = accessor.GetDouble("ScrollBarTrackBorderThemeThickness") ?? 0;

            return new ScrollBarPalette(
                trackFill,
                trackStroke,
                Math.Max(0, trackBorderThickness),
                thumbFill,
                thumbBorder,
                Math.Max(4, thickness),
                Math.Max(8, minimumThumb));
        }

        public static ScrollBarPalette CreateFallback()
        {
            var track = new ImmutableSolidColorBrush(Color.FromRgb(0xD0, 0xD0, 0xD0));
            var trackStroke = new ImmutableSolidColorBrush(Color.FromArgb(0x40, 0x00, 0x00, 0x00));
            var thumb = new ImmutableSolidColorBrush(Color.FromRgb(0x9C, 0x9C, 0x9C));
            var thumbPointer = new ImmutableSolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88));
            var thumbPressed = new ImmutableSolidColorBrush(Color.FromRgb(0x70, 0x70, 0x70));
            var thumbDisabled = new ImmutableSolidColorBrush(Color.FromRgb(0xCC, 0xCC, 0xCC));
            var border = new ImmutableSolidColorBrush(Color.FromRgb(0x66, 0x66, 0x66));

            return new ScrollBarPalette(
                new BrushState(track, track, track, new ImmutableSolidColorBrush(Color.FromRgb(0xE5, 0xE5, 0xE5))),
                new BrushState(trackStroke, trackStroke, trackStroke, new ImmutableSolidColorBrush(Color.FromArgb(0x20, 0x00, 0x00, 0x00))),
                0,
                new BrushState(thumb, thumbPointer, thumbPressed, thumbDisabled, thumbPointer),
                new BrushState(border, border, border, border, border),
                16,
                12);
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

    internal sealed record TextPalette(
        BrushState Foreground,
        BrushState Placeholder,
        ImmutableSolidColorBrush? SelectionHighlight,
        ImmutableSolidColorBrush? SelectionForeground,
        ImmutableSolidColorBrush? CaretBrush,
        ImmutableSolidColorBrush? HeaderForeground,
        Thickness? HeaderMargin,
        BrushState Hyperlink,
        TextTypographyPalette Typography)
    {
        public static TextPalette Create(ResourceAccessor accessor)
        {
            var selection = accessor.GetBrush("TextControlSelectionHighlightColor") ??
                            accessor.GetBrush("SystemControlHighlightAccentBrush");
            var selectionForeground = accessor.GetBrush("TextControlSelectionForeground") ??
                                      accessor.GetBrush("SystemControlForegroundChromeWhiteBrush") ??
                                      accessor.GetBrush("TextControlForegroundFocused") ??
                                      accessor.GetBrush("TextControlForeground");
            var caret = accessor.GetBrush("TextControlForeground") ??
                        new ImmutableSolidColorBrush(Color.FromRgb(0x33, 0x33, 0x33));

            return new TextPalette(
                BuildBrushState(accessor, "TextControlForeground"),
                BuildBrushState(accessor, "TextControlPlaceholderForeground"),
                selection,
                selectionForeground,
                caret,
                accessor.GetBrush("GroupBoxHeaderForeground") ?? selectionForeground,
                accessor.GetThickness("GroupBoxHeaderMargin"),
                CreateHyperlinkBrushState(accessor),
                TextTypographyPalette.Create(accessor));
        }

        public static TextPalette CreateFallback()
        {
            var foreground = new BrushState(
                new ImmutableSolidColorBrush(Color.FromRgb(0x33, 0x33, 0x33)),
                new ImmutableSolidColorBrush(Color.FromRgb(0x33, 0x33, 0x33)),
                new ImmutableSolidColorBrush(Color.FromRgb(0x33, 0x33, 0x33)),
                new ImmutableSolidColorBrush(Color.FromRgb(0x99, 0x99, 0x99)),
                new ImmutableSolidColorBrush(Color.FromRgb(0x33, 0x33, 0x33)));

            var placeholder = new BrushState(
                new ImmutableSolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88)),
                new ImmutableSolidColorBrush(Color.FromRgb(0x70, 0x70, 0x70)),
                new ImmutableSolidColorBrush(Color.FromRgb(0x66, 0x66, 0x66)),
                new ImmutableSolidColorBrush(Color.FromRgb(0xB5, 0xB5, 0xB5)),
                new ImmutableSolidColorBrush(Color.FromRgb(0x70, 0x70, 0x70)));

            var accent = new ImmutableSolidColorBrush(Color.FromRgb(0x31, 0x82, 0xCE));
            var hyperlink = new BrushState(
                accent,
                new ImmutableSolidColorBrush(Color.FromRgb(0x24, 0x62, 0x9C)),
                new ImmutableSolidColorBrush(Color.FromRgb(0x1B, 0x4B, 0x79)),
                new ImmutableSolidColorBrush(Color.FromRgb(0x99, 0x99, 0x99)),
                new ImmutableSolidColorBrush(Color.FromRgb(0x24, 0x62, 0x9C)));

            return new TextPalette(
                foreground,
                placeholder,
                new ImmutableSolidColorBrush(Color.FromArgb(0x99, 0x31, 0x82, 0xCE)),
                new ImmutableSolidColorBrush(Colors.White),
                new ImmutableSolidColorBrush(Color.FromRgb(0x33, 0x33, 0x33)),
                new ImmutableSolidColorBrush(Color.FromRgb(0x33, 0x33, 0x33)),
                new Thickness(0, 4, 0, 12),
                hyperlink,
                TextTypographyPalette.CreateFallback());
        }

        private static BrushState CreateHyperlinkBrushState(ResourceAccessor accessor)
        {
            var normal = accessor.GetBrush("SystemControlHyperlinkTextBrush") ??
                         accessor.GetBrush("SystemControlHighlightAccentBrush");
            var pointerOver = accessor.GetBrush("SystemControlHyperlinkBaseHighBrush") ?? normal;
            var pressed = accessor.GetBrush("SystemControlHyperlinkBaseMediumBrush") ?? normal;
            var disabled = accessor.GetBrush("SystemControlDisabledChromeDisabledLowBrush") ??
                           accessor.GetBrush("SystemControlDisabledBaseLowBrush") ??
                           normal;
            var focused = accessor.GetBrush("SystemControlHyperlinkBaseMediumHighBrush") ?? pointerOver;

            return new BrushState(normal, pointerOver, pressed, disabled, focused);
        }
    }

    internal sealed record TextTypographyPalette(
        TypographyStyle Body,
        TypographyStyle Caption,
        TypographyStyle Header,
        TypographyStyle Title)
    {
        public static TextTypographyPalette Create(ResourceAccessor accessor)
        {
            var bodyFamily = accessor.GetFontFamily("ContentControlThemeFontFamily") ?? FontFamily.Default;
            var bodySize = accessor.GetDouble("ControlContentThemeFontSize") ?? 14;
            var bodyWeight = accessor.GetFontWeight("ContentControlThemeFontWeight") ?? FontWeight.Normal;

            var captionSize = accessor.GetDouble("ToolTipContentThemeFontSize") ?? Math.Max(10, bodySize - 2);
            var captionWeight = accessor.GetFontWeight("ToolTipContentThemeFontWeight") ?? FontWeight.Normal;

            var headerSize = accessor.GetDouble("GroupBoxHeaderFontSize") ?? Math.Max(bodySize + 2, 16);
            var headerWeight = accessor.GetFontWeight("GroupBoxHeaderFontWeight") ?? FontWeight.SemiBold;

            var titleSize = accessor.GetDouble("TabItemHeaderFontSize") ?? Math.Max(headerSize, bodySize + 4);
            var titleWeight = accessor.GetFontWeight("TabItemHeaderThemeFontWeight") ?? FontWeight.SemiLight;

            return new TextTypographyPalette(
                new TypographyStyle(bodyFamily, bodySize, bodyWeight),
                new TypographyStyle(bodyFamily, captionSize, captionWeight),
                new TypographyStyle(bodyFamily, headerSize, headerWeight),
                new TypographyStyle(bodyFamily, titleSize, titleWeight));
        }

        public static TextTypographyPalette CreateFallback()
        {
            var bodyFamily = FontFamily.Default;
            return new TextTypographyPalette(
                new TypographyStyle(bodyFamily, 14, FontWeight.Normal),
                new TypographyStyle(bodyFamily, 12, FontWeight.Normal),
                new TypographyStyle(bodyFamily, 16, FontWeight.SemiBold),
                new TypographyStyle(bodyFamily, 20, FontWeight.SemiLight));
        }
    }

    internal sealed record TypographyStyle(FontFamily FontFamily, double FontSize, FontWeight FontWeight);

    internal sealed record BorderPalette(
        BrushState ControlBorder,
        ImmutableSolidColorBrush? FocusStroke,
        ImmutableSolidColorBrush? Divider)
    {
        public static BorderPalette Create(ResourceAccessor accessor)
        {
            var focus = accessor.GetBrush("TextControlBorderBrushFocused") ??
                        accessor.GetBrush("SystemControlHighlightAccentBrush");
            var divider = accessor.GetBrush("SystemControlForegroundBaseLowBrush") ??
                          accessor.GetBrush("SystemControlDisabledBaseLowBrush");

            return new BorderPalette(
                BuildBrushState(accessor, "TextControlBorderBrush"),
                focus,
                divider);
        }

        public static BorderPalette CreateFallback()
        {
            return new BorderPalette(
                new BrushState(
                    new ImmutableSolidColorBrush(Color.FromRgb(0xCD, 0xCD, 0xCD)),
                    new ImmutableSolidColorBrush(Color.FromRgb(0xB5, 0xB5, 0xB5)),
                    new ImmutableSolidColorBrush(Color.FromRgb(0x96, 0x96, 0x96)),
                    new ImmutableSolidColorBrush(Color.FromRgb(0xE0, 0xE0, 0xE0)),
                    new ImmutableSolidColorBrush(Color.FromRgb(0x24, 0x62, 0x9C))),
                new ImmutableSolidColorBrush(Color.FromRgb(0x24, 0x62, 0x9C)),
                new ImmutableSolidColorBrush(Color.FromRgb(0xE0, 0xE0, 0xE0)));
        }
    }

    internal sealed record SelectionPalette(
        ImmutableSolidColorBrush? SelectedBackground,
        ImmutableSolidColorBrush? SelectedForeground,
        ImmutableSolidColorBrush? PointerOverBackground,
        ImmutableSolidColorBrush? PointerOverForeground,
        ImmutableSolidColorBrush? InactiveBackground,
        ImmutableSolidColorBrush? InactiveForeground)
    {
        public static SelectionPalette Create(ResourceAccessor accessor)
        {
            var selectedBackground = accessor.GetBrush("SystemControlHighlightListAccentLowBrush");
            var selectedForeground = accessor.GetBrush("SystemControlForegroundChromeWhiteBrush") ??
                                     accessor.GetBrush("SystemControlForegroundBaseHighBrush");
            var pointerOverBackground = accessor.GetBrush("SystemControlHighlightListAccentMediumBrush") ?? selectedBackground;
            var pointerOverForeground = accessor.GetBrush("SystemControlForegroundChromeWhiteBrush") ?? selectedForeground;
            var inactiveBackground = accessor.GetBrush("SystemControlHighlightListLowBrush") ??
                                     accessor.GetBrush("SystemControlHighlightListAccentLowBrush");
            var inactiveForeground = accessor.GetBrush("SystemControlForegroundBaseHighBrush") ?? selectedForeground;

            return new SelectionPalette(
                selectedBackground,
                selectedForeground,
                pointerOverBackground,
                pointerOverForeground,
                inactiveBackground,
                inactiveForeground);
        }

        public static SelectionPalette CreateFallback()
        {
            return new SelectionPalette(
                new ImmutableSolidColorBrush(Color.FromRgb(0x31, 0x82, 0xCE)),
                new ImmutableSolidColorBrush(Colors.White),
                new ImmutableSolidColorBrush(Color.FromRgb(0x42, 0x8E, 0xD8)),
                new ImmutableSolidColorBrush(Colors.White),
                new ImmutableSolidColorBrush(Color.FromRgb(0xE0, 0xE0, 0xE0)),
                new ImmutableSolidColorBrush(Color.FromRgb(0x33, 0x33, 0x33)));
        }
    }

    internal sealed record TabPalette(
        BrushState UnselectedBackground,
        BrushState SelectedBackground,
        BrushState UnselectedForeground,
        BrushState SelectedForeground,
        ImmutableSolidColorBrush? IndicatorBrush,
        double IndicatorThickness,
        Thickness HeaderMargin,
        Thickness HeaderPadding,
        double MinHeight,
        ImmutableSolidColorBrush? StripBackground,
        CornerRadius HeaderCornerRadius)
    {
        public static TabPalette Create(ResourceAccessor accessor, CornerRadius defaultCornerRadius)
        {
            var unselectedBackground = new BrushState(
                accessor.GetBrush("TabItemHeaderBackgroundUnselected"),
                accessor.GetBrush("TabItemHeaderBackgroundUnselectedPointerOver"),
                accessor.GetBrush("TabItemHeaderBackgroundUnselectedPressed"),
                accessor.GetBrush("TabItemHeaderBackgroundDisabled"));

            var selectedBackground = new BrushState(
                accessor.GetBrush("TabItemHeaderBackgroundSelected"),
                accessor.GetBrush("TabItemHeaderBackgroundSelectedPointerOver"),
                accessor.GetBrush("TabItemHeaderBackgroundSelectedPressed"),
                accessor.GetBrush("TabItemHeaderBackgroundDisabled"));

            var unselectedForeground = new BrushState(
                accessor.GetBrush("TabItemHeaderForegroundUnselected"),
                accessor.GetBrush("TabItemHeaderForegroundUnselectedPointerOver"),
                accessor.GetBrush("TabItemHeaderForegroundUnselectedPressed"),
                accessor.GetBrush("TabItemHeaderForegroundDisabled"));

            var selectedForeground = new BrushState(
                accessor.GetBrush("TabItemHeaderForegroundSelected"),
                accessor.GetBrush("TabItemHeaderForegroundSelectedPointerOver"),
                accessor.GetBrush("TabItemHeaderForegroundSelectedPressed"),
                accessor.GetBrush("TabItemHeaderForegroundDisabled"));

            var indicatorBrush = accessor.GetBrush("TabItemHeaderSelectedPipeFill");
            var indicatorThickness = accessor.GetDouble("TabItemPipeThickness") ?? 2;
            var headerMargin = accessor.GetThickness("TabItemHeaderMargin") ?? new Thickness(12, 0, 12, 0);
            var headerPadding = accessor.GetThickness("TabItemMargin") ?? new Thickness(12, 8, 12, 8);
            var minHeight = accessor.GetDouble("TabItemMinHeight") ?? 40;
            var stripBackground = accessor.GetBrush("TabControlBackground") ?? new ImmutableSolidColorBrush(Colors.Transparent);
            var headerCorner = new CornerRadius(
                defaultCornerRadius.TopLeft > 0 ? defaultCornerRadius.TopLeft : 4,
                defaultCornerRadius.TopRight > 0 ? defaultCornerRadius.TopRight : 4,
                0,
                0);

            return new TabPalette(
                unselectedBackground,
                selectedBackground,
                unselectedForeground,
                selectedForeground,
                indicatorBrush,
                indicatorThickness,
                headerMargin,
                headerPadding,
                minHeight,
                stripBackground,
                headerCorner);
        }

        public static TabPalette CreateFallback()
        {
            var transparent = new ImmutableSolidColorBrush(Color.FromArgb(0x00, 0x00, 0x00, 0x00));
            var pointerOver = new ImmutableSolidColorBrush(Color.FromArgb(0x20, 0x31, 0x82, 0xCE));
            var pressed = new ImmutableSolidColorBrush(Color.FromArgb(0x30, 0x31, 0x82, 0xCE));
            var disabledBackground = transparent;
            var neutralForeground = new ImmutableSolidColorBrush(Color.FromRgb(0x55, 0x55, 0x55));
            var accentForeground = new ImmutableSolidColorBrush(Color.FromRgb(0x31, 0x82, 0xCE));
            var disabledForeground = new ImmutableSolidColorBrush(Color.FromRgb(0x99, 0x99, 0x99));
            var selectedForeground = new ImmutableSolidColorBrush(Colors.White);
            var indicator = new ImmutableSolidColorBrush(Color.FromRgb(0x31, 0x82, 0xCE));

            return new TabPalette(
                new BrushState(transparent, pointerOver, pressed, disabledBackground),
                new BrushState(pointerOver, pressed, pressed, disabledBackground),
                new BrushState(neutralForeground, accentForeground, accentForeground, disabledForeground),
                new BrushState(selectedForeground, selectedForeground, selectedForeground, disabledForeground),
                indicator,
                2,
                new Thickness(12, 0, 12, 0),
                new Thickness(12, 8, 12, 8),
                40,
                transparent,
                new CornerRadius(4, 4, 0, 0));
        }
    }

    internal sealed record MenuPalette(
        ImmutableSolidColorBrush? PresenterBackground,
        ImmutableSolidColorBrush? PresenterBorder,
        Thickness PresenterBorderThickness,
        Thickness ItemPadding,
        BrushState ItemBackground,
        BrushState ItemForeground)
    {
        public static MenuPalette Create(ResourceAccessor accessor)
        {
            var background = accessor.GetBrush("MenuFlyoutPresenterBackground") ??
                             accessor.GetBrush("SystemControlTransientBackgroundBrush");
            var border = accessor.GetBrush("MenuFlyoutPresenterBorderBrush") ??
                         accessor.GetBrush("SystemControlTransientBorderBrush");
            var borderThickness = accessor.GetThickness("MenuFlyoutPresenterBorderThemeThickness") ?? new Thickness(1);
            var itemPadding = accessor.GetThickness("MenuFlyoutItemThemePadding") ?? new Thickness(11, 9, 11, 10);

            return new MenuPalette(
                background,
                border,
                borderThickness,
                itemPadding,
                BuildBrushState(accessor, "MenuFlyoutItemBackground"),
                BuildBrushState(accessor, "MenuFlyoutItemForeground"));
        }

        public static MenuPalette CreateFallback()
        {
            return new MenuPalette(
                new ImmutableSolidColorBrush(Color.FromArgb(0xF0, 0x25, 0x25, 0x25)),
                new ImmutableSolidColorBrush(Color.FromArgb(0x40, 0xFF, 0xFF, 0xFF)),
                new Thickness(1),
                new Thickness(11, 9, 11, 10),
                new BrushState(
                    new ImmutableSolidColorBrush(Color.FromArgb(0x00, 0x00, 0x00, 0x00)),
                    new ImmutableSolidColorBrush(Color.FromArgb(0x1A, 0xFF, 0xFF, 0xFF)),
                    new ImmutableSolidColorBrush(Color.FromArgb(0x33, 0xFF, 0xFF, 0xFF)),
                    new ImmutableSolidColorBrush(Color.FromArgb(0x00, 0x00, 0x00, 0x00))),
                new BrushState(
                new ImmutableSolidColorBrush(Color.FromRgb(0xF9, 0xF9, 0xF9)),
                    new ImmutableSolidColorBrush(Color.FromRgb(0xFF, 0xFF, 0xFF)),
                    new ImmutableSolidColorBrush(Color.FromRgb(0xFF, 0xFF, 0xFF)),
                    new ImmutableSolidColorBrush(Color.FromRgb(0x9C, 0x9C, 0x9C))));
        }
    }

    internal sealed record ItemsPalette(
        ImmutableSolidColorBrush? ItemBackground,
        ImmutableSolidColorBrush? ItemForeground,
        ImmutableSolidColorBrush? PointerOverBackground,
        ImmutableSolidColorBrush? PointerOverForeground,
        ImmutableSolidColorBrush? SelectedBackground,
        ImmutableSolidColorBrush? SelectedForeground,
        ImmutableSolidColorBrush? DisabledBackground,
        ImmutableSolidColorBrush? DisabledForeground,
        ImmutableSolidColorBrush? TreeExpanderBrush,
        double TreeIndent,
        double TreeGlyphSize)
    {
        public static ItemsPalette Create(ResourceAccessor accessor)
        {
            var itemBackground = accessor.GetBrush("TreeViewItemBackground") ??
                                 accessor.GetBrush("SystemControlBackgroundChromeMediumLowBrush");
            var itemForeground = accessor.GetBrush("TreeViewItemForeground") ??
                                 accessor.GetBrush("SystemControlForegroundBaseHighBrush");
            var pointerBackground = accessor.GetBrush("TreeViewItemBackgroundPointerOver") ??
                                    accessor.GetBrush("SystemControlHighlightListLowBrush");
            var pointerForeground = accessor.GetBrush("TreeViewItemForegroundPointerOver") ??
                                    accessor.GetBrush("SystemControlHighlightAltBaseHighBrush");
            var selectedBackground = accessor.GetBrush("TreeViewItemBackgroundSelected") ??
                                     accessor.GetBrush("SystemControlHighlightListAccentLowBrush");
            var selectedForeground = accessor.GetBrush("TreeViewItemForegroundSelected") ??
                                     accessor.GetBrush("SystemControlHighlightAltBaseHighBrush");
            var disabledBackground = accessor.GetBrush("TreeViewItemBackgroundDisabled");
            var disabledForeground = accessor.GetBrush("TreeViewItemForegroundDisabled") ??
                                     accessor.GetBrush("SystemControlDisabledBaseMediumLowBrush");
            var treeExpander = accessor.GetBrush("TreeViewItemForeground") ?? itemForeground;
            var indent = accessor.GetDouble("TreeViewItemIndent") ?? 16d;
            var glyphSize = accessor.GetDouble("TreeViewItemExpandCollapseChevronSize") ?? 12d;

            return new ItemsPalette(
                itemBackground,
                itemForeground,
                pointerBackground,
                pointerForeground,
                selectedBackground,
                selectedForeground,
                disabledBackground,
                disabledForeground,
                treeExpander,
                indent,
                glyphSize);
        }

        public static ItemsPalette CreateFallback()
        {
            return new ItemsPalette(
                new ImmutableSolidColorBrush(Color.FromRgb(0xFF, 0xFF, 0xFF)),
                new ImmutableSolidColorBrush(Color.FromRgb(0x20, 0x20, 0x20)),
                new ImmutableSolidColorBrush(Color.FromRgb(0xF0, 0xF0, 0xF0)),
                new ImmutableSolidColorBrush(Color.FromRgb(0x20, 0x20, 0x20)),
                new ImmutableSolidColorBrush(Color.FromRgb(0x31, 0x82, 0xCE)),
                new ImmutableSolidColorBrush(Colors.White),
                new ImmutableSolidColorBrush(Color.FromRgb(0xE0, 0xE0, 0xE0)),
                new ImmutableSolidColorBrush(Color.FromRgb(0x98, 0x98, 0x98)),
                new ImmutableSolidColorBrush(Color.FromRgb(0x60, 0x60, 0x60)),
                16d,
                12d);
        }
    }

    internal sealed record CalendarPalette(
        ImmutableSolidColorBrush? Background,
        ImmutableSolidColorBrush? Border,
        ImmutableSolidColorBrush? SelectedBorder,
        ImmutableSolidColorBrush? SelectedHoverBorder,
        ImmutableSolidColorBrush? SelectedPressedBorder,
        ImmutableSolidColorBrush? SelectedForeground,
        ImmutableSolidColorBrush? TodayForeground,
        ImmutableSolidColorBrush? BlackoutForeground,
        ImmutableSolidColorBrush? OutOfScopeForeground)
    {
        public static CalendarPalette Create(ResourceAccessor accessor)
        {
            return new CalendarPalette(
                accessor.GetBrush("CalendarViewBackground"),
                accessor.GetBrush("CalendarViewBorderBrush"),
                accessor.GetBrush("CalendarViewSelectedBorderBrush"),
                accessor.GetBrush("CalendarViewSelectedHoverBorderBrush"),
                accessor.GetBrush("CalendarViewSelectedPressedBorderBrush"),
                accessor.GetBrush("CalendarViewCalendarItemForeground"),
                accessor.GetBrush("CalendarViewTodayForeground"),
                accessor.GetBrush("CalendarViewBlackoutForeground"),
                accessor.GetBrush("CalendarViewOutOfScopeForeground"));
        }

        public static CalendarPalette CreateFallback()
        {
            return new CalendarPalette(
                new ImmutableSolidColorBrush(Color.FromRgb(0xF6, 0xF6, 0xF6)),
                new ImmutableSolidColorBrush(Color.FromRgb(0xCE, 0xCE, 0xCE)),
                new ImmutableSolidColorBrush(Color.FromRgb(0x31, 0x82, 0xCE)),
                new ImmutableSolidColorBrush(Color.FromRgb(0x42, 0x8E, 0xD8)),
                new ImmutableSolidColorBrush(Color.FromRgb(0x24, 0x62, 0x9C)),
                new ImmutableSolidColorBrush(Color.FromRgb(0x33, 0x33, 0x33)),
                new ImmutableSolidColorBrush(Color.FromRgb(0x1F, 0x52, 0x80)),
                new ImmutableSolidColorBrush(Color.FromRgb(0x99, 0x99, 0x99)),
                new ImmutableSolidColorBrush(Color.FromRgb(0x66, 0x66, 0x66)));
        }
    }

    internal sealed record PickerPalette(
        BrushState ButtonBackground,
        BrushState ButtonBorder,
        BrushState ButtonForeground,
        ImmutableSolidColorBrush? SpacerFill,
        ImmutableSolidColorBrush? FlyoutBackground,
        ImmutableSolidColorBrush? FlyoutBorder,
        ImmutableSolidColorBrush? FlyoutHighlight)
    {
        public static PickerPalette Create(ResourceAccessor accessor)
        {
            var dateBackground = BuildBrushState(accessor, "DatePickerButtonBackground");
            var timeBackground = BuildBrushState(accessor, "TimePickerButtonBackground");
            var background = dateBackground.HasAnyValue ? dateBackground.WithFallback(timeBackground) : timeBackground.WithFallback(dateBackground);

            var dateBorder = BuildBrushState(accessor, "DatePickerButtonBorderBrush");
            var timeBorder = BuildBrushState(accessor, "TimePickerButtonBorderBrush");
            var border = dateBorder.HasAnyValue ? dateBorder.WithFallback(timeBorder) : timeBorder.WithFallback(dateBorder);

            var dateForeground = BuildBrushState(accessor, "DatePickerButtonForeground");
            var timeForeground = BuildBrushState(accessor, "TimePickerButtonForeground");
            var foreground = dateForeground.HasAnyValue ? dateForeground.WithFallback(timeForeground) : timeForeground.WithFallback(dateForeground);

            var spacer = accessor.GetBrush("DatePickerSpacerFill")
                         ?? accessor.GetBrush("TimePickerSpacerFill");
            var flyoutBackground = accessor.GetBrush("DatePickerFlyoutPresenterBackground")
                                   ?? accessor.GetBrush("TimePickerFlyoutPresenterBackground");
            var flyoutBorder = accessor.GetBrush("DatePickerFlyoutPresenterBorderBrush")
                                ?? accessor.GetBrush("TimePickerFlyoutPresenterBorderBrush");
            var flyoutHighlight = accessor.GetBrush("DatePickerFlyoutPresenterHighlightFill")
                                   ?? accessor.GetBrush("TimePickerFlyoutPresenterHighlightFill");

            return new PickerPalette(
                background,
                border,
                foreground,
                spacer,
                flyoutBackground,
                flyoutBorder,
                flyoutHighlight);
        }

        public static PickerPalette CreateFallback()
        {
            var background = new BrushState(
                new ImmutableSolidColorBrush(Color.FromRgb(0xF4, 0xF4, 0xF4)),
                new ImmutableSolidColorBrush(Color.FromRgb(0xE9, 0xE9, 0xE9)),
                new ImmutableSolidColorBrush(Color.FromRgb(0xDD, 0xDD, 0xDD)),
                new ImmutableSolidColorBrush(Color.FromRgb(0xF4, 0xF4, 0xF4)),
                new ImmutableSolidColorBrush(Color.FromRgb(0xE9, 0xE9, 0xE9)));

            var border = new BrushState(
                new ImmutableSolidColorBrush(Color.FromRgb(0xC0, 0xC0, 0xC0)),
                new ImmutableSolidColorBrush(Color.FromRgb(0xA4, 0xA4, 0xA4)),
                new ImmutableSolidColorBrush(Color.FromRgb(0x8F, 0x8F, 0x8F)),
                new ImmutableSolidColorBrush(Color.FromRgb(0xD6, 0xD6, 0xD6)),
                new ImmutableSolidColorBrush(Color.FromRgb(0xA4, 0xA4, 0xA4)));

            var foreground = new BrushState(
                new ImmutableSolidColorBrush(Color.FromRgb(0x20, 0x20, 0x20)),
                new ImmutableSolidColorBrush(Color.FromRgb(0x20, 0x20, 0x20)),
                new ImmutableSolidColorBrush(Color.FromRgb(0x20, 0x20, 0x20)),
                new ImmutableSolidColorBrush(Color.FromRgb(0x90, 0x90, 0x90)),
                new ImmutableSolidColorBrush(Color.FromRgb(0x20, 0x20, 0x20)));

            return new PickerPalette(
                background,
                border,
                foreground,
                new ImmutableSolidColorBrush(Color.FromRgb(0xDC, 0xDC, 0xDC)),
                new ImmutableSolidColorBrush(Color.FromRgb(0xFF, 0xFF, 0xFF)),
                new ImmutableSolidColorBrush(Color.FromRgb(0xC8, 0xC8, 0xC8)),
                new ImmutableSolidColorBrush(Color.FromRgb(0x31, 0x82, 0xCE)));
        }
    }

    internal sealed record FlyoutPalette(
        ImmutableSolidColorBrush? Background,
        ImmutableSolidColorBrush? Border,
        Thickness BorderThickness,
        ImmutableSolidColorBrush? Shadow)
    {
        public static FlyoutPalette Create(ResourceAccessor accessor)
        {
            var background = accessor.GetBrush("MenuFlyoutPresenterBackground") ??
                             accessor.GetBrush("SystemControlTransientBackgroundBrush");
            var border = accessor.GetBrush("MenuFlyoutPresenterBorderBrush") ??
                         accessor.GetBrush("SystemControlTransientBorderBrush");
            var borderThickness = accessor.GetThickness("MenuFlyoutPresenterBorderThemeThickness") ?? new Thickness(1);
            var shadow = accessor.GetBrush("SystemControlShadowBaseColor");

            return new FlyoutPalette(background, border, borderThickness, shadow);
        }

        public static FlyoutPalette CreateFallback()
        {
            return new FlyoutPalette(
                new ImmutableSolidColorBrush(Color.FromArgb(0xF0, 0x25, 0x25, 0x25)),
                new ImmutableSolidColorBrush(Color.FromArgb(0x40, 0xFF, 0xFF, 0xFF)),
                new Thickness(1),
                new ImmutableSolidColorBrush(Color.FromArgb(0x99, 0x00, 0x00, 0x00)));
        }
    }

    internal sealed record ShapePalette(
        ImmutableSolidColorBrush? Stroke,
        ImmutableSolidColorBrush? Fill,
        double StrokeThickness,
        IReadOnlyList<double>? StrokeDashArray,
        double StrokeDashOffset,
        PenLineCap StrokeLineCap,
        PenLineJoin StrokeLineJoin,
        double StrokeMiterLimit)
    {
        public static ShapePalette Create(ResourceAccessor accessor)
        {
            var stroke = accessor.GetBrush("ShapeStrokeBrush") ?? accessor.GetBrush("SystemControlForegroundBaseHighBrush");
            var fill = accessor.GetBrush("ShapeFillBrush") ?? accessor.GetBrush("SystemControlBackgroundBaseLowBrush");
            var thickness = accessor.GetDouble("ShapeStrokeThickness")
                           ?? accessor.GetThickness("ShapeStrokeThickness")?.Left
                           ?? 1;
            var dashArray = ToDashArray(accessor.Get("ShapeStrokeDashArray"));
            var dashOffset = accessor.GetDouble("ShapeStrokeDashOffset") ?? 0;
            var lineCap = accessor.Get("ShapeStrokeLineCap") is PenLineCap cap ? cap : PenLineCap.Flat;
            var lineJoin = accessor.Get("ShapeStrokeLineJoin") is PenLineJoin join ? join : PenLineJoin.Miter;
            var miterLimit = accessor.GetDouble("ShapeStrokeMiterLimit") ?? 10;

            return new ShapePalette(
                stroke ?? new ImmutableSolidColorBrush(Color.FromRgb(0x33, 0x33, 0x33)),
                fill ?? new ImmutableSolidColorBrush(Color.FromArgb(0x40, 0x33, 0x33, 0x33)),
                Math.Max(0, thickness),
                dashArray,
                Math.Max(0, dashOffset),
                lineCap,
                lineJoin,
                Math.Max(0.1, miterLimit));
        }

        public static ShapePalette CreateFallback()
        {
            return new ShapePalette(
                new ImmutableSolidColorBrush(Color.FromRgb(0x33, 0x33, 0x33)),
                new ImmutableSolidColorBrush(Color.FromArgb(0x40, 0x33, 0x33, 0x33)),
                1,
                null,
                0,
                PenLineCap.Flat,
                PenLineJoin.Miter,
                10);
        }

        private static IReadOnlyList<double>? ToDashArray(object? value)
        {
            if (value is null)
            {
                return null;
            }

            if (value is IReadOnlyList<double> readOnlyList)
            {
                return readOnlyList.Count == 0 ? null : readOnlyList.ToArray();
            }

            if (value is IEnumerable<double> enumerable)
            {
                var array = enumerable as double[] ?? enumerable.ToArray();
                return array.Length == 0 ? null : array;
            }

            if (value is string text)
            {
                var tokens = text
                    .Split(new[] { ' ', ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
                if (tokens.Length == 0)
                {
                    return null;
                }

                var parsed = new List<double>(tokens.Length);
                foreach (var token in tokens)
                {
                    if (double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out var number) && number > 0)
                    {
                        parsed.Add(number);
                    }
                }

                return parsed.Count == 0 ? null : parsed.ToArray();
            }

            return null;
        }
    }

    internal sealed record LayoutPalette(
        Thickness ContentPadding,
        double DefaultSpacing,
        ImmutableSolidColorBrush? SplitViewPaneBackground,
        CornerRadius ControlCornerRadius)
    {
        public static LayoutPalette Create(ResourceAccessor accessor, CornerRadius defaultCornerRadius)
        {
            var padding = accessor.GetThickness("ControlContentPadding") ?? new Thickness(12, 8, 12, 12);
            var spacing = accessor.GetDouble("ControlSpacing") ?? 4;
            var background = accessor.GetBrush("SplitViewPaneBackground") ?? accessor.GetBrush("SystemControlBackgroundBaseLowBrush");
            var cornerRadius = accessor.GetCornerRadius("ControlCornerRadius") ?? defaultCornerRadius;

            return new LayoutPalette(padding, Math.Max(0, spacing), background, cornerRadius);
        }

        public static LayoutPalette CreateFallback(CornerRadius defaultCornerRadius)
        {
            return new LayoutPalette(
                new Thickness(12, 8, 12, 12),
                4,
                new ImmutableSolidColorBrush(Color.FromRgb(0xF2, 0xF2, 0xF2)),
                defaultCornerRadius);
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
