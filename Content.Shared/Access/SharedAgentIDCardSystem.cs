using Content.Shared.Access.Components;
using Content.Shared.Lock;
using Content.Shared.StatusIcon;
using Content.Shared.UserInterface;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Access.Systems
{
    public abstract partial class SharedAgentIdCardSystem : EntitySystem
    {
         // [Dependency] private readonly LockSystem _lock = default!;

         // public override void Initialize()
         // {
         //     base.Initialize();
         //
         //     // SubscribeLocalEvent<AgentIDCardComponent, ActivatableUIOpenAttemptEvent>(OnActivatableUIOpenAttemptEvent);
         // }

         // private void OnActivatableUIOpenAttemptEvent(Entity<AgentIDCardComponent> ent, ref ActivatableUIOpenAttemptEvent args)
         // {
         //     if (_lock.IsLocked(ent.Owner))
         //         args.Cancel();
         // }
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

        public AgentIDCardBoundUserInterfaceState(string currentName, string currentJob, string currentJobIconId)
        {
            CurrentName = currentName;
            CurrentJob = currentJob;
            CurrentJobIconId = currentJobIconId;
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
