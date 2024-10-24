using Content.Shared.Whitelist;

namespace Content.Shared.BroadcastInteractionUsingToContainer.Components;

public abstract partial class BroadcastUsingToContainerComponent : Component
{
    [DataField]
    public EntityWhitelist? Whitelist;

    [DataField]
    public EntityWhitelist? Blacklist;
}


/// <summary>
///  Provides broadcasting interaction event from used entity to entities in it's container. Also implements whitelists/blacklists.
/// </summary>
[RegisterComponent]
public sealed partial class BroadcastInteractingFromContainerComponent : BroadcastUsingToContainerComponent { }

/// <summary>
///  Provides broadcasting target of interaction event from target entity to entities in it's container. Also implements whitelists/blacklists.
/// </summary>
[RegisterComponent]
public sealed partial class BroadcastInteractingIntoContainerComponent : BroadcastUsingToContainerComponent { }

/// <summary>
///  Provides broadcasting after interaction event from used entity to entities in it's container. Also implements whitelists/blacklists.
/// </summary>
[RegisterComponent]
public sealed partial class BroadcastAfterInteractingFromContainerComponent : BroadcastUsingToContainerComponent { }


