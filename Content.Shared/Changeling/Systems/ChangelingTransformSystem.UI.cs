using Robust.Shared.Serialization;

namespace Content.Shared.Changeling.Systems;

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

[Serializable, NetSerializable]
public enum ChangelingTransformUiKey : byte
{
    Key,
}
