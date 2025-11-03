# Control Catalog Widget Migration

This plan tracks the work required to bring widget-rendered equivalents into the ControlCatalog sample alongside the existing Avalonia control pages.

1. [x] Land the shared infrastructure (`SideBySideComparisonPage`, widget project reference) to host Avalonia and widget views side-by-side.
2. [x] Implement the first comparison by pairing `TextBlockPage` with the widget-based `TextBlockWidgetPage` inside `SideBySideComparisonPage`.
3. [ ] Composition – duplicate the existing page with widgets and register a comparison tab.
4. [ ] Accelerator – create widget page mirroring behaviors (keyboard navigation, command samples) and add the comparison host.
5. [ ] Acrylic – reproduce the acrylic brushes, layering, and interactivity using widgets.
6. [ ] AdornerLayer – port adorners scenarios to widgets and verify overlay correctness.
7. [ ] AutoCompleteBox – model the suggestion list, filtering, and styling with widget equivalents.
8. [ ] Border – replicate rounded borders, gradient brushes, and nesting using widgets.
9. [ ] Buttons – cover all button variants (standard, repeat, split) with widget implementations.
10. [ ] ButtonSpinner – translate spinner templates, stepping logic, and validation visuals to widgets.
11. [ ] Calendar – render month grid, selection states, and navigation chrome via widgets.
12. [ ] Canvas – migrate drawing samples to widget primitives preserving interactions.
13. [ ] Carousel – rebuild item host, transitions, and indicators with widget composition.
14. [ ] CheckBox – map three-state checkbox visuals, commands, and indeterminate support.
15. [ ] Clipboard – implement copy/paste demonstrations using widget surfaces and selection APIs.
16. [ ] ColorPicker – reproduce palette, sliders, eye-dropper, and preview widget counterparts.
17. [ ] ComboBox – port dropdown host, item templates, and virtualization to widgets.
18. [ ] Container Queries – ensure responsive layout demo works with widget measurement logic.
19. [ ] ContextFlyout – translate contextual flyout samples and gestures to widgets.
20. [ ] ContextMenu – rebuild nested menus, accelerators, and icons using widget menu primitives.
21. [ ] Cursor – showcase cursor variations with widget-hosted surfaces.
22. [ ] Custom Drawing – migrate drawing canvas and pen tooling to widget rendering pipeline.
23. [ ] DataGrid – compose widget-based grid sample, including sorting, grouping, and editing behaviors.
24. [ ] Data Validation – demonstrate validation states, adorners, and tooltips through widgets.
25. [ ] Date/Time Picker – port date/time picking workflows with widget input primitives.
26. [ ] CalendarDatePicker – replicate calendar drop-down and selection states in widgets.
27. [ ] Dialogs – implement modal overlays and button stacks with widget equivalents.
28. [ ] Drag+Drop – migrate drag targets, feedback visuals, and drop indicators to widgets.
29. [ ] Expander – rebuild expand/collapse headers, icons, and animations in widget form.
30. [ ] Flyouts – cover flyout placement, light-dismiss, and nested flyouts via widgets.
31. [ ] Focus – demonstrate focus scopes, navigation, and indicators using widgets.
32. [ ] Gestures – port pointer gesture samples (tap, hold, pinch) to widget handlers.
33. [ ] Image – render bitmap, vector, and svg samples with widget drawing ops.
34. [ ] Label – recreate labeling, access keys, and alignment using widget text components.
35. [ ] LayoutTransformControl – mirror layout transformations (rotate, scale) in widget tree.
36. [ ] ListBox – implement selection, virtualization, and styling with widget list controls.
37. [ ] Menu – migrate menu bar and submenu scenarios to widget menu primitives.
38. [ ] Notifications – render toast and inline notifications with widget surfaces.
39. [ ] NumericUpDown – rebuild numeric input, spin buttons, and validation cues in widgets.
40. [ ] OpenGL – integrate widget-hosted OpenGL sample ensuring overlay compatibility.
41. [ ] OpenGL Lease – port lease sample to widgets maintaining lifetime management UI.
42. [ ] Platform Information – present environment diagnostics using widget text/layout.
43. [ ] Pointers – reproduce pointer visualization overlays with widget drawing.
44. [ ] ProgressBar – showcase determinate/indeterminate progress with widget visuals.
45. [ ] RadioButton – implement option groups, selection logic, and disabled states via widgets.
46. [ ] RefreshContainer – translate pull-to-refresh animation and content updates to widgets.
47. [ ] RelativePanel – rebuild layout constraints and handles as widget layout sample.
48. [ ] ScrollViewer – use widget scroll presenter to mimic scrolling demos.
49. [ ] Slider – implement slider, tick marks, and tooltips within widget framework.
50. [ ] SplitView – recreate pane modes, adaptive behavior, and animations with widgets.
51. [ ] TabControl – port tab headers, content transitions, and add/remove interactions to widgets.
52. [ ] TabStrip – build widget version of tab strip sample including drag/reorder (if present).
53. [x] TextBox – implement text box behaviors (masking, watermark, validation) with widgets.
54. [ ] Theme Variants – ensure theme switching manifests correctly across widget pages.
55. [ ] ToggleSwitch – translate toggle visuals, animations, and bindings to widgets.
56. [ ] ToolTip – provide tooltip triggers and placement using widget overlay helpers.
57. [ ] TransitioningContentControl – migrate content transitions and animations to widget equivalents.
58. [ ] TreeView – rebuild hierarchical data presentation with widget tree view primitives.
59. [ ] Viewbox – demonstrate scaling behaviors using widget layout host.
60. [ ] Native Embed – host native content within widget surface maintaining interop.
61. [ ] Window Customizations – replicate chrome customization sample with widgets.
62. [ ] HeaderedContentControl – rebuild header/content patterns within widgets.
63. [ ] Screens – port multi-monitor diagnostic UI to widget layout.
