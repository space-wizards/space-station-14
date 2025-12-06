using Robust.Shared.Serialization;

namespace Content.Shared.Research.Components
{
    [NetSerializable, Serializable]
    public enum ResearchConsoleUiKey : byte
    {
        Key,
    }

    [Serializable, NetSerializable]
    public sealed class ConsoleUnlockTechnologyMessage(string id) : BoundUserInterfaceMessage
    {
        public string Id = id;
    }

    [Serializable, NetSerializable]
    public sealed class ConsoleRediscoverTechnologyMessage : BoundUserInterfaceMessage;

    [Serializable, NetSerializable]
    public sealed class ConsoleServerSelectionMessage : BoundUserInterfaceMessage
    {

    }

    [Serializable, NetSerializable]
    public sealed class ResearchConsoleBoundInterfaceState(int points, TimeSpan nextRediscover, int rediscoverCost) : BoundUserInterfaceState
    {
        public int Points = points;

        public TimeSpan NextRediscover = nextRediscover;

        public int RediscoverCost = rediscoverCost;
    }
}
