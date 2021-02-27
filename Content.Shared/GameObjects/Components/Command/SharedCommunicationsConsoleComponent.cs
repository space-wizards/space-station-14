#nullable enable
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
        public readonly bool CanCall;
        public readonly TimeSpan? ExpectedCountdownEnd;
        public readonly bool CountdownStarted;

        public CommunicationsConsoleInterfaceState(bool canCall, TimeSpan? expectedCountdownEnd = null)
        {
            CanCall = canCall;
            ExpectedCountdownEnd = expectedCountdownEnd;
            CountdownStarted = expectedCountdownEnd != null;
        }
    }

    [Serializable, NetSerializable]
    public class CommunicationsConsoleCallEmergencyShuttleMessage : BoundUserInterfaceMessage
    {
    }

    [Serializable, NetSerializable]
    public class CommunicationsConsoleRecallEmergencyShuttleMessage : BoundUserInterfaceMessage
    {
    }

    [Serializable, NetSerializable]
    public enum CommunicationsConsoleUiKey
    {
        Key
    }
}
