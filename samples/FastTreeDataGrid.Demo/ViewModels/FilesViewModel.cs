using System;
using System.Collections.Generic;
using System.ComponentModel;
using FastTreeDataGrid.Control.Infrastructure;
using FastTreeDataGrid.Control.Models;
using FastTreeDataGrid.Demo.ViewModels.FileSystem;

namespace FastTreeDataGrid.Demo.ViewModels;

public sealed class FilesViewModel : INotifyPropertyChanged, IDisposable
{
    private const StringComparison ComparisonMode = StringComparison.CurrentCultureIgnoreCase;

    private readonly FileSystemTreeSource _source;
    private string _searchText = string.Empty;

    public FilesViewModel(FileSystemTreeSource source)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public IFastTreeDataGridSource Source => _source;

    public string SearchText
    {
        get => _searchText;
        set
        {
            var normalized = value?.Trim() ?? string.Empty;
            if (SetProperty(ref _searchText, normalized, nameof(SearchText)))
            {
                ApplyFilter();
            }
        }
    }

    public bool ApplySort(FastTreeDataGridColumn column, FastTreeDataGridSortDirection direction)
    {
        if (column is null)
        {
            return false;
        }

        if (direction == FastTreeDataGridSortDirection.None)
        {
            _source.ResetSort();
            return true;
        }

        if (string.IsNullOrEmpty(column.ValueKey))
        {
            _source.ResetSort();
            return false;
        }

        var comparison = GetComparison(column.ValueKey);
        if (comparison is null)
        {
            _source.ResetSort();
            return false;
        }

        if (direction == FastTreeDataGridSortDirection.Descending)
        {
            var baseComparison = comparison;
            comparison = (left, right) => baseComparison(right, left);
        }

        _source.Sort(comparison);
        return true;
    }

    public void Dispose()
    {
        _source.Dispose();
    }

    private void ApplyFilter()
    {
        if (string.IsNullOrWhiteSpace(_searchText))
        {
            _source.ApplyFilter(null);
            return;
        }

        var query = _searchText;
        _source.ApplyFilter(node =>
        {
            if (node.IsPlaceholder)
            {
                return true;
            }

            return (node.Name?.Contains(query, ComparisonMode) ?? false)
                   || (node.FullPath?.Contains(query, ComparisonMode) ?? false);
        });
    }

    private Comparison<FastTreeDataGridRow>? GetComparison(string valueKey)
    {
        return valueKey switch
        {
            FileSystemNode.KeyName => CreateStringComparison(node => node.Name),
            FileSystemNode.KeyType => CreateStringComparison(node => node.DisplayType),
            FileSystemNode.KeySize => CreateNumericComparison(node => node.SizeBytes),
            FileSystemNode.KeyModified => CreateDateComparison(node => node.Modified),
            _ => null,
        };
    }

    private static Comparison<FastTreeDataGridRow> CreateStringComparison(Func<FileSystemNode, string?> selector)
    {
        var comparer = StringComparer.CurrentCultureIgnoreCase;
        return (leftRow, rightRow) =>
        {
            if (leftRow.Item is not FileSystemNode left || rightRow.Item is not FileSystemNode right)
            {
                return 0;
            }

            var directoryOrder = CompareDirectory(left, right);
            if (directoryOrder != 0)
            {
                return directoryOrder;
            }

            return comparer.Compare(selector(left) ?? string.Empty, selector(right) ?? string.Empty);
        };
    }

    private static Comparison<FastTreeDataGridRow> CreateNumericComparison(Func<FileSystemNode, long> selector)
    {
        var comparer = StringComparer.CurrentCultureIgnoreCase;
        return (leftRow, rightRow) =>
        {
            if (leftRow.Item is not FileSystemNode left || rightRow.Item is not FileSystemNode right)
            {
                return 0;
            }

            var directoryOrder = CompareDirectory(left, right);
            if (directoryOrder != 0)
            {
                return directoryOrder;
            }

            var comparison = selector(left).CompareTo(selector(right));
            if (comparison != 0)
            {
                return comparison;
            }

            return comparer.Compare(left.Name, right.Name);
        };
    }

    private static Comparison<FastTreeDataGridRow> CreateDateComparison(Func<FileSystemNode, DateTimeOffset> selector)
    {
        var comparer = StringComparer.CurrentCultureIgnoreCase;
        return (leftRow, rightRow) =>
        {
            if (leftRow.Item is not FileSystemNode left || rightRow.Item is not FileSystemNode right)
            {
                return 0;
            }

            var directoryOrder = CompareDirectory(left, right);
            if (directoryOrder != 0)
            {
                return directoryOrder;
            }

            var comparison = selector(left).CompareTo(selector(right));
            if (comparison != 0)
            {
                return comparison;
            }

            return comparer.Compare(left.Name, right.Name);
        };
    }

    private static int CompareDirectory(FileSystemNode left, FileSystemNode right)
    {
        if (left.IsPlaceholder && !right.IsPlaceholder)
        {
            return -1;
        }

        if (!left.IsPlaceholder && right.IsPlaceholder)
        {
            return 1;
        }

        if (left.IsDirectory && !right.IsDirectory)
        {
            return -1;
        }

        if (!left.IsDirectory && right.IsDirectory)
        {
            return 1;
        }

        return 0;
    }

    private bool SetProperty<T>(ref T storage, T value, string propertyName)
    {
        if (EqualityComparer<T>.Default.Equals(storage, value))
        {
            return false;
        }

        storage = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }
}
