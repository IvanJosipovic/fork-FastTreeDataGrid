using System;
using System.Globalization;
using FastTreeDataGrid.Control.Infrastructure;

namespace FastTreeDataGrid.Demo.ViewModels.FileSystem;

internal sealed class FileSystemNode : IFastTreeDataGridValueProvider, IFastTreeDataGridGroup
{
    public const string KeyName = "File.Name";
    public const string KeyType = "File.Type";
    public const string KeySize = "File.Size";
    public const string KeyModified = "File.Modified";

    private readonly string _sizeText;
    private readonly string _modifiedText;

    private FileSystemNode(
        string name,
        string fullPath,
        bool isDirectory,
        bool isPlaceholder,
        bool isLoading,
        long size,
        DateTimeOffset modified,
        string displayType)
    {
        Name = name;
        FullPath = fullPath;
        IsDirectory = isDirectory;
        IsPlaceholder = isPlaceholder;
        IsLoading = isLoading;
        DisplayType = displayType;
        SizeBytes = size;
        Modified = modified;
        _sizeText = isDirectory || isPlaceholder ? string.Empty : FormatSize(size);
        _modifiedText = isPlaceholder ? string.Empty : modified.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
    }

    public string Name { get; }

    public string FullPath { get; }

    public bool IsDirectory { get; }

    public bool IsPlaceholder { get; }

    public bool IsLoading { get; }

    public long SizeBytes { get; }

    public DateTimeOffset Modified { get; }

    public string DisplayType { get; }

    public bool IsGroup => IsDirectory;

    public static FileSystemNode CreateDirectory(string name, string fullPath, DateTimeOffset modified) =>
        new(name, fullPath, isDirectory: true, isPlaceholder: false, isLoading: false, size: 0, modified, "Folder");

    public static FileSystemNode CreateFile(string name, string fullPath, long size, DateTimeOffset modified, string? extension) =>
        new(name, fullPath, isDirectory: false, isPlaceholder: false, isLoading: false, size, modified, extension is { Length: > 0 } ? extension : "File");

    public static FileSystemNode CreateLoading(string name) =>
        new(name, name, isDirectory: false, isPlaceholder: true, isLoading: true, size: 0, DateTimeOffset.Now, string.Empty);

    public object? GetValue(object? item, string key) => key switch
    {
        KeyName => IsPlaceholder ? "Loading..." : Name,
        KeyType => DisplayType,
        KeySize => _sizeText,
        KeyModified => _modifiedText,
        _ => string.Empty,
    };

    public event EventHandler<ValueInvalidatedEventArgs>? ValueInvalidated
    {
        add { }
        remove { }
    }

    private static string FormatSize(long size)
    {
        string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
        double value = size;
        var order = 0;
        while (value >= 1024 && order < suffixes.Length - 1)
        {
            value /= 1024;
            order++;
        }

        return order == 0 ? $"{size:N0} {suffixes[order]}" : $"{value:0.##} {suffixes[order]}";
    }
}
