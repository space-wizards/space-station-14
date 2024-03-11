using Content.Shared.Clothing.Components;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Components;
using Content.Shared.Inventory.Events;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Storage;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Shared.Clothing.EntitySystems;

public sealed partial class PilotedClothingSystem : EntitySystem
{
    [Dependency] private readonly IEntityManager _entMan = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedMoverController _moverController = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PilotedClothingComponent, EntInsertedIntoContainerMessage>(OnEntInserted);
        SubscribeLocalEvent<PilotedClothingComponent, EntRemovedFromContainerMessage>(OnEntRemoved);
        SubscribeLocalEvent<PilotedClothingComponent, GotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<PilotedClothingComponent, GotUnequippedEvent>(OnUnequipped);
    }

    private void OnEntInserted(Entity<PilotedClothingComponent> entity, ref EntInsertedIntoContainerMessage args)
    {
        // Make sure the entity was actually inserted into storage and not a different container.
        if (!TryComp(entity, out StorageComponent? storage) || args.Container != storage.Container)
            return;

        // Check potential pilot against whitelist, if one exists.
        if (entity.Comp.PilotWhitelist != null && !entity.Comp.PilotWhitelist.IsValid(args.Entity))
            return;

        entity.Comp.Pilot = _entMan.GetNetEntity(args.Entity);
        Dirty(entity);

        // Attempt to setup control link, if Pilot and Wearer are both present.
        StartPiloting(entity);
    }

    private void OnEntRemoved(Entity<PilotedClothingComponent> entity, ref EntRemovedFromContainerMessage args)
    {
        // Make sure the removed entity is actually the pilot.
        if (_entMan.GetNetEntity(args.Entity) != entity.Comp.Pilot)
            return;

        StopPiloting(entity);
    }

    private void OnEquipped(Entity<PilotedClothingComponent> entity, ref GotEquippedEvent args)
    {
        if (!TryComp(entity, out ClothingComponent? clothing))
            return;

        // Make sure the clothing item was equipped to the right slot, and not just held in a hand.
        var isCorrectSlot = clothing.Slots.HasFlag(args.SlotFlags);
        if (!isCorrectSlot)
            return;

        entity.Comp.Wearer = _entMan.GetNetEntity(args.Equipee);
        Dirty(entity);

        // Attempt to setup control link, if Pilot and Wearer are both present.
        StartPiloting(entity);
    }

    private void OnUnequipped(Entity<PilotedClothingComponent> entity, ref GotUnequippedEvent args)
    {
        StopPiloting(entity);
    }

    /// <summary>
    /// Attempt to establish movement/interaction relay connection(s) from Pilot to Wearer.
    /// If either is missing, fails and returns false.
    /// </summary>
    private bool StartPiloting(Entity<PilotedClothingComponent> entity)
    {
        // Make sure we have both a Pilot and a Wearer
        if (entity.Comp.Pilot == null || entity.Comp.Wearer == null)
            return false;

        if (!_timing.IsFirstTimePredicted)
            return false;

        var pilotEnt = _entMan.GetEntity(entity.Comp.Pilot.Value);
        var wearerEnt = _entMan.GetEntity(entity.Comp.Wearer.Value);

        // Add component to block prediction of wearer
        AddComp<PilotedByClothingComponent>(wearerEnt);

        if (entity.Comp.RelayMovement)
        {
            // Establish movement input relay.
            _moverController.SetRelay(pilotEnt, wearerEnt);
        }

        if (entity.Comp.RelayInteraction)
        {
            // Establish click input relay.
            var interactionRelay = EnsureComp<InteractionRelayComponent>(pilotEnt);
            _interaction.SetRelay(pilotEnt, wearerEnt, interactionRelay);
        }

        var pilotEv = new StartedPilotingClothingEvent(entity, wearerEnt);
        RaiseLocalEvent(pilotEnt, ref pilotEv);

        var wearerEv = new StartingBeingPilotedByClothing(entity, pilotEnt);
        RaiseLocalEvent(wearerEnt, ref wearerEv);

        return true;
    }

    private bool StopPiloting(Entity<PilotedClothingComponent> entity)
    {
        if (entity.Comp.Pilot == null || entity.Comp.Wearer == null)
            return false;

        // Break the relay connection by removing components.
        var pilotEnt = _entMan.GetEntity(entity.Comp.Pilot.Value);
        RemCompDeferred<RelayInputMoverComponent>(pilotEnt);
        RemCompDeferred<InteractionRelayComponent>(pilotEnt);

        var wearerEnt = _entMan.GetEntity(entity.Comp.Wearer.Value);
        RemCompDeferred<MovementRelayTargetComponent>(wearerEnt);
        RemCompDeferred<PilotedByClothingComponent>(wearerEnt);

        var pilotEv = new StoppedPilotingClothingEvent(entity, wearerEnt);
        RaiseLocalEvent(pilotEnt, ref pilotEv);

        var wearerEv = new StoppedBeingPilotedByClothing(entity, pilotEnt);
        RaiseLocalEvent(wearerEnt, ref wearerEv);

        entity.Comp.Pilot = null;
        entity.Comp.Wearer = null;

        Dirty(entity);
        return true;
    }
}

[ByRefEvent]
public record struct StartedPilotingClothingEvent(EntityUid Clothing, EntityUid Wearer);

[ByRefEvent]
public record struct StoppedPilotingClothingEvent(EntityUid Clothing, EntityUid Wearer);

[ByRefEvent]
public record struct StartingBeingPilotedByClothing(EntityUid Clothing, EntityUid Pilot);

[ByRefEvent]
public record struct StoppedBeingPilotedByClothing(EntityUid Clothing, EntityUid Pilot);
