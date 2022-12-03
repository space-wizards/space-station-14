using System.Linq;
using Content.Server.DoAfter;
using Content.Server.Interaction;
using Content.Server.Mech.Components;
using Content.Server.Mech.Equipment.Components;
using Content.Server.Mech.Systems;
using Content.Shared.Interaction;
using Content.Shared.Mech;
using Content.Shared.Mech.Equipment.Components;
using Content.Shared.Wall;
using Robust.Shared.Containers;
using Robust.Shared.Map;

namespace Content.Server.Mech.Equipment.EntitySystems;

public sealed class MechGrabberSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly MechSystem _mech = default!;
    [Dependency] private readonly DoAfterSystem _doAfter = default!;
    [Dependency] private readonly InteractionSystem _interaction = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<MechGrabberComponent, MechEquipmentUiMessageRelayEvent>(OnGrabberMessage);
        SubscribeLocalEvent<MechGrabberComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<MechGrabberComponent, MechEquipmentUiStateReadyEvent>(OnUiStateReady);

        SubscribeLocalEvent<MechGrabberComponent, InteractNoHandEvent>(OnInteract);
        SubscribeLocalEvent<MechGrabberComponent, MechGrabberGrabFinishedEvent>(OnGrabFinished);
        SubscribeLocalEvent<MechGrabberComponent, MechGrabberGrabCancelledEvent>(OnGrabCancelled);
    }

    private void OnGrabberMessage(EntityUid uid, MechGrabberComponent component, MechEquipmentUiMessageRelayEvent args)
    {
        if (args.Message is not MechGrabberEjectMessage msg)
            return;

        if (!TryComp<MechEquipmentComponent>(uid, out var equipmentComponent) ||
            equipmentComponent.EquipmentOwner == null)
            return;
        var mech = equipmentComponent.EquipmentOwner.Value;

        var targetCoords = new EntityCoordinates(mech, component.DepositOffset);
        if (!_interaction.InRangeUnobstructed(mech, targetCoords))
            return;

        if (!component.ItemContainer.Contains(msg.Item))
            return;

        component.ItemContainer.Remove(msg.Item);
        var mechxform = Transform(mech);
        var xform = Transform(msg.Item);
        xform.AttachToGridOrMap();
        xform.WorldPosition = mechxform.WorldPosition + mechxform.WorldRotation.RotateVec(component.DepositOffset);
        _mech.UpdateUserInterface(mech);
    }

    private void OnStartup(EntityUid uid, MechGrabberComponent component, ComponentStartup args)
    {
        component.ItemContainer = _container.EnsureContainer<Container>(uid, "item-container");
    }

    private void OnUiStateReady(EntityUid uid, MechGrabberComponent component, MechEquipmentUiStateReadyEvent args)
    {
        var state = new MechGrabberUiState
        {
            Contents = component.ItemContainer.ContainedEntities.ToList(),
            MaxContents = component.MaxContents
        };
        args.States.Add(uid, state);
    }

    private void OnInteract(EntityUid uid, MechGrabberComponent component, InteractNoHandEvent args)
    {
        if (args.Handled || args.Target == null)
            return;

        var xform = Transform(args.Target.Value);
        if (xform.Anchored || HasComp<WallMountComponent>(args.Target.Value))
            return;

        if (component.ItemContainer.ContainedEntities.Count >= component.MaxContents)
            return;

        if (!TryComp<MechComponent>(args.User, out var mech))
            return;

        if (mech.Energy + component.EnergyPerGrab < 0)
            return;

        if (component.Token != null)
            return;

        args.Handled = true;
        component.Token = new();
        _doAfter.DoAfter(new DoAfterEventArgs(args.User, component.GrabDelay, component.Token.Token, args.Target, uid)
        {
            BreakOnTargetMove = true,
            BreakOnUserMove = true,
            UsedFinishedEvent = new MechGrabberGrabFinishedEvent(args.Target.Value),
            UserCancelledEvent = new MechGrabberGrabCancelledEvent()
        });
    }

    private void OnGrabFinished(EntityUid uid, MechGrabberComponent component, MechGrabberGrabFinishedEvent args)
    {
        component.Token = null;

        if (!TryComp<MechEquipmentComponent>(uid, out var equipmentComponent) || equipmentComponent.EquipmentOwner == null)
            return;
        if (!_mech.TryChangeEnergy(equipmentComponent.EquipmentOwner.Value, component.EnergyPerGrab))
            return;

        component.ItemContainer.Insert(args.Grabbed);
        _mech.UpdateUserInterface(equipmentComponent.EquipmentOwner.Value);
    }

    private void OnGrabCancelled(EntityUid uid, MechGrabberComponent component, MechGrabberGrabCancelledEvent args)
    {
        component.Token = null;
    }
}

public sealed class MechGrabberGrabFinishedEvent : EntityEventArgs
{
    public EntityUid Grabbed;

    public MechGrabberGrabFinishedEvent(EntityUid grabbed)
    {
        Grabbed = grabbed;
    }
}

public sealed class MechGrabberGrabCancelledEvent : EntityEventArgs
{

}
