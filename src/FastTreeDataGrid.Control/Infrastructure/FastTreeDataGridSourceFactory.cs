using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls.Templates;
using Avalonia.Data;

namespace FastTreeDataGrid.Control.Infrastructure;

/// <summary>
/// Helper methods for constructing <see cref="IFastTreeDataGridSource"/> instances
/// from common hierarchical data shapes.
/// </summary>
public static class FastTreeDataGridSourceFactory
{
    /// <summary>
    /// Creates a tree data grid source from an enumerable of root items using the provided child selector.
    /// </summary>
    /// <typeparam name="T">Item type.</typeparam>
    /// <param name="rootItems">Root items.</param>
    /// <param name="childrenSelector">Selector that yields children for a given item.</param>
    /// <param name="keySelector">Optional selector that produces a stable key for an item.</param>
    /// <param name="keyComparer">Optional key comparer.</param>
    public static IFastTreeDataGridSource FromEnumerable<T>(
        IEnumerable<T> rootItems,
        Func<T, IEnumerable<T>> childrenSelector,
        Func<T, string>? keySelector = null,
        IEqualityComparer<string>? keyComparer = null)
    {
        if (rootItems is null)
        {
            throw new ArgumentNullException(nameof(rootItems));
        }

        if (childrenSelector is null)
        {
            throw new ArgumentNullException(nameof(childrenSelector));
        }

        return new FastTreeDataGridFlatSource<T>(
            rootItems,
            childrenSelector,
            keySelector,
            keyComparer);
    }

    /// <summary>
    /// Creates a tree data grid source from an enumerable of root items using an untyped child selector.
    /// </summary>
    /// <param name="rootItems">Root items.</param>
    /// <param name="childrenSelector">Selector that yields children for a given item.</param>
    /// <param name="keySelector">Optional selector that produces a stable key for an item.</param>
    /// <param name="keyComparer">Optional key comparer.</param>
    public static IFastTreeDataGridSource FromEnumerable(
        IEnumerable rootItems,
        Func<object?, IEnumerable?> childrenSelector,
        Func<object?, string?>? keySelector = null,
        IEqualityComparer<string>? keyComparer = null)
    {
        if (rootItems is null)
        {
            throw new ArgumentNullException(nameof(rootItems));
        }

        if (childrenSelector is null)
        {
            throw new ArgumentNullException(nameof(childrenSelector));
        }

        var normalizedKeySelector = keySelector is null
            ? null
            : new Func<object?, string>(value => keySelector(value) ?? string.Empty);

        return new FastTreeDataGridFlatSource<object?>(
            MaterializeRoot(rootItems),
            item => Materialize(childrenSelector(item)),
            normalizedKeySelector,
            keyComparer);
    }

    /// <summary>
    /// Creates a tree data grid source from an enumerable and an <see cref="ITreeDataTemplate"/>.
    /// </summary>
    /// <param name="rootItems">Root items.</param>
    /// <param name="template">Template used to resolve the children of each item.</param>
    /// <param name="fallbackChildrenSelector">Optional fallback child selector when the template does not yield results.</param>
    /// <param name="keySelector">Optional selector that produces a stable key for an item.</param>
    /// <param name="keyComparer">Optional key comparer.</param>
    public static IFastTreeDataGridSource FromTreeDataTemplate(
        IEnumerable rootItems,
        ITreeDataTemplate template,
        Func<object?, IEnumerable?>? fallbackChildrenSelector = null,
        Func<object?, string?>? keySelector = null,
        IEqualityComparer<string>? keyComparer = null)
    {
        if (rootItems is null)
        {
            throw new ArgumentNullException(nameof(rootItems));
        }

        if (template is null)
        {
            throw new ArgumentNullException(nameof(template));
        }

        var normalizedKeySelector = keySelector is null
            ? null
            : new Func<object?, string>(value => keySelector(value) ?? string.Empty);

        return new FastTreeDataGridFlatSource<object?>(
            MaterializeRoot(rootItems),
            item => ResolveChildrenFromTemplate(template, item, fallbackChildrenSelector),
            normalizedKeySelector,
            keyComparer);
    }

    private static IReadOnlyList<object?> MaterializeRoot(IEnumerable items)
    {
        var list = new List<object?>();
        foreach (var item in items)
        {
            list.Add(item);
        }

        return list;
    }

    private static IEnumerable<object?> Materialize(IEnumerable? items)
    {
        if (items is null)
        {
            yield break;
        }

        foreach (var item in items)
        {
            yield return item;
        }
    }

    private static IEnumerable<object?> Materialize(object? value)
    {
        if (value is null)
        {
            yield break;
        }

        if (value is string text)
        {
            yield return text;
            yield break;
        }

        if (value is IEnumerable enumerable)
        {
            foreach (var child in enumerable)
            {
                yield return child;
            }

            yield break;
        }

        yield return value;
    }

    private static IEnumerable<object?> ResolveChildrenFromTemplate(
        ITreeDataTemplate template,
        object? item,
        Func<object?, IEnumerable?>? fallback)
    {
        if (item is null)
        {
            return Array.Empty<object?>();
        }

        IReadOnlyList<object?>? materialized = null;

        try
        {
            var binding = template.ItemsSelector(item);
            if (binding is not null)
            {
                materialized = EvaluateBinding(binding);
            }
        }
        catch
        {
            materialized = null;
        }

        if (materialized is null && fallback is not null)
        {
            materialized = MaterializeToList(fallback(item));
        }

        return materialized ?? Array.Empty<object?>();
    }

    private static IReadOnlyList<object?>? EvaluateBinding(InstancedBinding binding)
    {
        if (binding.Source is IObservable<object?> observable)
        {
            var observer = new SingleValueObserver();
            using (observable.Subscribe(observer))
            {
            }

            if (observer.HasValue)
            {
                return MaterializeToList(observer.Value);
            }
        }

        return null;
    }

    private static IReadOnlyList<object?> MaterializeToList(object? value)
    {
        if (value is null)
        {
            return Array.Empty<object?>();
        }

        if (value is string text)
        {
            return new object?[] { text };
        }

        if (value is IEnumerable enumerable)
        {
            var list = new List<object?>();
            foreach (var item in enumerable)
            {
                list.Add(item);
            }

            return list;
        }

        return new object?[] { value };
    }

    private sealed class SingleValueObserver : IObserver<object?>
    {
        public object? Value { get; private set; }

        public bool HasValue { get; private set; }

        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
        }

        public void OnNext(object? value)
        {
            Value = value;
            HasValue = true;
        }
    }
}
