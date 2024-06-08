using Robust.Shared.Serialization;

namespace Content.Shared.Administration.Events;

[Serializable, NetSerializable]
public sealed class BabyJailStatus
{
    public bool Enabled;
    public bool ShowReason;
    public int MaxAccountAgeHours;
    public int MaxOverallHours;
}

[Serializable, NetSerializable]
public sealed class BabyJailChangedEvent(BabyJailStatus status) : EntityEventArgs
{
    public BabyJailStatus Status = status;
}
