namespace Wms.DomainService.Seed;

public static class Seeder
{
    public static IReadOnlyList<string> DefaultRoles() =>
    [
        "SuperAdmin",
        "WarehouseManager",
        "QualityInspector",
        "Operator",
        "Auditor",
        "AiAdmin"
    ];
}
