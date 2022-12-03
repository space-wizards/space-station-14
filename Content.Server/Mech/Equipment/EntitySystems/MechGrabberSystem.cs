using System.Linq;
using Content.Server.DoAfter;
using Content.Server.Mech.Equipment.Components;
using Content.Shared.Interaction;
using Content.Shared.Mech;
using Content.Shared.Mech.EntitySystems;
using Robust.Shared.Containers;

namespace Content.Server.Mech.Equipment.EntitySystems;

public sealed class MechGrabberSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedMechSystem _mech = default!;
    [Dependency] private readonly DoAfterSystem _doAfter = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<MechGrabberComponent, MechEquipmentUiMessageRelayEvent>(OnGrabberMessage);
        SubscribeLocalEvent<MechGrabberComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<MechGrabberComponent, InteractNoHandEvent>(OnInteract);
        SubscribeLocalEvent<MechGrabberComponent, MechEquipmentUiStateReadyEvent>(OnUiStateReady);
    }

    private void OnGrabberMessage(EntityUid uid, MechGrabberComponent component, MechEquipmentUiMessageRelayEvent args)
    {
        if (args.Message is not MechGrabberEjectMessage msg)
            return;

        if (!component.ItemContainer.Contains(msg.Uid))
            return;
        component.ItemContainer.Remove(msg.Uid);
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
        if (args.Handled)
            return;
        args.Handled = true;


    }
}
