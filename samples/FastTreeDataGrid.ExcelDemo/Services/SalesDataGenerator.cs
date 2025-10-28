using System;
using System.Collections.Generic;
using FastTreeDataGrid.ExcelDemo.Models;

namespace FastTreeDataGrid.ExcelDemo.Services;

public static class SalesDataGenerator
{
    private static readonly string[] Regions =
    {
        "Americas",
        "EMEA",
        "APAC",
        "LATAM",
        "DACH",
        "Nordics",
        "Benelux",
        "Greater China",
    };

    private static readonly Dictionary<string, string[]> CountriesByRegion = new()
    {
        ["Americas"] = new[] { "United States", "Canada", "Mexico", "Brazil", "Argentina" },
        ["EMEA"] = new[] { "United Kingdom", "France", "Spain", "Italy", "Poland" },
        ["APAC"] = new[] { "Japan", "South Korea", "Australia", "Singapore", "India" },
        ["LATAM"] = new[] { "Chile", "Peru", "Colombia", "Costa Rica", "Panama" },
        ["DACH"] = new[] { "Germany", "Austria", "Switzerland" },
        ["Nordics"] = new[] { "Sweden", "Norway", "Finland", "Denmark" },
        ["Benelux"] = new[] { "Belgium", "Netherlands", "Luxembourg" },
        ["Greater China"] = new[] { "China", "Hong Kong", "Taiwan" },
    };

    private static readonly string[] Segments =
    {
        "Enterprise",
        "Mid-Market",
        "SMB",
        "Public Sector",
        "Startups",
    };

    private static readonly string[] ProductFamilies =
    {
        "Balance",
        "Catalyst",
        "Nimbus",
        "Vertex",
        "Aurora",
        "Pulse",
        "Helix",
        "Lumen",
        "Vector",
        "Harbor",
        "Orion",
        "Nova",
    };

    private static readonly string[] ProductQualifiers =
    {
        "Suite",
        "Analytics",
        "Core",
        "Cloud",
        "Edge",
        "AI",
        "Insights",
        "Ops",
        "Signal",
        "Flow",
        "Forge",
        "Pilot",
    };

    private static readonly string[] ProductSeries =
    {
        "100",
        "200",
        "300",
        "400",
        "500",
        "600",
        "700",
        "800",
    };

    private static readonly string[] ProductTiers =
    {
        "Standard",
        "Plus",
        "Prime",
        "Elite",
        "Pro",
        "Max",
    };

    private static readonly string[] Salespeople =
    {
        "Alex Johnson",
        "Priya Patel",
        "Lars Eriksen",
        "Sofia Garcia",
        "Wei Zhang",
        "Emily Clark",
        "Daniel Novak",
        "Sara Rossi",
        "Carlos Hernandez",
        "Hannah Lee",
        "Mateusz Kowalski",
        "Isabella Silva",
        "Noah Brown",
        "Yuki Tanaka",
        "Oliver Smith",
    };

    public static IReadOnlyList<SalesRecord> Create(int count)
    {
        if (count <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }

        var random = new Random(17);
        var records = new List<SalesRecord>(count);
        var start = new DateTime(2019, 1, 1);
        var daysRange = (new DateTime(2024, 12, 31) - start).Days;

        for (var i = 0; i < count; i++)
        {
            var transactionId = i + 1;
            var region = Regions[random.Next(Regions.Length)];
            var countries = CountriesByRegion[region];
            var country = countries[random.Next(countries.Length)];
            var segment = Segments[random.Next(Segments.Length)];
            var family = ProductFamilies[random.Next(ProductFamilies.Length)];
            var qualifier = ProductQualifiers[random.Next(ProductQualifiers.Length)];
            var series = ProductSeries[random.Next(ProductSeries.Length)];
            var tier = ProductTiers[random.Next(ProductTiers.Length)];
            var product = $"{family} {qualifier} {series}-{tier}";
            var salesperson = Salespeople[random.Next(Salespeople.Length)];

            var date = start.AddDays(random.Next(daysRange));

            var baseDemand = random.NextDouble() * 0.6 + 0.7; // 0.7 .. 1.3
            var seasonal = 1 + 0.2 * Math.Sin((date.Month / 12d) * Math.PI * 2);
            var regionWeight = 0.8 + (Array.IndexOf(Regions, region) * 0.03);
            var units = (int)Math.Max(1, Math.Round(20 + (baseDemand * seasonal * regionWeight * 50 * random.NextDouble())));

            var unitPrice = 95 + random.NextDouble() * 155; // 95 .. 250
            var discountFactor = 0.85 + random.NextDouble() * 0.12; // 0.85 .. 0.97
            var sales = units * unitPrice * discountFactor;
            var costBasis = unitPrice * (0.45 + random.NextDouble() * 0.3);
            var cost = units * costBasis;

            records.Add(new SalesRecord(
                transactionId,
                region,
                country,
                segment,
                product,
                salesperson,
                date,
                Math.Round(sales, 2),
                Math.Round(cost, 2),
                units));
        }

        return records;
    }
}
