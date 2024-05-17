using Content.Shared.Actions;
using Content.Shared.Charges.Components;
using Content.Shared.Charges.Systems;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Ninja.Components;
using Content.Shared.Popups;
using Content.Shared.Examine;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Shared.Ninja.Systems;

/// <summary>
/// Handles dashing logic including charge consumption and checking attempt events.
/// </summary>
public sealed class DashAbilitySystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedChargesSystem _charges = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly ExamineSystemShared _examine = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly ActionContainerSystem _actionContainer = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DashAbilityComponent, GetItemActionsEvent>(OnGetItemActions);
        SubscribeLocalEvent<DashAbilityComponent, DashEvent>(OnDash);
        SubscribeLocalEvent<DashAbilityComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(EntityUid uid, DashAbilityComponent component, MapInitEvent args)
    {
        _actionContainer.EnsureAction(uid, ref component.DashActionEntity, component.DashAction);
        Dirty(uid, component);
    }

    private void OnGetItemActions(EntityUid uid, DashAbilityComponent comp, GetItemActionsEvent args)
    {
        var ev = new AddDashActionEvent(args.User);
        RaiseLocalEvent(uid, ev);

        if (ev.Cancelled)
            return;

        args.AddAction(ref comp.DashActionEntity, comp.DashAction);
    }

    /// <summary>
    /// Handle charges and teleport to a visible location.
    /// </summary>
    private void OnDash(EntityUid uid, DashAbilityComponent comp, DashEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        var user = args.Performer;
        args.Handled = true;

        var ev = new DashAttemptEvent(user);
        RaiseLocalEvent(uid, ev);
        if (ev.Cancelled)
            return;

        if (!_hands.IsHolding(user, uid, out var _))
        {
            _popup.PopupClient(Loc.GetString("dash-ability-not-held", ("item", uid)), user, user);
            return;
        }

        TryComp<LimitedChargesComponent>(uid, out var charges);
        if (_charges.IsEmpty(uid, charges))
        {
            _popup.PopupClient(Loc.GetString("dash-ability-no-charges", ("item", uid)), user, user);
            return;
        }
        var origin = _transform.GetMapCoordinates(user);
        var target = args.Target.ToMap(EntityManager, _transform);
        // prevent collision with the user duh
        if (!_examine.InRangeUnOccluded(origin, target, SharedInteractionSystem.MaxRaycastRange, null))
        {
            // can only dash if the destination is visible on screen
            _popup.PopupClient(Loc.GetString("dash-ability-cant-see", ("item", uid)), user, user);
            return;
        }

        _transform.SetCoordinates(user, args.Target);
        _transform.AttachToGridOrMap(user);
        _audio.PlayPredicted(comp.BlinkSound, user, user);
        if (charges != null)
            _charges.UseCharge(uid, charges);
    }
}

/// <summary>
/// Raised on the item before adding the dash action
/// </summary>
public sealed class AddDashActionEvent : CancellableEntityEventArgs
{
    public EntityUid User;

    public AddDashActionEvent(EntityUid user)
    {
        User = user;
    }
}

/// <summary>
/// Raised on the item before dashing is done.
/// </summary>
public sealed class DashAttemptEvent : CancellableEntityEventArgs
{
    public EntityUid User;

    public DashAttemptEvent(EntityUid user)
    {
        User = user;
    }
}
