using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Command
{
    public class SharedCommunicationsConsoleComponent : Component
    {
        public override string Name => "CommunicationsConsole";

    }

    [Serializable, NetSerializable]
    public class CommunicationsConsoleInterfaceState : BoundUserInterfaceState
    {
        public readonly TimeSpan? ExpectedCountdownEnd;
        public readonly bool CountdownStarted;

        public CommunicationsConsoleInterfaceState(TimeSpan? expectedCountdownEnd = null)
        {
            ExpectedCountdownEnd = expectedCountdownEnd;
            CountdownStarted = expectedCountdownEnd != null;

        }
    }

    [Serializable, NetSerializable]
    public class CommunicationsConsoleCallEmergencyShuttleMessage : BoundUserInterfaceMessage
    {
        public CommunicationsConsoleCallEmergencyShuttleMessage()
        {
        }
    }

    [Serializable, NetSerializable]
    public class CommunicationsConsoleRecallEmergencyShuttleMessage : BoundUserInterfaceMessage
    {
        public CommunicationsConsoleRecallEmergencyShuttleMessage()
        {
        }
    }

    [Serializable, NetSerializable]
    public enum CommunicationsConsoleUiKey
    {
        Key
    }
}
