using Content.Shared.Actions.Events;
using Content.Shared.Charges.Systems;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.Popups;
using Content.Shared.Teleportation.Components;

namespace Content.Shared.Teleportation.Systems;

public sealed class SharedTeleportationActionSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedChargesSystem _charges = default!;
    [Dependency] private readonly ExamineSystemShared _examine = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly PullingSystem _pullingSystem = default!;

    private const string OccludedMessage = "dash-ability-cant-see";
    private const string OutOfChargesMessage = "dash-ability-no-charges";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TeleportationActionComponent, TeleportActionEvent>(OnTeleportAction);
    }

    private void OnTeleportAction(Entity<TeleportationActionComponent> ent, ref TeleportActionEvent args)
    {
        if (args.Handled)
            return;

        var user = args.Performer;
        var teleportCauser = ent.Owner;
        var origin = _transform.GetMapCoordinates(user);
        var target = _transform.ToMapCoordinates(args.Target);
        var transform = Transform(user);

        if (transform.MapID != _transform.GetMapId(args.Target))
            return;

        if (!_examine.InRangeUnOccluded(origin, target, SharedInteractionSystem.MaxRaycastRange, null))
        {
            // can only dash if the destination is visible on screen
            _popup.PopupClient(Loc.GetString(OccludedMessage, ("item", teleportCauser)), user, user);
            return;
        }

        if (!_charges.TryUseCharge(teleportCauser))
        {
            _popup.PopupClient(Loc.GetString(OutOfChargesMessage, ("item", teleportCauser)), user, user);
            return;
        }


        // Check if the user is BEING pulled, and escape if so
        if (TryComp<PullableComponent>(user, out var pull) && _pullingSystem.IsPulled(user, pull))
            _pullingSystem.TryStopPull(user, pull);

        if (ent.Comp.DropsPulled)
        {
            // Check if the user is pulling anything, and drop it if so
            if (TryComp<PullerComponent>(user, out var puller) && TryComp<PullableComponent>(puller.Pulling, out var pullable))
                _pullingSystem.TryStopPull(puller.Pulling.Value, pullable);
        }

        _transform.SetCoordinates(user, transform, args.Target);
        _transform.AttachToGridOrMap(user, transform);

        args.Handled = true;
    }
}
