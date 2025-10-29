using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using FastTreeDataGrid.Engine.Infrastructure;

namespace FastTreeDataGrid.ControlsDemo.ViewModels;

public sealed class ListBoxPageViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly IReadOnlyList<ListEntryRow> _entries;
    private readonly FastTreeDataGridFlatSource<ListEntryRow> _source;
    private IReadOnlyList<int> _selectedIndices = Array.Empty<int>();
    private ListEntryRow? _selectedItem;

    public ListBoxPageViewModel()
    {
        _entries = CreateEntries();
        foreach (var entry in _entries)
        {
            entry.PropertyChanged += OnEntryPropertyChanged;
        }

        _source = new FastTreeDataGridFlatSource<ListEntryRow>(_entries, _ => Array.Empty<ListEntryRow>());
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public IFastTreeDataGridSource Source => _source;

    public IReadOnlyList<int> SelectedIndices
    {
        get => _selectedIndices;
        set
        {
            var normalized = value ?? Array.Empty<int>();
            if (_selectedIndices == normalized)
            {
                return;
            }

            _selectedIndices = normalized;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedIndices)));

            SelectedItem = normalized.Count > 0
                ? _source.GetRow(normalized[0]).Item as ListEntryRow
                : null;
        }
    }

    public ListEntryRow? SelectedItem
    {
        get => _selectedItem;
        private set
        {
            if (!Equals(_selectedItem, value))
            {
                _selectedItem = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedItem)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectionSummary)));
            }
        }
    }

    public string SelectionSummary => SelectedItem is null
        ? "Select a reminder to see its details."
        : $"{SelectedItem.Title} • {SelectedItem.DueTime.ToString("t", CultureInfo.CurrentCulture)}";

    public void Dispose()
    {
        foreach (var entry in _entries)
        {
            entry.PropertyChanged -= OnEntryPropertyChanged;
        }
    }

    private static IReadOnlyList<ListEntryRow> CreateEntries()
    {
        return new[]
        {
            new ListEntryRow("Standup meeting", "Daily sync with the product team.", TimeSpan.FromMinutes(30), true),
            new ListEntryRow("Design review", "UI polish review with design and QA.", TimeSpan.FromHours(2), false),
            new ListEntryRow("Customer interview", "Listen to beta user feedback on the reporting workflow.", TimeSpan.FromHours(1.5), true),
            new ListEntryRow("Code cleanup", "Refactor the import parser and add unit tests.", TimeSpan.FromHours(3), false),
            new ListEntryRow("Roadmap planning", "Gather estimates for Q3 features.", TimeSpan.FromHours(1), false),
            new ListEntryRow("Async catch up", "Reply to open threads and unblock the integrations team.", TimeSpan.FromMinutes(45), false),
        };
    }

    private void OnEntryPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (ReferenceEquals(sender, SelectedItem))
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectionSummary)));
        }
    }
}

public sealed class ListEntryRow : IFastTreeDataGridValueProvider, INotifyPropertyChanged
{
    public const string KeyTitle = nameof(Title);
    public const string KeyDescription = nameof(Description);
    public const string KeyDuration = nameof(Duration);
    public const string KeyDueTime = nameof(DueTime);
    public const string KeyIsPinned = nameof(IsPinned);

    private string _title;
    private string _description;
    private TimeSpan _duration;
    private DateTime _dueTime;
    private bool _isPinned;

    public ListEntryRow(string title, string description, TimeSpan duration, bool isPinned)
    {
        _title = title;
        _description = description;
        _duration = duration;
        _isPinned = isPinned;
        _dueTime = DateTime.Today.AddHours(9).Add(duration);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public event EventHandler<ValueInvalidatedEventArgs>? ValueInvalidated;

    public string Title
    {
        get => _title;
        set => SetField(ref _title, value, nameof(Title));
    }

    public string Description
    {
        get => _description;
        set => SetField(ref _description, value, nameof(Description));
    }

    public TimeSpan Duration
    {
        get => _duration;
        set
        {
            if (SetField(ref _duration, value, nameof(Duration)))
            {
                DueTime = DateTime.Today.AddHours(9).Add(_duration);
            }
        }
    }

    public DateTime DueTime
    {
        get => _dueTime;
        private set => SetField(ref _dueTime, value, nameof(DueTime));
    }

    public bool IsPinned
    {
        get => _isPinned;
        set => SetField(ref _isPinned, value, nameof(IsPinned));
    }

    public object? GetValue(object? item, string key)
    {
        return key switch
        {
            KeyTitle => Title,
            KeyDescription => Description,
            KeyDuration => Duration,
            KeyDueTime => DueTime,
            KeyIsPinned => IsPinned,
            _ => null,
        };
    }

    private bool SetField<T>(ref T field, T value, string propertyName)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        ValueInvalidated?.Invoke(this, new ValueInvalidatedEventArgs(this, propertyName));

        return true;
    }
}
