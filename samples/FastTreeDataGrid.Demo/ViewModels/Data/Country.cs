namespace FastTreeDataGrid.Demo.ViewModels.Data;

internal class Country
{
    public string? Name { get; set; }
    public string Region { get; private set; }
    public int Population { get; private set; }
    public int Area { get; private set; }
    public double PopulationDensity { get; private set; }
    public double CoastLine { get; private set; }
    public double? NetMigration { get; private set; }
    public double? InfantMortality { get; private set; }
    public int GDP { get; private set; }
    public double? LiteracyPercent { get; private set; }
    public double? Phones { get; private set; }
    public double? BirthRate { get; private set; }
    public double? DeathRate { get; private set; }

    public Country(
        string name,
        string region,
        int population,
        int area,
        double density,
        double coast,
        double? migration,
        double? infantMorality,
        int gdp,
        double? literacy,
        double? phones,
        double? birth,
        double? death)
    {
        Name = name;
        Region = region;
        Population = population;
        Area = area;
        PopulationDensity = density;
        CoastLine = coast;
        NetMigration = migration;
        InfantMortality = infantMorality;
        GDP = gdp;
        LiteracyPercent = literacy;
        BirthRate = birth;
        DeathRate = death;
    }
}
