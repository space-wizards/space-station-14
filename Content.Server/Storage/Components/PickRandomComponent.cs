using Content.Server.Storage.EntitySystems;
using Content.Shared.Whitelist;

namespace Content.Server.Storage.Components;

/// <summary>
/// Adds a verb to pick a random item from a container.
/// Only picks items that match the whitelist.
/// </summary>
[RegisterComponent]
[Access(typeof(PickRandomSystem))]
public sealed partial class PickRandomComponent : Component
{
    /// <summary>
    /// Whitelist for potential picked items.
    /// </summary>
    [DataField("whitelist"), ViewVariables(VVAccess.ReadWrite)]
    public EntityWhitelist? Whitelist;

    /// <summary>
    /// Locale id for the pick verb text.
    /// </summary>
    [DataField("verbText"), ViewVariables(VVAccess.ReadWrite)]
    public string VerbText = "comp-pick-random-verb-text";

    /// <summary>
    /// Locale id for the empty storage message.
    /// </summary>
    [DataField("emptyText"), ViewVariables(VVAccess.ReadWrite)]
    public string EmptyText = "comp-pick-random-empty";
}
