using Robust.Shared.Serialization;

/*
 * TODO: Remove baby jail code once a more mature gateway process is established. This code is only being issued as a stopgap to help with potential tiding in the immediate future.
 */

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
