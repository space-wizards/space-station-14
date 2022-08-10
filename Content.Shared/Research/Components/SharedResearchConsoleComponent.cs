using Robust.Shared.Serialization;

namespace Content.Shared.Research.Components
{
    [NetSerializable, Serializable]
    public enum ResearchConsoleUiKey : byte
    {
        Key,
    }

    [Serializable, NetSerializable]
    public sealed class ConsoleUnlockTechnologyMessage : BoundUserInterfaceMessage
    {
        public string Id;

        public ConsoleUnlockTechnologyMessage(string id)
        {
            Id = id;
        }
    }

    [Serializable, NetSerializable]
    public sealed class ConsoleServerSyncMessage : BoundUserInterfaceMessage
    {
        public ConsoleServerSyncMessage()
        {}
    }

    [Serializable, NetSerializable]
    public sealed class ConsoleServerSelectionMessage : BoundUserInterfaceMessage
    {
        public ConsoleServerSelectionMessage()
        {}
    }

    [Serializable, NetSerializable]
    public sealed class ResearchConsoleBoundInterfaceState : BoundUserInterfaceState
    {
        public int Points;
        public int PointsPerSecond;
        public ResearchConsoleBoundInterfaceState(int points, int pointsPerSecond)
        {
            Points = points;
            PointsPerSecond = pointsPerSecond;
        }
    }
}
