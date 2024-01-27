using System.Linq;
using Content.Server.Interaction;
using Content.Server.Mech.Equipment.Components;
using Content.Server.Mech.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Mech;
using Content.Shared.Mech.Components;
using Content.Shared.Mech.Equipment.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Wall;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;

namespace Content.Server.Mech.Equipment.EntitySystems;

/// <summary>
/// Handles <see cref="MechGrabberComponent"/> and all related UI logic
/// </summary>
public sealed class MechGrabberSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly MechSystem _mech = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly InteractionSystem _interaction = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly TransformSystem _transform = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<MechGrabberComponent, MechEquipmentUiMessageRelayEvent>(OnGrabberMessage);
        SubscribeLocalEvent<MechGrabberComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<MechGrabberComponent, MechEquipmentUiStateReadyEvent>(OnUiStateReady);
        SubscribeLocalEvent<MechGrabberComponent, MechEquipmentRemovedEvent>(OnEquipmentRemoved);
        SubscribeLocalEvent<MechGrabberComponent, AttemptRemoveMechEquipmentEvent>(OnAttemptRemove);

        SubscribeLocalEvent<MechGrabberComponent, InteractNoHandEvent>(OnInteract);
        SubscribeLocalEvent<MechGrabberComponent, GrabberDoAfterEvent>(OnMechGrab);
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

        var item = GetEntity(msg.Item);

        if (!component.ItemContainer.Contains(item))
            return;

        RemoveItem(uid, mech, item, component);
    }

    /// <summary>
    /// Removes an item from the grabber's container
    /// </summary>
    /// <param name="uid">The mech grabber</param>
    /// <param name="mech">The mech it belongs to</param>
    /// <param name="toRemove">The item being removed</param>
    /// <param name="component"></param>
    public void RemoveItem(EntityUid uid, EntityUid mech, EntityUid toRemove, MechGrabberComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        _container.Remove(toRemove, component.ItemContainer);
        var mechxform = Transform(mech);
        var xform = Transform(toRemove);
        _transform.AttachToGridOrMap(toRemove, xform);
        var (mechPos, mechRot) = _transform.GetWorldPositionRotation(mechxform);

        var offset = mechPos + mechRot.RotateVec(component.DepositOffset);
        _transform.SetWorldPositionRotation(xform, offset, Angle.Zero);
        _mech.UpdateUserInterface(mech);
    }

    private void OnEquipmentRemoved(EntityUid uid, MechGrabberComponent component, ref MechEquipmentRemovedEvent args)
    {
        if (!TryComp<MechEquipmentComponent>(uid, out var equipmentComponent) ||
            equipmentComponent.EquipmentOwner == null)
            return;
        var mech = equipmentComponent.EquipmentOwner.Value;

        var allItems = new List<EntityUid>(component.ItemContainer.ContainedEntities);
        foreach (var item in allItems)
        {
            RemoveItem(uid, mech, item, component);
        }
    }

    private void OnAttemptRemove(EntityUid uid, MechGrabberComponent component, ref AttemptRemoveMechEquipmentEvent args)
    {
        args.Cancelled = component.ItemContainer.ContainedEntities.Any();
    }

    private void OnStartup(EntityUid uid, MechGrabberComponent component, ComponentStartup args)
    {
        component.ItemContainer = _container.EnsureContainer<Container>(uid, "item-container");
    }

    private void OnUiStateReady(EntityUid uid, MechGrabberComponent component, MechEquipmentUiStateReadyEvent args)
    {
        var state = new MechGrabberUiState
        {
            Contents = GetNetEntityList(component.ItemContainer.ContainedEntities.ToList()),
            MaxContents = component.MaxContents
        };
        args.States.Add(GetNetEntity(uid), state);
    }

    private void OnInteract(EntityUid uid, MechGrabberComponent component, InteractNoHandEvent args)
    {
        if (args.Handled || args.Target is not {} target)
            return;

        if (TryComp<PhysicsComponent>(target, out var physics) && physics.BodyType == BodyType.Static ||
            HasComp<WallMountComponent>(target) ||
            HasComp<MobStateComponent>(target))
        {
            return;
        }

        if (Transform(target).Anchored)
            return;

        if (component.ItemContainer.ContainedEntities.Count >= component.MaxContents)
            return;

        if (!TryComp<MechComponent>(args.User, out var mech) || mech.PilotSlot.ContainedEntity == target)
            return;

        if (mech.Energy + component.GrabEnergyDelta < 0)
            return;

        if (!_interaction.InRangeUnobstructed(args.User, target))
            return;

        args.Handled = true;
        component.AudioStream = _audio.PlayPvs(component.GrabSound, uid).Value.Entity;
        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, args.User, component.GrabDelay, new GrabberDoAfterEvent(), uid, target: target, used: uid)
        {
            BreakOnTargetMove = true,
            BreakOnUserMove = true
        });
    }

    private void OnMechGrab(EntityUid uid, MechGrabberComponent component, DoAfterEvent args)
    {
        if (args.Cancelled)
        {
            component.AudioStream = _audio.Stop(component.AudioStream);
            return;
        }

        if (args.Handled || args.Args.Target == null)
            return;

        if (!TryComp<MechEquipmentComponent>(uid, out var equipmentComponent) || equipmentComponent.EquipmentOwner == null)
            return;
        if (!_mech.TryChangeEnergy(equipmentComponent.EquipmentOwner.Value, component.GrabEnergyDelta))
            return;

        _container.Insert(args.Args.Target.Value, component.ItemContainer);
        _mech.UpdateUserInterface(equipmentComponent.EquipmentOwner.Value);

        args.Handled = true;
    }
}
