using Content.Shared.Access.Components;
using Content.Shared.Inventory;
using Content.Shared.Roles.Jobs;
using Content.Shared.StatusIcon;
using Content.Shared.VoiceMask;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Access.Systems;

/// <summary>
/// Handles the UI of the agent ID, as well as automatically updating from voice masks, implants, etc.
/// </summary>
public abstract partial class SharedAgentIdCardSystem : EntitySystem
{
    [Dependency] private SharedIdCardSystem _card = default!;
    [Dependency] private SharedJobSystem _job = default!;
    [Dependency] private SharedJobStatusSystem _jobStatus = default!;

    [SubscribeLocalEvent]
    private void OnVoiceMaskNameChanged(Entity<AgentIDCardComponent> ent,
        ref InventoryRelayedEvent<VoiceMaskNameUpdatedEvent> args)
    {
        if (!TryComp<IdCardComponent>(ent, out var idCard))
            return;

        if (!args.Args.VoiceMask.Comp.ChangeIDName)
            return;

        _card.TryChangeFullName(ent, args.Args.NewName, idCard);
    }

    [SubscribeLocalEvent]
    private void OnNameChanged(Entity<AgentIDCardComponent> ent, ref AgentIDCardNameChangedMessage args)
    {
        if (!_card.TryChangeFullName(ent, args.Name))
            return;

        UpdateUi(ent);
    }

    [SubscribeLocalEvent]
    private void OnJobChanged(Entity<AgentIDCardComponent> ent, ref AgentIDCardJobChangedMessage args)
    {
        if (!_card.TryChangeJobTitle(ent, args.Job))
            return;

        UpdateUi(ent);
    }

    [SubscribeLocalEvent]
    private void OnJobIconChanged(Entity<AgentIDCardComponent> ent, ref AgentIDCardJobIconChangedMessage args)
    {
        if (!ProtoMan.Resolve(args.JobIconId, out var jobIcon) ||
            !_card.TryChangeJobIcon(ent, jobIcon))
            return;

        if (_job.TryGetJobFromIcon(jobIcon.ID, out var job))
            _card.TryChangeJobDepartment(ent, job);

        _jobStatus.UpdateIdHolderStatus(ent);
        UpdateUi(ent);
    }

    /// <summary>
    /// Update the agent id UI with new component info.
    /// </summary>
    public virtual void UpdateUi(EntityUid entity)
    {
        // Overridden on client
    }
}

/// <summary>
/// Key representing which bound user interface is currently open.
/// Useful when there are multiple UI for an object. Here it's future-proofing only.
/// </summary>
[Serializable, NetSerializable]
public enum AgentIDCardUiKey : byte
{
    Key,
}

/// <summary>
/// Sent from the agent ID UI to change the card name.
/// </summary>
[Serializable, NetSerializable]
public sealed class AgentIDCardNameChangedMessage(string name) : BoundUserInterfaceMessage
{
    public string Name { get; } = name;
}

/// <summary>
/// Sent from the agent ID UI to change the job title.
/// </summary>
[Serializable, NetSerializable]
public sealed class AgentIDCardJobChangedMessage(string job) : BoundUserInterfaceMessage
{
    public string Job { get; } = job;
}

/// <summary>
/// Sent from the agent ID UI to change the job icon.
/// </summary>
[Serializable, NetSerializable]
public sealed class AgentIDCardJobIconChangedMessage(ProtoId<JobIconPrototype> icon) : BoundUserInterfaceMessage
{
    public ProtoId<JobIconPrototype> JobIconId { get; } = icon;
}
