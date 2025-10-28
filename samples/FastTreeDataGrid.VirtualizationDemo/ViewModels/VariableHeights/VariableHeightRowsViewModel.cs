using System;
using System.Collections.Generic;
using System.Linq;
using FastTreeDataGrid.Engine.Infrastructure;

namespace FastTreeDataGrid.VirtualizationDemo.ViewModels.VariableHeights;

public sealed class VariableHeightRowsViewModel
{
    private readonly FastTreeDataGridFlatSource<VariableHeightSampleRow> _source;

    public VariableHeightRowsViewModel(int groupCount = 8, int itemsPerGroup = 600)
    {
        var data = BuildData(groupCount, itemsPerGroup);
        _source = new FastTreeDataGridFlatSource<VariableHeightSampleRow>(data, node => node.Children);
        ExpandAllGroups(_source);
    }

    public IFastTreeDataGridSource Source => _source;

    private static IReadOnlyList<VariableHeightSampleRow> BuildData(int groupCount, int itemsPerGroup)
    {
        var random = new Random(1337);
        var groups = new List<VariableHeightSampleRow>(groupCount);

        for (var groupIndex = 0; groupIndex < groupCount; groupIndex++)
        {
            var children = new List<VariableHeightSampleRow>(itemsPerGroup);
            for (var itemIndex = 0; itemIndex < itemsPerGroup; itemIndex++)
            {
                var variation = random.NextDouble();
                var height = 24d + variation * 160d;
                var category = $"Section {groupIndex + 1:00}";
                var title = $"Row {groupIndex + 1:00}-{itemIndex + 1:0000}";
                var details = CreateDetails(itemIndex, variation, height);
                children.Add(new VariableHeightSampleRow(title, category, height, isGroup: false, details));
            }

            var average = children.Average(x => x.Height);
            var groupHeight = 36d + (groupIndex % 4) * 6d;
            var groupDetails = $"{children.Count:#,0} rows · avg {average:0.0}px";
            groups.Add(new VariableHeightSampleRow(
                $"Section {groupIndex + 1:00}",
                $"Section {groupIndex + 1:00}",
                groupHeight,
                isGroup: true,
                groupDetails,
                children));
        }

        return groups;
    }

    private static string CreateDetails(int index, double variation, double height)
    {
        var profile = variation switch
        {
            < 0.1 => "compact",
            < 0.25 => "condensed",
            < 0.5 => "standard",
            < 0.7 => "roomy",
            < 0.85 => "spacious",
            _ => "expanded"
        };

        return $"Row #{index + 1} • profile: {profile} • height: {height:0.0}px";
    }

    private static void ExpandAllGroups(FastTreeDataGridFlatSource<VariableHeightSampleRow> source)
    {
        var index = 0;
        while (index < source.RowCount)
        {
            var row = source.GetRow(index);
            if (row.HasChildren && !row.IsExpanded)
            {
                source.ToggleExpansion(index);
            }

            index++;
        }
    }
}
