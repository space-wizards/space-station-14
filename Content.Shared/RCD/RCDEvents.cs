using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.RCD;

[Serializable, NetSerializable]
public sealed class RCDSystemMessage(ProtoId<RCDPrototype> protoId) : BoundUserInterfaceMessage
{
    public ProtoId<RCDPrototype> ProtoId = protoId;
}

[Serializable, NetSerializable]
public sealed class RCDConstructionGhostRotationEvent(NetEntity netEntity, Direction direction) : EntityEventArgs
{
    public readonly NetEntity NetEntity = netEntity;
    public readonly Direction Direction = direction;
}

/// <summary>
/// An event raised on an entity when it is attempted to be destroyed with an RCD.
/// </summary>
/// <param name="User">The user attempting to deconstruct the </param>
[ByRefEvent]
public sealed partial class AttemptRCDDeconstructionEvent(EntityUid user, EntityUid tool) : CancellableEntityEventArgs
{
    public readonly EntityUid User = user;
    public readonly EntityUid Tool = tool;
    public string Reason = string.Empty;
}

[Serializable, NetSerializable]
public enum RcdUiKey : byte
{
    Key
}
