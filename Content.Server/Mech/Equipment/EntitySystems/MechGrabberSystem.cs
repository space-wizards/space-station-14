using System.Linq;
using Content.Server.DoAfter;
using Content.Server.Interaction;
using Content.Server.Mech.Components;
using Content.Server.Mech.Equipment.Components;
using Content.Server.Mech.Systems;
using Content.Shared.Interaction;
using Content.Shared.Mech;
using Content.Shared.Mech.Equipment.Components;
using Content.Shared.MobState.Components;
using Content.Shared.Wall;
using Robust.Shared.Containers;
using Robust.Shared.Map;

namespace Content.Server.Mech.Equipment.EntitySystems;

/// <summary>
/// Handles <see cref="MechGrabberComponent"/> and all related UI logic
/// </summary>
public sealed class MechGrabberSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly MechSystem _mech = default!;
    [Dependency] private readonly DoAfterSystem _doAfter = default!;
    [Dependency] private readonly InteractionSystem _interaction = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<MechGrabberComponent, MechEquipmentUiMessageRelayEvent>(OnGrabberMessage);
        SubscribeLocalEvent<MechGrabberComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<MechGrabberComponent, MechEquipmentUiStateReadyEvent>(OnUiStateReady);
        SubscribeLocalEvent<MechGrabberComponent, MechEquipmentRemovedEvent>(OnEquipmentRemoved);
        SubscribeLocalEvent<MechGrabberComponent, AttemptRemoveMechEquipmentEvent>(OnAttemptRemove);

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

        RemoveItem(uid, mech, msg.Item, component);
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

        component.ItemContainer.Remove(toRemove);
        var mechxform = Transform(mech);
        var xform = Transform(toRemove);
        xform.AttachToGridOrMap();
        xform.WorldPosition = mechxform.WorldPosition + mechxform.WorldRotation.RotateVec(component.DepositOffset);
        xform.WorldRotation = Angle.Zero;
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
            Contents = component.ItemContainer.ContainedEntities.ToList(),
            MaxContents = component.MaxContents
        };
        args.States.Add(uid, state);
    }

    private void OnInteract(EntityUid uid, MechGrabberComponent component, InteractNoHandEvent args)
    {
        if (args.Handled || args.Target is not {} target)
            return;

        var xform = Transform(target);
        if (xform.Anchored || HasComp<WallMountComponent>(target) || HasComp<MobStateComponent>(target))
            return;

        if (component.ItemContainer.ContainedEntities.Count >= component.MaxContents)
            return;

        if (!TryComp<MechComponent>(args.User, out var mech) || mech.PilotSlot.ContainedEntity == target)
            return;

        if (mech.Energy + component.GrabEnergyDelta < 0)
            return;

        if (component.Token != null)
            return;

        if (!_interaction.InRangeUnobstructed(args.User, target))
            return;

        args.Handled = true;
        component.Token = new();
        component.AudioStream = _audio.PlayPvs(component.GrabSound, uid);
        _doAfter.DoAfter(new DoAfterEventArgs(args.User, component.GrabDelay, component.Token.Token, target, uid)
        {
            BreakOnTargetMove = true,
            BreakOnUserMove = true,
            UsedFinishedEvent = new MechGrabberGrabFinishedEvent(target),
            UsedCancelledEvent = new MechGrabberGrabCancelledEvent()
        });
    }

    private void OnGrabFinished(EntityUid uid, MechGrabberComponent component, MechGrabberGrabFinishedEvent args)
    {
        component.Token = null;

        if (!TryComp<MechEquipmentComponent>(uid, out var equipmentComponent) || equipmentComponent.EquipmentOwner == null)
            return;
        if (!_mech.TryChangeEnergy(equipmentComponent.EquipmentOwner.Value, component.GrabEnergyDelta))
            return;

        component.ItemContainer.Insert(args.Grabbed);
        _mech.UpdateUserInterface(equipmentComponent.EquipmentOwner.Value);
    }

    private void OnGrabCancelled(EntityUid uid, MechGrabberComponent component, MechGrabberGrabCancelledEvent args)
    {
        component.AudioStream?.Stop();
        component.Token = null;
    }
}
