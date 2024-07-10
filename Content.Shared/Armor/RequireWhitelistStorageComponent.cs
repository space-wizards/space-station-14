using Content.Shared.Whitelist;

namespace Content.Shared.Armor;

[RegisterComponent]
public sealed partial class RequireWhitelistStorageComponent : Component
{
    /// <summary>
    /// What tag is needed on the slot to equip it.
    /// </summary>
    [DataField]
    public EntityWhitelist? Whitelist;
}
