using Robust.Shared.Serialization;

namespace Content.Shared.Communications
{
    [Virtual]
    public partial class SharedCommunicationsConsoleComponent : Component
    {
        public const string FirstPrivilegedSlotId = "CommunicationsConsole-firstId";
        public const string SecondPrivilegedSlotId = "CommunicationsConsole-secondId";
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

        public readonly bool ERTCanCall;
        public List<string>? ERTList;
        public readonly TimeSpan? ERTCountdownTime;
        public readonly bool ERTCountdownStarted;

        public readonly bool IsFirstPrivilegedIdPresent;
        public readonly bool IsSecondPrivilegedIdPresent;
        public readonly bool IsFirstPrivilegedIdValid;
        public readonly bool IsSecondPrivilegedIdValid;

        public CommunicationsConsoleInterfaceState(
            bool canAnnounce,
            bool canCall,
            List<string>? alertLevels,
            string currentAlert,
            float currentAlertDelay,

            TimeSpan? expectedCountdownEnd = null,

            bool ertCanCall = false,
            List<string>? ertList = null,
            TimeSpan? ertCountdownTipe = null,

            bool isFirstPrivilegedIdPresent = false,
            bool isSecondPrivilegedIdPresent = false,
            bool isFirstPrivilegedIdValid = false,
            bool isSecondPrivilegedIdValid = false)
        {
            CanAnnounce = canAnnounce;
            CanCall = canCall;
            ExpectedCountdownEnd = expectedCountdownEnd;
            CountdownStarted = expectedCountdownEnd != null;
            AlertLevels = alertLevels;
            CurrentAlert = currentAlert;
            CurrentAlertDelay = currentAlertDelay;

            ERTCanCall = ertCanCall;
            ERTList = ertList;
            ERTCountdownTime = ertCountdownTipe;
            ERTCountdownStarted = ertCountdownTipe != null;

            IsFirstPrivilegedIdPresent = isFirstPrivilegedIdPresent;
            IsSecondPrivilegedIdPresent = isSecondPrivilegedIdPresent;
            IsFirstPrivilegedIdValid = isFirstPrivilegedIdValid;
            IsSecondPrivilegedIdValid = isSecondPrivilegedIdValid;
        }
    }

    [Serializable, NetSerializable]
    public sealed class CommunicationsConsoleSelectAlertLevelMessage : BoundUserInterfaceMessage
    {
    }

    [Serializable, NetSerializable]
    public sealed class CommunicationsConsoleSetAlertLevelMessage : BoundUserInterfaceMessage
    {
        public readonly string Level;

        public CommunicationsConsoleSetAlertLevelMessage(string level)
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
    public sealed class CommunicationsConsoleCallERTMessage : BoundUserInterfaceMessage
    {
        public readonly string ERTTeam;
        public readonly string Message;

        public CommunicationsConsoleCallERTMessage(string ertTeam, string message)
        {
            ERTTeam = ertTeam;
            Message = message;
        }
    }

    [Serializable, NetSerializable]
    public sealed class CommunicationsConsoleRecallERTMessage : BoundUserInterfaceMessage
    {
    }

    [Serializable, NetSerializable]
    public sealed class CommunicationsConsoleSelectERTMessage : BoundUserInterfaceMessage
    {
    }

    [Serializable, NetSerializable]
    public enum CommunicationsConsoleUiKey
    {
        Key
    }
}
