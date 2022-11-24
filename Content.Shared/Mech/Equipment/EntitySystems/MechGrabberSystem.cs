using Content.Shared.Interaction;
using Content.Shared.Mech.EntitySystems;
using Content.Shared.Mech.Equipment.Components;
using Content.Shared.Pulling;
using Content.Shared.Pulling.Components;

namespace Content.Shared.Mech.Equipment.EntitySystems;

public sealed class MechGrabberSystem : EntitySystem
{
    [Dependency] private readonly SharedMechSystem _mech = default!;
    [Dependency] private readonly SharedPullingSystem _pulling = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<MechGrabberComponent, MechEquipmentGetUiInformationEvent>(OnGetUiInfo);
        SubscribeLocalEvent<MechGrabberComponent, InteractNoHandEvent>(OnInteract);
    }

    private void OnGetUiInfo(EntityUid uid, MechGrabberComponent component, ref MechEquipmentGetUiInformationEvent args)
    {
        args.Information.CanBeRemoved = true;
        args.Information.CanBeEnabled = false;
    }

    private void OnInteract(EntityUid uid, MechGrabberComponent component, InteractNoHandEvent args)
    {
        if (args.Handled)
            return;
        args.Handled = true;

        var inRange = _interaction.InRangeUnobstructed(args.User, args.ClickLocation);

        if (inRange && _pulling.TogglePull(args.User, args.Target))
        {
            if (_pulling.IsPulling(args.User))
                _mech.TryChangeEnergy(args.User, component.EnergyPerGrab);

            return;
        }

        var pulled = _pulling.GetPulled(args.User);
        if (pulled == EntityUid.Invalid || !TryComp<SharedPullableComponent>(pulled, out var pullable))
        {
            args.Handled = false; //weird logic flow but trust me
            return;
        }
        _pulling.TryStopPull(pullable, args.User);
    }
}
