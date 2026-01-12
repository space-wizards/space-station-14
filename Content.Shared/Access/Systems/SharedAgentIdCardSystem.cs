using System.Diagnostics.CodeAnalysis;
using Content.Shared.Access.Components;
using Content.Shared.Interaction;
using Content.Shared.Lock;
using Content.Shared.Popups;
using Content.Shared.Roles;
using Content.Shared.StatusIcon;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Access.Systems;

public abstract class SharedAgentIdCardSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SharedIdCardSystem _idCard = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly LockSystem _lock = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<IdCardComponent, AfterAutoHandleStateEvent>(OnAfterAutoHandleState);
        SubscribeLocalEvent<AgentIDCardComponent, AfterInteractEvent>(OnAfterInteract);

        // BUI
        SubscribeLocalEvent<AgentIDCardComponent, AgentIDCardNameChangedMessage>(OnNameChanged);
        SubscribeLocalEvent<AgentIDCardComponent, AgentIDCardJobChangedMessage>(OnJobChanged);
        SubscribeLocalEvent<AgentIDCardComponent, AgentIDCardJobIconChangedMessage>(OnJobIconChanged);
    }

    private void OnAfterAutoHandleState(Entity<IdCardComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        UpdateUi(ent);
    }

    private void UpdateUi(EntityUid uid)
    {
        if (_ui.TryGetOpenUi(uid, AgentIDCardUiKey.Key, out var bui))
            bui.Update();
    }

    private void OnAfterInteract(Entity<AgentIDCardComponent> ent, ref AfterInteractEvent args)
    {
        if (!args.CanReach)
            return;

        if (!TryComp<AccessComponent>(args.Target, out var targetAccess) ||
            !HasComp<IdCardComponent>(args.Target))
            return;

        if (!TryComp<AccessComponent>(ent, out var access) ||
            !HasComp<IdCardComponent>(ent) ||
            _lock.IsLocked(ent.Owner))
            return;

        var beforeLength = access.Tags.Count;
        access.Tags.UnionWith(targetAccess.Tags);
        var addedLength = access.Tags.Count - beforeLength;

        _popup.PopupClient(Loc.GetString("agent-id-new", ("number", addedLength), ("card", args.Target)), args.Target.Value, args.User);
        if (addedLength > 0)
        {
            Dirty(ent.Owner, access);
            UpdateUi(ent);
        }
    }

    private void OnJobChanged(Entity<AgentIDCardComponent> ent, ref AgentIDCardJobChangedMessage args)
    {
        if (!TryComp<IdCardComponent>(ent, out var idCard))
            return;

        _idCard.TryChangeJobTitle(ent.Owner, args.Job, idCard, player: args.Actor);
        UpdateUi(ent);
    }

    private void OnNameChanged(Entity<AgentIDCardComponent> ent, ref AgentIDCardNameChangedMessage args)
    {
        if (!TryComp<IdCardComponent>(ent, out var idCard))
            return;

        _idCard.TryChangeFullName(ent.Owner, args.Name, idCard, player: args.Actor);
        UpdateUi(ent);
    }

    private void OnJobIconChanged(Entity<AgentIDCardComponent> ent, ref AgentIDCardJobIconChangedMessage args)
    {
        if (!TryComp<IdCardComponent>(ent, out var idCard))
            return;

        if (!_prototype.Resolve(args.JobIconId, out var jobIcon))
            return;

        _idCard.TryChangeJobIcon(ent.Owner, jobIcon, idCard, player: args.Actor);

        if (TryFindJobProtoFromIcon(jobIcon, out var job))
            _idCard.TryChangeJobDepartment(ent.Owner, job, idCard);

        UpdateUi(ent);
    }

    private bool TryFindJobProtoFromIcon(JobIconPrototype jobIcon, [NotNullWhen(true)] out JobPrototype? job)
    {
        foreach (var jobPrototype in _prototype.EnumeratePrototypes<JobPrototype>())
        {
            if (jobPrototype.Icon == jobIcon.ID)
            {
                job = jobPrototype;
                return true;
            }
        }

        job = null;
        return false;
    }
}

/// <summary>
/// Key for the BoundUserInterface.
/// </summary>
[Serializable, NetSerializable]
public enum AgentIDCardUiKey : byte
{
    Key,
}

/// <summary>
/// Send by the client when changing the ID card's name.
/// </summary>
[Serializable, NetSerializable]
public sealed class AgentIDCardNameChangedMessage(string name) : BoundUserInterfaceMessage
{
    /// <summary>
    /// The new name.
    /// </summary>
    public string Name = name;
}

/// <summary>
/// Send by the client when changing the ID card's job.
/// </summary>
[Serializable, NetSerializable]
public sealed class AgentIDCardJobChangedMessage(ProtoId<JobPrototype> job) : BoundUserInterfaceMessage
{
    /// <summary>
    /// The new job title.
    /// This is a string, not a ProtoId since the player can create custom job titles.
    /// </summary>
    public string Job = job;
}

/// <summary>
/// Send by the client when changing the ID card's job icon.
/// </summary>
[Serializable, NetSerializable]
public sealed class AgentIDCardJobIconChangedMessage(ProtoId<JobIconPrototype> jobIconId) : BoundUserInterfaceMessage
{
    /// <summary>
    /// The new status icon.
    /// </summary>
    public ProtoId<JobIconPrototype> JobIconId = jobIconId;
}
