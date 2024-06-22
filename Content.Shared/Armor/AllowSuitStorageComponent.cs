using Content.Shared.Whitelist;

namespace Content.Shared.Armor;

/// <summary>
///     Used on outerclothing to allow use of suit storage
/// </summary>
[RegisterComponent]
public sealed partial class AllowSuitStorageComponent : Component
{

    [DataField]
    public EntityWhitelist Whitelist = new()
    {
        Components = new[]
        {
            "Item"
        }
    };

}
