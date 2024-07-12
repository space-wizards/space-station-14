using Content.Shared.Whitelist;

namespace Content.Shared.Armor;

[RegisterComponent]
public sealed partial class RequireWhitelistStorageComponent : Component
{
    /// <summary>
    /// Whitelist for what is needed on the slot to equip it.
    /// </summary>
    [DataField(required:true)]
    public EntityWhitelist? Whitelist;

    /// <summary>
    /// What slots to check has the whitelist.
    /// </summary>
    [DataField(required:true)]
    public List<string> Slots = new();

    /// <summary>
    /// What slots should ignore the whitelist
    /// </summary>
    [DataField]
    public List<string> IgnoreSlots = new()
    {
        "pocket1",
        "pocket2",
        "pocket3",
        "pocket4"
    };

}
