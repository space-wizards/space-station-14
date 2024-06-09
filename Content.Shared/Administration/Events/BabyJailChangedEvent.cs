using Robust.Shared.Serialization;

namespace Content.Shared.Administration.Events;

[Serializable, NetSerializable]
public sealed class BabyJailStatus
{
    public bool Enabled;
    public bool ShowReason;
    public int MaxAccountAgeMinutes;
    public int MaxOverallMinutes;
}

[Serializable, NetSerializable]
public sealed class BabyJailChangedEvent(BabyJailStatus status) : EntityEventArgs
{
    public BabyJailStatus Status = status;
}
