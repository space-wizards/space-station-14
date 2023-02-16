using Robust.Shared.Serialization;

namespace Content.Shared.NukeOps
{
    /// <summary>
    /// Key representing which <see cref="BoundUserInterface"/> is currently open.
    /// Useful when there are multiple UI for an object. Here it's future-proofing only.
    /// </summary>
    [Serializable, NetSerializable]
    public enum WarDeclaratorUiKey
    {
        Key,
    }

    public enum WarConditionStatus : byte
    {
        YES_WAR,
        TC_DISTRIBUTED,
        NO_WAR_UNKNOWN,
        NO_WAR_TIMEOUT,
        NO_WAR_SMALL_CREW,
        NO_WAR_SHUTTLE_DEPARTED
    }

    /// <summary>
    /// Represents a <see cref="HandLabelerComponent"/> state that can be sent to the client
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class WarDeclaratorBoundUserInterfaceState : BoundUserInterfaceState
    {
        public string Message { get; }
        public WarConditionStatus Status;
        public int MinCrew;

        public WarDeclaratorBoundUserInterfaceState(string msg, WarConditionStatus status, int minCrew)
        {
            Message = msg;
            Status = status;
            MinCrew = minCrew;
        }
    }

    [Serializable, NetSerializable]
    public sealed class WarDeclaratorChangedMessage : BoundUserInterfaceMessage
    {
        public string Message { get; }

        public WarDeclaratorChangedMessage(string msg)
        {
            Message = msg;
        }
    }

    [Serializable, NetSerializable]
    public sealed class WarDeclaratorPressedWarButton : BoundUserInterfaceMessage
    {
        public string? Message { get; }

        public WarDeclaratorPressedWarButton(string? msg)
        {
            Message = msg;
        }
    }
}
