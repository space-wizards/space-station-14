using Content.Shared.Charges.Systems;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.Ninja.Components;
using Content.Shared.Popups;

namespace Content.Shared.Ninja.Systems;

public sealed partial class DashAbilitySystem : EntitySystem
{
    [Dependency] private SharedChargesSystem _sharedCharges = default!;
    [Dependency] private ExamineSystemShared _examine = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private PullingSystem _pullingSystem = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    [SubscribeLocalEvent]
    private void OnDash(Entity<DashAbilityComponent> ent, ref DashEvent args)
    {
        var uid = ent.Owner;
        var user = args.Performer;

        var origin = _transform.GetMapCoordinates(user);
        var target = _transform.ToMapCoordinates(args.Target);
        if (!_examine.InRangeUnOccluded(origin, target, SharedInteractionSystem.MaxRaycastRange, null))
        {
            _popup.PopupEntity(Loc.GetString("dash-ability-cant-see", ("item", uid)), user, user);
            return;
        }

        if (!_sharedCharges.TryUseCharge(uid))
        {
            _popup.PopupEntity(Loc.GetString("dash-ability-no-charges", ("item", uid)), user, user);
            return;
        }

        if (TryComp<PullableComponent>(user, out var pull) && _pullingSystem.IsPulled(user, pull))
            _pullingSystem.TryStopPull(user, pull);

        if (TryComp<PullerComponent>(user, out var puller) && TryComp<PullableComponent>(puller.Pulling, out var pullable))
            _pullingSystem.TryStopPull(puller.Pulling.Value, pullable);

        var xform = Transform(user);
        _transform.SetCoordinates(user, xform, args.Target);
        _transform.AttachToGridOrMap(user, xform);
        args.Handled = true;
    }
}
