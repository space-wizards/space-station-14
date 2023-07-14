namespace Content.Shared.VendingMachines;

/// <summary>
/// A utility class for storing inventory type names. When
/// adding a new type, it is extremely important to add a
/// name here and use it through this class.
/// </summary>
public static class VendingMachinesInventoryTypeNames
{
    public const string Regular = "RegularInventory";
    public const string Emagged = "EmaggedInventory";
    public const string Contraband = "ContrabandInventory";
}
