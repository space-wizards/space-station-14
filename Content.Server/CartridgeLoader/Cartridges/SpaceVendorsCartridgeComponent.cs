using Content.Shared.CartridgeLoader.Cartridges;

namespace Content.Server.CartridgeLoader.Cartridges;

[RegisterComponent, Access(typeof(SpaceVendorsCartridgeSystem))]
public sealed class SpaceVendorsCartridgeComponent : Component
{
    /// <summary>
    /// List of valuable items
    /// </summary>
    [DataField("appraisedItems"), ViewVariables(VVAccess.ReadWrite)]
    public List<AppraisedItem> AppraisedItems = new();

    /// <summary>
    /// Limit on the number of saves
    /// </summary>
    [DataField("maxSavedItems"), ViewVariables(VVAccess.ReadWrite)]
    public int MaxSavedItems = 9;
}
