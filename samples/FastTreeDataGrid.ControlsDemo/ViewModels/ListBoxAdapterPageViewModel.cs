using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace FastTreeDataGrid.ControlsDemo.ViewModels;

public sealed class ListBoxAdapterPageViewModel
{
    public ListBoxAdapterPageViewModel()
    {
        Reminders = new ObservableCollection<ReminderItem>(CreateReminders());
    }

    public ObservableCollection<ReminderItem> Reminders { get; }

    private static IReadOnlyList<ReminderItem> CreateReminders() => new[]
    {
        new ReminderItem("Sprint review", "Share highlights with stakeholders and capture action items.", TimeSpan.FromHours(1), true),
        new ReminderItem("Support rotation", "Hand off pager duties and review uptime report.", TimeSpan.FromMinutes(45), false),
        new ReminderItem("UX validation", "Observe three users navigating the analytics dashboard.", TimeSpan.FromHours(2), true),
        new ReminderItem("OKR sync", "Review key results and surface blockers for leadership.", TimeSpan.FromHours(1.5), false),
        new ReminderItem("Tech talk prep", "Polish deck and run through the demo scripts.", TimeSpan.FromHours(2.5), true),
        new ReminderItem("Incident retro", "Document follow-ups and update postmortem template.", TimeSpan.FromHours(1), false),
    };
}

public sealed class ReminderItem : INotifyPropertyChanged
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

    public ReminderItem(string title, string description, TimeSpan duration, bool isPinned)
    {
        _title = title;
        _description = description;
        _duration = duration;
        _isPinned = isPinned;
        _dueTime = DateTime.Today.AddHours(9).Add(duration);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

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

    private bool SetField<T>(ref T field, T value, string propertyName)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }
}
