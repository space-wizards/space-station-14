using Robust.Shared.Serialization;

namespace Content.Shared.Communications
{
    [Virtual]
    public partial class SharedCommunicationsConsoleComponent : Component
    {
    }

    [Serializable, NetSerializable]
    public sealed class CommunicationsConsoleInterfaceState : BoundUserInterfaceState
    {
        public readonly bool CanAnnounce;
        public readonly bool CanBroadcast = true;
        public readonly bool CanCall;
        public readonly TimeSpan? ExpectedCountdownEnd;
        public readonly bool CountdownStarted;
        public List<string>? AlertLevels;
        public string CurrentAlert;
        public float CurrentAlertDelay;

        // Starlight edit Start
        public readonly TimeSpan? AnnouncementCooldownEnd;
        public readonly TimeSpan? ShuttleCountdownEnd;
        public readonly TimeSpan? CallRecallCooldownEnd;
        public readonly bool ShuttleCallsAllowed;
        public readonly TimeSpan? LastCountdownStart;

        public CommunicationsConsoleInterfaceState(
            bool canAnnounce,
            bool canCall,
            List<string>? alertLevels,
            string currentAlert,
            float currentAlertDelay,
            TimeSpan? expectedCountdownEnd = null,
            TimeSpan? announcementCooldownEnd = null,
            TimeSpan? callRecallCooldownEnd = null,
            TimeSpan? shuttleCountdownEnd = null,
            bool shuttleCallsAllowed = true,
            TimeSpan? lastCountdownStart = null
        )
        // Starlight edit End
        {
            CanAnnounce = canAnnounce;
            CanCall = canCall;
            ExpectedCountdownEnd = expectedCountdownEnd;
            CountdownStarted = expectedCountdownEnd != null;
            AlertLevels = alertLevels;
            CurrentAlert = currentAlert;
            CurrentAlertDelay = currentAlertDelay;
            // Starlight Start
            AnnouncementCooldownEnd = announcementCooldownEnd;
            CallRecallCooldownEnd = callRecallCooldownEnd;
            ShuttleCountdownEnd = shuttleCountdownEnd;
            ShuttleCallsAllowed = shuttleCallsAllowed;
            LastCountdownStart = lastCountdownStart;
            // Starlight End
        }
    }

    [Serializable, NetSerializable]
    public sealed class CommunicationsConsoleSelectAlertLevelMessage : BoundUserInterfaceMessage
    {
        public readonly string Level;

        public CommunicationsConsoleSelectAlertLevelMessage(string level)
        {
            Level = level;
        }
    }

    [Serializable, NetSerializable]
    public sealed class CommunicationsConsoleAnnounceMessage : BoundUserInterfaceMessage
    {
        public readonly string Message;

        public CommunicationsConsoleAnnounceMessage(string message)
        {
            Message = message;
        }
    }

    [Serializable, NetSerializable]
    public sealed class CommunicationsConsoleBroadcastMessage : BoundUserInterfaceMessage
    {
        public readonly string Message;
        public CommunicationsConsoleBroadcastMessage(string message)
        {
            Message = message;
        }
    }

    [Serializable, NetSerializable]
    public sealed class CommunicationsConsoleCallEmergencyShuttleMessage : BoundUserInterfaceMessage
    {
    }

    [Serializable, NetSerializable]
    public sealed class CommunicationsConsoleRecallEmergencyShuttleMessage : BoundUserInterfaceMessage
    {
    }

    [Serializable, NetSerializable]
    public enum CommunicationsConsoleUiKey
    {
        Key
    }
}
