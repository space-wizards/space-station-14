using Robust.Shared.Serialization;

namespace Content.Shared.Shuttles.Events;

[Serializable, NetSerializable]
public sealed class StartPilotRequestEvent : EntityEventArgs
{
    public EntityUid Uid;
}
