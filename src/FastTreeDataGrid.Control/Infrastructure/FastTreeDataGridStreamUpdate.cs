using System;
using System.Collections.Generic;

namespace FastTreeDataGrid.Control.Infrastructure;

public sealed class FastTreeDataGridStreamUpdate<T>
{
    public FastTreeDataGridStreamUpdate(Action<IList<T>> apply)
    {
        Apply = apply ?? throw new ArgumentNullException(nameof(apply));
    }

    public Action<IList<T>> Apply { get; }

    public static FastTreeDataGridStreamUpdate<T> Reset(IEnumerable<T> items) =>
        new(list =>
        {
            list.Clear();
            if (items is null)
            {
                return;
            }

            foreach (var item in items)
            {
                list.Add(item);
            }
        });

    public static FastTreeDataGridStreamUpdate<T> Add(T item) =>
        new(list => list.Add(item));

    public static FastTreeDataGridStreamUpdate<T> AddRange(IEnumerable<T> items) =>
        new(list =>
        {
            if (items is null)
            {
                return;
            }

            foreach (var item in items)
            {
                list.Add(item);
            }
        });

    public static FastTreeDataGridStreamUpdate<T> Remove(Predicate<T> match) =>
        new(list =>
        {
            if (match is null)
            {
                return;
            }

            for (var index = list.Count - 1; index >= 0; index--)
            {
                if (match(list[index]))
                {
                    list.RemoveAt(index);
                }
            }
        });

    public static FastTreeDataGridStreamUpdate<T> Replace(IEnumerable<T> items) =>
        Reset(items);
}
