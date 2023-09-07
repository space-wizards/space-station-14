using Robust.Shared.Serialization;

namespace Content.Shared.NukeOps;

[Serializable, NetSerializable]
public enum WarDeclaratorUiKey
{
    Key,
}

public enum WarConditionStatus : byte
{
    WAR_READY,
    WAR_DELAY,
    YES_WAR,
    NO_WAR_UNKNOWN,
    NO_WAR_TIMEOUT,
    NO_WAR_SMALL_CREW,
    NO_WAR_SHUTTLE_DEPARTED
}

[Serializable, NetSerializable]
public sealed class WarDeclaratorBoundUserInterfaceState : BoundUserInterfaceState
{
    public WarConditionStatus Status;
    public int MinCrew;
    public TimeSpan Delay;
    public TimeSpan EndTime;

    public WarDeclaratorBoundUserInterfaceState(WarConditionStatus status, int minCrew, TimeSpan delay, TimeSpan endTime)
    {
        Status = status;
        MinCrew = minCrew;
        Delay = delay;
        EndTime = endTime;
    }
}

[Serializable, NetSerializable]
public sealed class WarDeclaratorActivateMessage : BoundUserInterfaceMessage
{
    public string Message { get; }

    public WarDeclaratorActivateMessage(string msg)
    {
        Message = msg;
    }
}
