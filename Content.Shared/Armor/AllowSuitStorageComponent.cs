using Content.Shared.Whitelist;

namespace Content.Shared.Armor;

/// <summary>
///     Used on outerclothing to allow use of suit storage
/// </summary>
[RegisterComponent]
public sealed partial class AllowSuitStorageComponent : Component
{
    /// <summary>
    /// Whitelist for what entities are allowed in the suit storage slot.
    /// </summary>
    [DataField]
    public EntityWhitelist Whitelist = new()
    {
        Components = new[] {"Item"}
    };

    /// <summary>
    /// Examine string for enabling suit storage
    /// </summary>
    [DataField]
    public LocId Examine = "examine-enable-suit-storage-base";
}
