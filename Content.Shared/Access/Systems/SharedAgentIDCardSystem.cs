using System.Diagnostics.CodeAnalysis;
using Content.Shared.Access.Components;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Lock;
using Content.Shared.Popups;
using Content.Shared.Roles;
using Content.Shared.StatusIcon;
using Content.Shared.VoiceMask;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Access.Systems;

/// <summary>
/// Handles things related to the agent ID, such as copying access and the UI.
/// </summary>
public abstract partial class SharedAgentIdCardSystem : EntitySystem
{
    [Dependency] private LockSystem _lock = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedIdCardSystem _card = default!;
    [Dependency] private SharedJobStatusSystem _jobStatus = default!;

    /// <inheritdoc />
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AgentIDCardComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<AgentIDCardComponent, InventoryRelayedEvent<VoiceMaskNameUpdatedEvent>>(OnVoiceMaskNameChanged);
        // BUI
        SubscribeLocalEvent<AgentIDCardComponent, AgentIDCardNameChangedMessage>(OnNameChanged);
        SubscribeLocalEvent<AgentIDCardComponent, AgentIDCardJobChangedMessage>(OnJobChanged);
        SubscribeLocalEvent<AgentIDCardComponent, AgentIDCardJobIconChangedMessage>(OnJobIconChanged);
    }

    // TODO this should be its own component
    /// <summary>
    /// Steals access from interacted ids.
    /// </summary>
    private void OnAfterInteract(Entity<AgentIDCardComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Target == null || !args.CanReach || _lock.IsLocked(ent.Owner) ||
            !TryComp<AccessComponent>(args.Target, out var targetAccess) || !HasComp<IdCardComponent>(args.Target))
            return;

        // Am I an id?
        if (!TryComp<AccessComponent>(ent, out var access) || !HasComp<IdCardComponent>(ent))
            return;

        var beforeLength = access.Tags.Count;
        access.Tags.UnionWith(targetAccess.Tags);
        var addedLength = access.Tags.Count - beforeLength;

        _popup.PopupPredicted(Loc.GetString("agent-id-new", ("number", addedLength), ("card", args.Target)),
            args.Target.Value,
            args.User);
        if (addedLength > 0)
            Dirty(ent, access);
    }

    private void OnVoiceMaskNameChanged(Entity<AgentIDCardComponent> ent,
        ref InventoryRelayedEvent<VoiceMaskNameUpdatedEvent> args)
    {
        if (!TryComp<IdCardComponent>(ent, out var idCard))
            return;

        if (!args.Args.VoiceMask.Comp.ChangeIDName)
            return;

        _card.TryChangeFullName(ent, args.Args.NewName, idCard);
    }

    private void OnNameChanged(Entity<AgentIDCardComponent> ent, ref AgentIDCardNameChangedMessage args)
    {
        if (!_card.TryChangeFullName(ent, args.Name))
            return;

        UpdateUi(ent);
    }

    private void OnJobChanged(Entity<AgentIDCardComponent> ent, ref AgentIDCardJobChangedMessage args)
    {
        if (!_card.TryChangeJobTitle(ent, args.Job))
            return;

        UpdateUi(ent);
    }

    private void OnJobIconChanged(Entity<AgentIDCardComponent> ent, ref AgentIDCardJobIconChangedMessage args)
    {
        if (!ProtoMan.Resolve(args.JobIconId, out var jobIcon) ||
            !_card.TryChangeJobIcon(ent, jobIcon))
            return;

        if (TryFindJobProtoFromIcon(jobIcon, out var job))
            _card.TryChangeJobDepartment(ent, job);

        _jobStatus.UpdateStatus(Transform(ent).ParentUid);
        UpdateUi(ent);
    }

    /// <summary>
    /// Attempts to find a matching job to a job icon. TODO move this somewhere else
    /// </summary>
    /// <returns> True if a JobPrototype is found. </returns>
    private bool TryFindJobProtoFromIcon(JobIconPrototype jobIcon, [NotNullWhen(true)] out JobPrototype? job)
    {
        job = null;

        foreach (var jobPrototype in ProtoMan.EnumeratePrototypes<JobPrototype>())
        {
            if (jobPrototype.Icon != jobIcon.ID)
                continue;

            job = jobPrototype;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Update the agent id UI with new component info.
    /// </summary>
    protected virtual void UpdateUi(EntityUid entity)
    {
        // Overridden on client
    }
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

[Serializable, NetSerializable]
public sealed class AgentIDCardNameChangedMessage(string name) : BoundUserInterfaceMessage
{
    public string Name { get; } = name;
}

[Serializable, NetSerializable]
public sealed class AgentIDCardJobChangedMessage(string job) : BoundUserInterfaceMessage
{
    public string Job { get; } = job;
}

[Serializable, NetSerializable]
public sealed class AgentIDCardJobIconChangedMessage(ProtoId<JobIconPrototype> icon) : BoundUserInterfaceMessage
{
    public ProtoId<JobIconPrototype> JobIconId { get; } = icon;
}
