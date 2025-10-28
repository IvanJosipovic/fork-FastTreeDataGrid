using System.Collections.Generic;
using System;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using FastTreeDataGrid.Engine.Infrastructure;
using FastTreeDataGrid.VirtualizationDemo.ViewModels.Data;
using FastTreeDataGrid.Control.Infrastructure;

namespace FastTreeDataGrid.VirtualizationDemo.ViewModels;

public sealed class AdaptersSamplesViewModel
{
    public AdaptersSamplesViewModel()
    {
        Catalog = BuildCatalog();

        EnumerableSource = FastTreeDataGridSourceFactory.FromEnumerable(
            Catalog,
            node => node.Children,
            node => node.Key);

        TemplateSource = FastTreeDataGridSourceFactory.FromTreeDataTemplate(
            Catalog,
            new FuncTreeDataTemplate<CatalogNode>(
                (_, _) => new ContentControl(),
                node => node.Children),
            item => (item as CatalogNode)?.Children ?? Array.Empty<CatalogNode>(),
            item => (item as CatalogNode)?.Key ?? string.Empty);
    }

    public IReadOnlyList<CatalogNode> Catalog { get; }

    public IFastTreeDataGridSource EnumerableSource { get; }

    public IFastTreeDataGridSource TemplateSource { get; }

    private static IReadOnlyList<CatalogNode> BuildCatalog()
    {
        return new[]
        {
            CatalogNode.CreateGroup(
                "electronics",
                "Electronics",
                new[]
                {
                    CatalogNode.CreateGroup(
                        "audio",
                        "Audio",
                        new[]
                        {
                            CatalogNode.CreateItem("headphones", "Noise-Cancelling Headphones", 299.95m),
                            CatalogNode.CreateItem("soundbar", "Dolby Atmos Soundbar", 799.00m),
                            CatalogNode.CreateItem("turntable", "Classic Turntable", 219.50m),
                        }),
                    CatalogNode.CreateGroup(
                        "computing",
                        "Computing",
                        new[]
                        {
                            CatalogNode.CreateItem("ultrabook", "14\" Ultrabook", 1249m),
                            CatalogNode.CreateItem("workstation", "Compact Desktop Workstation", 1899m),
                            CatalogNode.CreateItem("monitor", "34\" Ultrawide Monitor", 699.99m),
                        }),
                }),
            CatalogNode.CreateGroup(
                "outdoors",
                "Outdoors",
                new[]
                {
                    CatalogNode.CreateGroup(
                        "camping",
                        "Camping",
                        new[]
                        {
                            CatalogNode.CreateItem("tent", "4-Person Backpacking Tent", 349.95m),
                            CatalogNode.CreateItem("stove", "Titanium Cooking Stove", 129.00m),
                        }),
                    CatalogNode.CreateGroup(
                        "cycling",
                        "Cycling",
                        new[]
                        {
                            CatalogNode.CreateItem("city-bike", "Urban Commuter Bike", 899.00m),
                            CatalogNode.CreateItem("helmet", "Lightweight Helmet", 139.50m),
                            CatalogNode.CreateItem("lights", "Rechargeable Light Set", 89.90m),
                        }),
                }),
        };
    }
}
