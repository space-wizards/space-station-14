using Robust.Shared.Serialization;

namespace Content.Shared.Whistle.Events;

[Serializable, NetSerializable]
public sealed class OnWhistleEvent : EntityEventArgs
{
    public EntityUid Source;
    public EntityUid User;

    public OnWhistleEvent(EntityUid source, EntityUid user)
    {
        Source = source;
        User = user;
    }
}
