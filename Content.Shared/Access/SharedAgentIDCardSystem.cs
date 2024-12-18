using Content.Shared.StatusIcon;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Access.Systems
{
    public abstract class SharedAgentIdCardSystem : EntitySystem
    {
        // Just for friending for now
    }

    /// <summary>
    /// Key representing which <see cref="PlayerBoundUserInterface"/> is currently open.
    /// Useful when there are multiple UI for an object. Here it's future-proofing only.
    /// </summary>
    [Serializable, NetSerializable]
    public enum AgentIDCardUiKey : byte
    {
        Key,
    }

    /// <summary>
    /// Represents an <see cref="AgentIDCardComponent"/> state that can be sent to the client
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class AgentIDCardBoundUserInterfaceState : BoundUserInterfaceState
    {
        public string CurrentName { get; }
        public string CurrentJob { get; }
        public string CurrentJobIconId { get; }
        public uint? CurrentNumber { get; } // DeltaV
        public AgentIDCardBoundUserInterfaceState(string currentName, string currentJob, string currentJobIconId, uint? currentNumber = null) // DeltaV - Added currentNumber
        {
            CurrentName = currentName;
            CurrentJob = currentJob;
            CurrentJobIconId = currentJobIconId;
            CurrentNumber = currentNumber; // DeltaV
    }
}

// DeltaV - Add number change message
[Serializable, NetSerializable]
public sealed class AgentIDCardNumberChangedMessage : BoundUserInterfaceMessage
{
    public uint Number { get; }

    public AgentIDCardNumberChangedMessage(uint number)
    {
        Number = number;
        }
    }

    [Serializable, NetSerializable]
    public sealed class AgentIDCardNameChangedMessage : BoundUserInterfaceMessage
    {
        public string Name { get; }

        public AgentIDCardNameChangedMessage(string name)
        {
            Name = name;
        }
    }

    [Serializable, NetSerializable]
    public sealed class AgentIDCardJobChangedMessage : BoundUserInterfaceMessage
    {
        public string Job { get; }

        public AgentIDCardJobChangedMessage(string job)
        {
            Job = job;
        }
    }

    [Serializable, NetSerializable]
    public sealed class AgentIDCardJobIconChangedMessage : BoundUserInterfaceMessage
    {
        public ProtoId<JobIconPrototype> JobIconId { get; }

        public AgentIDCardJobIconChangedMessage(ProtoId<JobIconPrototype> jobIconId)
        {
            JobIconId = jobIconId;
        }
    }
}
