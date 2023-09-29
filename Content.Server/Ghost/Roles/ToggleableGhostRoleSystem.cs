using Content.Server.Ghost.Roles.Components;
using Content.Server.Mind;
using Content.Server.Mind.Components;
using Content.Server.PAI;
using Content.Shared.Examine;
using Content.Shared.Interaction.Events;
using Content.Shared.Mind;
using Content.Shared.Popups;
using Content.Shared.Verbs;

namespace Content.Server.Ghost.Roles;

/// <summary>
/// This handles logic and interaction related to <see cref="ToggleableGhostRoleComponent"/>
/// </summary>
public sealed class ToggleableGhostRoleSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    //todo this really shouldn't be in here but this system was converted from PAIs
    [Dependency] private readonly PAISystem _pai = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ToggleableGhostRoleComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<ToggleableGhostRoleComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<ToggleableGhostRoleComponent, MindAddedMessage>(OnMindAdded);
        SubscribeLocalEvent<ToggleableGhostRoleComponent, MindRemovedMessage>(OnMindRemoved);
        SubscribeLocalEvent<ToggleableGhostRoleComponent, GetVerbsEvent<ActivationVerb>>(AddWipeVerb);
    }

    private void OnUseInHand(EntityUid uid, ToggleableGhostRoleComponent component, UseInHandEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        // check if a mind is present
        if (TryComp<MindContainerComponent>(uid, out var mind) && mind.HasMind)
        {
            _popup.PopupEntity(Loc.GetString(component.ExamineTextMindPresent), uid, args.User, PopupType.Large);
            return;
        }
        if (HasComp<GhostTakeoverAvailableComponent>(uid))
        {
            _popup.PopupEntity(Loc.GetString(component.ExamineTextMindSearching), uid, args.User);
            return;
        }
        _popup.PopupEntity(Loc.GetString(component.BeginSearchingText), uid, args.User);

        UpdateAppearance(uid, ToggleableGhostRoleStatus.Searching);

        var ghostRole = EnsureComp<GhostRoleComponent>(uid);
        EnsureComp<GhostTakeoverAvailableComponent>(uid);
        ghostRole.RoleName = Loc.GetString(component.RoleName);
        ghostRole.RoleDescription = Loc.GetString(component.RoleDescription);
    }

    private void OnExamined(EntityUid uid, ToggleableGhostRoleComponent component, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        if (TryComp<MindContainerComponent>(uid, out var mind) && mind.HasMind)
        {
            args.PushMarkup(Loc.GetString(component.ExamineTextMindPresent));
        }
        else if (HasComp<GhostTakeoverAvailableComponent>(uid))
        {
            args.PushMarkup(Loc.GetString(component.ExamineTextMindSearching));
        }
        else
        {
            args.PushMarkup(Loc.GetString(component.ExamineTextNoMind));
        }
    }

    private void OnMindAdded(EntityUid uid, ToggleableGhostRoleComponent pai, MindAddedMessage args)
    {
        // Mind was added, shutdown the ghost role stuff so it won't get in the way
        RemCompDeferred<GhostTakeoverAvailableComponent>(uid);
        UpdateAppearance(uid, ToggleableGhostRoleStatus.On);
    }

    private void OnMindRemoved(EntityUid uid, ToggleableGhostRoleComponent component, MindRemovedMessage args)
    {
        UpdateAppearance(uid, ToggleableGhostRoleStatus.Off);
    }

    private void UpdateAppearance(EntityUid uid, ToggleableGhostRoleStatus status)
    {
        _appearance.SetData(uid, ToggleableGhostRoleVisuals.Status, status);
    }

    private void AddWipeVerb(EntityUid uid, ToggleableGhostRoleComponent component, GetVerbsEvent<ActivationVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        if (TryComp<MindContainerComponent>(uid, out var mind) && mind.HasMind)
        {
            ActivationVerb verb = new()
            {
                Text = Loc.GetString(component.WipeVerbText),
                Act = () =>
                {
                    if (!_mind.TryGetMind(uid, out var mindId, out var mind))
                        return;
                    // Wiping device :(
                    // The shutdown of the Mind should cause automatic reset of the pAI during OnMindRemoved
                    // EDIT: But it doesn't!!!! Wtf? Do stuff manually
                    _mind.TransferTo(mindId, null, mind: mind);
                    _popup.PopupEntity(Loc.GetString(component.WipeVerbPopup), uid, args.User, PopupType.Large);
                    UpdateAppearance(uid, ToggleableGhostRoleStatus.Off);
                    _pai.PAITurningOff(uid);
                }
            };
            args.Verbs.Add(verb);
        }
        else if (HasComp<GhostTakeoverAvailableComponent>(uid))
        {
            ActivationVerb verb = new()
            {
                Text = Loc.GetString(component.StopSearchVerbText),
                Act = () =>
                {
                    if (component.Deleted || !HasComp<GhostTakeoverAvailableComponent>(uid))
                        return;
                    RemCompDeferred<GhostTakeoverAvailableComponent>(uid);
                    RemCompDeferred<GhostRoleComponent>(uid);
                    _popup.PopupEntity(Loc.GetString(component.StopSearchVerbPopup), uid, args.User);
                    UpdateAppearance(uid, ToggleableGhostRoleStatus.Off);
                    _pai.PAITurningOff(uid);
                }
            };
            args.Verbs.Add(verb);
        }
    }
}
