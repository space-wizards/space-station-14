using Content.Shared.Examine;
using Content.Shared.Ghost.Roles.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Popups;
using Content.Shared.Verbs;

namespace Content.Shared.Ghost.Roles;

/// <summary>
/// This handles logic and interaction related to <see cref="ToggleableGhostRoleComponent"/>.
/// </summary>
public sealed class ToggleableGhostRoleSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ToggleableGhostRoleComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<ToggleableGhostRoleComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<ToggleableGhostRoleComponent, MindAddedMessage>(OnMindAdded);
        SubscribeLocalEvent<ToggleableGhostRoleComponent, MindRemovedMessage>(OnMindRemoved);
        SubscribeLocalEvent<ToggleableGhostRoleComponent, GetVerbsEvent<ActivationVerb>>(AddWipeVerb);
    }

    private void OnUseInHand(Entity<ToggleableGhostRoleComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        // Check if a mind is present.
        if (TryComp<MindContainerComponent>(ent.Owner, out var mind) && mind.HasMind)
        {
            _popup.PopupClient(Loc.GetString(ent.Comp.ExamineTextMindPresent), ent.Owner, args.User, PopupType.Large);
            return;
        }
        if (HasComp<GhostTakeoverAvailableComponent>(ent.Owner))
        {
            _popup.PopupClient(Loc.GetString(ent.Comp.ExamineTextMindSearching), ent.Owner, args.User);
            return;
        }
        _popup.PopupClient(Loc.GetString(ent.Comp.BeginSearchingText), ent.Owner, args.User);

        UpdateAppearance(ent.Owner, ToggleableGhostRoleStatus.Searching);

        ActivateGhostRole(ent.AsNullable());
    }

    public void ActivateGhostRole(Entity<ToggleableGhostRoleComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        var ghostRole = EnsureComp<GhostRoleComponent>(ent);
        EnsureComp<GhostTakeoverAvailableComponent>(ent);

        // GhostRoleComponent inherits custom settings from the <see cref="ToggleableGhostRoleComponent"/>.
        ghostRole.RoleName = Loc.GetString(ent.Comp.RoleName);
        ghostRole.RoleDescription = Loc.GetString(ent.Comp.RoleDescription);
        ghostRole.RoleRules = Loc.GetString(ent.Comp.RoleRules);
        ghostRole.JobProto = ent.Comp.JobProto;
        ghostRole.MindRoles = ent.Comp.MindRoles;
    }

    private void OnExamined(Entity<ToggleableGhostRoleComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        string textKey;
        if (TryComp<MindContainerComponent>(ent.Owner, out var mindComp) && mindComp.HasMind)
            textKey = ent.Comp.ExamineTextMindPresent;
        else if (HasComp<GhostTakeoverAvailableComponent>(ent.Owner))
            textKey = ent.Comp.ExamineTextMindSearching;
        else
            textKey = ent.Comp.ExamineTextNoMind;

        args.PushMarkup(Loc.GetString(textKey));
    }

    private void OnMindAdded(Entity<ToggleableGhostRoleComponent> ent, ref MindAddedMessage args)
    {
        // Mind was added, shutdown the ghost role stuff so it won't get in the way.
        RemCompDeferred<GhostTakeoverAvailableComponent>(ent.Owner);
        UpdateAppearance(ent.Owner, ToggleableGhostRoleStatus.On);
    }

    private void OnMindRemoved(Entity<ToggleableGhostRoleComponent> ent, ref MindRemovedMessage args)
    {
        // Mind was removed, prepare for re-toggle of the role.
        RemCompDeferred<GhostRoleComponent>(ent.Owner);
        UpdateAppearance(ent.Owner, ToggleableGhostRoleStatus.Off);
    }

    private void UpdateAppearance(EntityUid uid, ToggleableGhostRoleStatus status)
    {
        _appearance.SetData(uid, ToggleableGhostRoleVisuals.Status, status);
    }

    private void AddWipeVerb(Entity<ToggleableGhostRoleComponent> ent, ref GetVerbsEvent<ActivationVerb> args)
    {
        if (args.Hands == null || !args.CanAccess || !args.CanInteract)
            return;

        var user = args.User;
        if (TryComp<MindContainerComponent>(ent.Owner, out var mind) && mind.HasMind)
        {
            ActivationVerb verb = new()
            {
                Text = Loc.GetString(ent.Comp.WipeVerbText),
                Act = () =>
                {
                    if (!_mind.TryGetMind(ent.Owner, out var mindId, out var mind))
                        return;
                    // Wiping device :(
                    // The shutdown of the Mind should cause automatic reset of the pAI during OnMindRemoved
                    _mind.TransferTo(mindId, null, mind: mind);
                    _popup.PopupClient(Loc.GetString(ent.Comp.WipeVerbPopup), ent.Owner, user, PopupType.Large);
                }
            };
            args.Verbs.Add(verb);
        }
        else if (HasComp<GhostTakeoverAvailableComponent>(ent.Owner))
        {
            ActivationVerb verb = new()
            {
                Text = Loc.GetString(ent.Comp.StopSearchVerbText),
                Act = () =>
                {
                    if (ent.Comp.Deleted || !HasComp<GhostTakeoverAvailableComponent>(ent.Owner))
                        return;

                    RemCompDeferred<GhostTakeoverAvailableComponent>(ent.Owner);
                    RemCompDeferred<GhostRoleComponent>(ent.Owner);
                    _popup.PopupClient(Loc.GetString(ent.Comp.StopSearchVerbPopup), ent.Owner, user);
                    UpdateAppearance(ent.Owner, ToggleableGhostRoleStatus.Off);
                }
            };
            args.Verbs.Add(verb);
        }
    }

    /// <summary>
    /// If there is a player present, kicks it out.
    /// If not, prevents future ghosts taking it.
    /// No popups are made, but appearance is updated.
    /// </summary>
    public void Wipe(EntityUid uid)
    {
        if (TryComp<MindContainerComponent>(uid, out var mindContainer) &&
            mindContainer.HasMind &&
            _mind.TryGetMind(uid, out var mindId, out var mind))
        {
            _mind.TransferTo(mindId, null, mind: mind);
        }

        if (!HasComp<GhostTakeoverAvailableComponent>(uid))
            return;

        RemCompDeferred<GhostTakeoverAvailableComponent>(uid);
        RemCompDeferred<GhostRoleComponent>(uid);
        UpdateAppearance(uid, ToggleableGhostRoleStatus.Off);
    }
}
