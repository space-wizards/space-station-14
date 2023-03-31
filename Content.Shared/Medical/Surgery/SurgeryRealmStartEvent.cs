using Robust.Shared.Serialization;

namespace Content.Shared.Medical.Surgery;

[Serializable, NetSerializable]
public sealed class SurgeryRealmStartEvent : EntityEventArgs
{
    public EntityUid Camera { get; }

    public SurgeryRealmStartEvent(EntityUid camera)
    {
        Camera = camera;
    }
}
