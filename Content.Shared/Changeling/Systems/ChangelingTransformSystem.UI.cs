using Robust.Shared.Serialization;

namespace Content.Shared.Changeling.Systems;

/// <summary>
/// Send when a player selects an identity to transform into in the radial menu.
/// </summary>
[Serializable, NetSerializable]
public sealed class ChangelingTransformIdentitySelectMessage(NetEntity targetIdentity) : BoundUserInterfaceMessage
{
    /// <summary>
    /// The uid of the stored identity.
    /// </summary>
    public readonly NetEntity TargetIdentity = targetIdentity;
}

/// <summary>
/// Send when a player selects an identity to drop from their storage.
/// </summary>
[Serializable, NetSerializable]
public sealed class ChangelingTransformIdentityDropMessage(NetEntity targetIdentity) : BoundUserInterfaceMessage
{
    /// <summary>
    /// The uid of the stored identity.
    /// </summary>
    public readonly NetEntity TargetIdentity = targetIdentity;
}

[Serializable, NetSerializable]
public enum ChangelingTransformUiKey : byte
{
    Key,
}
