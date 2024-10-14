using Content.Shared.Whitelist;

namespace Content.Shared.BroadcastInteractionUsingToContainer;

/// <summary>
///  Provides broadcasting interaction from entity to entities in it's container. Also implements whitelists/blacklists.
/// </summary>
[RegisterComponent]
public sealed partial class BroadcastInteractUsingToContainerComponent : Component
{
    [DataField]
    public EntityWhitelist? Whitelist;

    [DataField]
    public EntityWhitelist? Blacklist;
}
