namespace RengaMcp.Models;

public static class CreationCatalog
{
    public static IReadOnlyList<CreationType> Types { get; } =
    [
        new("Level", "{C3CE17FF-6F28-411F-B18D-74FE957B2BA8}", false, "Building model"),
        new("Wall", "{4329112A-6B65-48D9-9DA8-ABF1F8F36327}", false, "Optional level local ID"),
        new("Beam", "{63478188-7C88-4A6D-B891-9725F04A5BC7}", false, "Optional level local ID"),
        new("Column", "{D9EE2442-E807-42FB-8FE5-9DCFE543035D}", false, "Optional level local ID"),
        new("Floor", "{F5BD8BD8-39C1-47F8-8499-F673C580DFBE}", false, "Optional level local ID"),
        new("Plate", "{62CF086E-5A39-4484-840C-FFA6A1C6E2B7}", false, "Optional level local ID"),
        new("Roof", "{BAC4470F-D560-4F57-A49E-FAA5F6E5A279}", false, "Optional level local ID"),
        new("Room", "{F1A805FF-573D-F46B-FFBA-57F4BCCAA6ED}", false, "Optional level local ID"),
        new("Element", "{E1E3BD66-2E13-4FA4-A9EB-677E03067C2F}", false, "Optional level local ID; select an element style"),
        new("Equipment", "{5D2F3734-5A49-4504-90B1-0676F0F25DA7}", false, "Optional level local ID; style is required"),
        new("PlumbingFixture", "{B8C7155A-B462-4FF5-BC41-C9C17A9F48FA}", false, "Optional level local ID; style is required"),
        new("Door", "{1CFBA99C-01E7-4078-AE1A-3E2FF0673599}", true, "Wall local ID"),
        new("Window", "{2B02B353-2CA5-4566-88BB-917EA8460174}", true, "Wall local ID"),
        new("Opening", "{FC443D5A-B76C-45E5-B91C-520EF0896109}", true, "Allowed host local ID")
    ];

    public static IReadOnlyDictionary<string, string> StyleCollections { get; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["beam"] = "BeamStyles",
            ["column"] = "ColumnStyles",
            ["door"] = "DoorStyles",
            ["window"] = "WindowStyles",
            ["plate"] = "PlateStyles",
            ["element"] = "ElementStyles",
            ["equipment"] = "EquipmentStyles",
            ["mechanical_equipment"] = "MechanicalEquipmentStyles",
            ["plumbing_fixture"] = "PlumbingFixtureStyles",
            ["lighting_fixture"] = "LightingFixtureStyles",
            ["wiring_accessory"] = "WiringAccessoryStyles"
        };
}
