using Robust.Shared.Serialization;

namespace Content.Shared.Changeling.Transform;

/// <summary>
/// Send when a player selects an intentity to transform into in the radial menu.
/// </summary>
[Serializable, NetSerializable]
public sealed class ChangelingTransformIdentitySelectMessage(NetEntity targetIdentity) : BoundUserInterfaceMessage
{
    /// <summary>
    /// The uid of the cloned identity.
    /// </summary>
    public readonly NetEntity TargetIdentity = targetIdentity;
}

// TODO: Replace with component states.
// We are already networking the ChangelingIdentityComponent, which contains all this information,
// so we can just read it from them from the component and update the UI in an AfterAuotHandleState subscription.
[Serializable, NetSerializable]
public sealed class ChangelingTransformBoundUserInterfaceState(List<NetEntity> identities) : BoundUserInterfaceState
{
    /// <summary>
    /// The uids of the cloned identities.
    /// </summary>
    public readonly List<NetEntity> Identites = identities;
}

[Serializable, NetSerializable]
public enum TransformUI : byte
{
    Key,
}
