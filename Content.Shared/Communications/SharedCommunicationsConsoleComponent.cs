using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.Communications
{
    public class SharedCommunicationsConsoleComponent : Component
    {
    }

    [Serializable, NetSerializable]
    public class CommunicationsConsoleInterfaceState : BoundUserInterfaceState
    {
        public readonly bool CanAnnounce;
        public readonly bool CanCall;
        public readonly TimeSpan? ExpectedCountdownEnd;
        public readonly bool CountdownStarted;

        public CommunicationsConsoleInterfaceState(bool canAnnounce, bool canCall, TimeSpan? expectedCountdownEnd = null)
        {
            CanAnnounce = canAnnounce;
            CanCall = canCall;
            ExpectedCountdownEnd = expectedCountdownEnd;
            CountdownStarted = expectedCountdownEnd != null;
        }
    }

    [Serializable, NetSerializable]
    public class CommunicationsConsoleAnnounceMessage : BoundUserInterfaceMessage
    {
        public readonly string Message;

        public CommunicationsConsoleAnnounceMessage(string message)
        {
            Message = message;
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
