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

[Serializable, NetSerializable]
public sealed class SurgeryRealmRequestSelfEvent : EntityEventArgs
{
}

[Serializable, NetSerializable]
public sealed class SurgeryRealmAcceptSelfEvent : EntityEventArgs
{
}
