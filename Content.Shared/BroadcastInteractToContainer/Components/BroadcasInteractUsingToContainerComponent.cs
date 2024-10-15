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
///  Provides broadcasting interaction event from entity to entities in it's container. Also implements whitelists/blacklists.
/// </summary>
[RegisterComponent]
public sealed partial class BroadcastInteractUsingToContainerComponent : BroadcastUsingToContainerComponent { }

/// <summary>
///  Provides broadcasting target of interaction event from entity to entities in it's container. Also implements whitelists/blacklists.
/// </summary>
[RegisterComponent]
public sealed partial class BroadcastInteractUsingTargetToContainerComponent : BroadcastUsingToContainerComponent { }

/// <summary>
///  Provides broadcasting after interaction event from entity to entities in it's container. Also implements whitelists/blacklists.
/// </summary>
[RegisterComponent]
public sealed partial class BroadcastAfterInteractUsingToContainerComponent : BroadcastUsingToContainerComponent { }


