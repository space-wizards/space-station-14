using Robust.Shared.Serialization;

namespace Content.Shared.Communications
{
    [Virtual]
    public class SharedCommunicationsConsoleComponent : Component
    {
    }

    [Serializable, NetSerializable]
    public sealed class CommunicationsConsoleInterfaceState : BoundUserInterfaceState
    {
        public readonly bool CanAnnounce;
        public readonly bool CanCall;
        public readonly bool CanCallErt;
        public readonly TimeSpan? ExpectedCountdownEnd;
        public readonly TimeSpan? ErtCountdownTime;
        public readonly bool CountdownStarted;
        public readonly bool ErtCountdownStarted;
        public List<string>? AlertLevels;
        public List<string>? ErtsList;
        public string CurrentAlert;
        public float CurrentAlertDelay;

        public CommunicationsConsoleInterfaceState(bool canAnnounce,
            bool canCall,
            bool canCallErt,
            List<string>? alertLevels,
            List<string>? ertsList,
            string currentAlert,
            float currentAlertDelay,
            TimeSpan? expectedCountdownEnd = null,
            TimeSpan? ertCountdownTipe = null)
        {
            CanAnnounce = canAnnounce;
            CanCall = canCall;
            CanCallErt = canCallErt;
            ExpectedCountdownEnd = expectedCountdownEnd;
            ErtCountdownTime = ertCountdownTipe;
            CountdownStarted = expectedCountdownEnd != null;
            ErtCountdownStarted = ertCountdownTipe != null;
            AlertLevels = alertLevels;
            ErtsList = ertsList;
            CurrentAlert = currentAlert;
            CurrentAlertDelay = currentAlertDelay;
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
    public sealed class CommunicationsConsoleCallEmergencyShuttleMessage : BoundUserInterfaceMessage
    {
    }

    [Serializable, NetSerializable]
    public sealed class CommunicationsConsoleRecallEmergencyShuttleMessage : BoundUserInterfaceMessage
    {
    }

    [Serializable, NetSerializable]
    public sealed class CommunicationsConsoleCallErtMessage : BoundUserInterfaceMessage
    {
        public readonly string ErtGroup;

        public CommunicationsConsoleCallErtMessage(string ertGroup)
        {
            ErtGroup = ertGroup;
        }
    }

    [Serializable, NetSerializable]
    public sealed class CommunicationsConsoleRecallErtMessage : BoundUserInterfaceMessage
    {
    }

    [Serializable, NetSerializable]
    public sealed class CommunicationsConsoleSelectErtMessage : BoundUserInterfaceMessage
    {
    }

    [Serializable, NetSerializable]
    public enum CommunicationsConsoleUiKey
    {
        Key
    }
}
