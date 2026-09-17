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
            ["wiring_accessory"] = "WiringAccessoryStyles",
            ["pipe"] = "PipeStyles",
            ["pipe_fitting"] = "PipeFittingStyles",
            ["duct"] = "DuctStyles",
            ["duct_fitting"] = "DuctFittingStyles",
            ["electrical_circuit_line"] = "ElectricalCircuitLineStyles",
            ["layered_material"] = "LayeredMaterials",
            ["system"] = "SystemStyles"
        };

    public static IReadOnlyList<SystemCategoryInfo> SystemCategories { get; } =
    [
        new("domestic_cold_water", 2, "pipe"),
        new("domestic_hot_water", 3, "pipe"),
        new("domestic_sewerage", 4, "pipe"),
        new("domestic_gas_supply", 5, "pipe"),
        new("water_fire_extinguishing", 6, "pipe"),
        new("water_heating", 7, "pipe"),
        new("gas_fire_extinguishing", 8, "pipe"),
        new("storm_drain", 9, "pipe"),
        new("industrial_cold_water", 10, "pipe"),
        new("industrial_hot_water", 11, "pipe"),
        new("industrial_sewerage", 12, "pipe"),
        new("other_pipe_system", 13, "pipe"),
        new("ventilation", 14, "duct"),
        new("exhaust", 15, "duct"),
        new("pressurization", 16, "duct"),
        new("smoke_exhaust", 17, "duct"),
        new("vacuum", 18, "duct"),
        new("other_duct_system", 19, "duct"),
        new("lighting_circuit", 20, "electrical"),
        new("power_circuit", 21, "electrical"),
        new("other_electrical_system", 22, "electrical")
    ];

    public static SystemCategoryInfo GetSystemCategory(string name)
    {
        var category = SystemCategories.FirstOrDefault(item =>
            string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase));
        return category ?? throw new ArgumentException(
            $"Unknown system category '{name}'. Use renga_system_categories for allowed values.",
            nameof(name));
    }

    public static SystemCategoryInfo GetSystemCategory(int value) =>
        SystemCategories.FirstOrDefault(item => item.Value == value)
        ?? new SystemCategoryInfo($"unknown_{value}", value, "unknown");
}
