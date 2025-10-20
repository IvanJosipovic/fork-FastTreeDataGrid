using System;
using System.Collections.Generic;
using System.Linq;
using FastTreeDataGrid.Demo.ViewModels.Data;

namespace FastTreeDataGrid.Demo.ViewModels;

public static class DemoDataFactory
{
    internal static IReadOnlyList<CountryNode> CreateCountries()
    {
        var groups = Countries.All
            .GroupBy(country => country.Region)
            .OrderBy(g => g.Key, StringComparer.CurrentCultureIgnoreCase);

        var result = new List<CountryNode>();

        foreach (var group in groups)
        {
            var children = group
                .OrderBy(country => country.Name, StringComparer.CurrentCultureIgnoreCase)
                .Select(CountryNode.CreateLeaf)
                .ToList();

            result.Add(CountryNode.CreateGroup(group.Key, children));
        }

        return result;
    }
}
