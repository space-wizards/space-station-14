using Content.Shared.Clothing.Components;
using Content.Shared.Inventory.Events;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Storage;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Shared.Clothing.EntitySystems;

public sealed partial class PilotedClothingSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedMoverController _moverController = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;

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
        if (_whitelist.IsWhitelistFail(entity.Comp.PilotWhitelist, args.Entity))
            return;

        entity.Comp.Pilot = args.Entity;
        Dirty(entity);

        // Attempt to setup control link, if Pilot and Wearer are both present.
        StartPiloting(entity);
    }

    private void OnEntRemoved(Entity<PilotedClothingComponent> entity, ref EntRemovedFromContainerMessage args)
    {
        // Make sure the removed entity is actually the pilot.
        if (args.Entity != entity.Comp.Pilot)
            return;

        StopPiloting(entity);
        entity.Comp.Pilot = null;
        Dirty(entity);
    }

    private void OnEquipped(Entity<PilotedClothingComponent> entity, ref GotEquippedEvent args)
    {
        if (!TryComp(entity, out ClothingComponent? clothing))
            return;

        // Make sure the clothing item was equipped to the right slot, and not just held in a hand.
        var isCorrectSlot = (clothing.Slots & args.SlotFlags) != Inventory.SlotFlags.NONE;
        if (!isCorrectSlot)
            return;

        entity.Comp.Wearer = args.Equipee;
        Dirty(entity);

        // Attempt to setup control link, if Pilot and Wearer are both present.
        StartPiloting(entity);
    }

    private void OnUnequipped(Entity<PilotedClothingComponent> entity, ref GotUnequippedEvent args)
    {
        StopPiloting(entity);

        entity.Comp.Wearer = null;
        Dirty(entity);
    }

    /// <summary>
    /// Attempts to establish movement/interaction relay connection(s) from Pilot to Wearer.
    /// If either is missing, fails and returns false.
    /// </summary>
    private bool StartPiloting(Entity<PilotedClothingComponent> entity)
    {
        // Make sure we have both a Pilot and a Wearer
        if (entity.Comp.Pilot == null || entity.Comp.Wearer == null)
            return false;

        if (!_timing.IsFirstTimePredicted)
            return false;

        var pilotEnt = entity.Comp.Pilot.Value;
        var wearerEnt = entity.Comp.Wearer.Value;

        // Add component to block prediction of wearer
        EnsureComp<PilotedByClothingComponent>(wearerEnt);

        if (entity.Comp.RelayMovement)
        {
            // Establish movement input relay.
            _moverController.SetRelay(pilotEnt, wearerEnt);
        }

        var pilotEv = new StartedPilotingClothingEvent(entity, wearerEnt);
        RaiseLocalEvent(pilotEnt, ref pilotEv);

        var wearerEv = new StartingBeingPilotedByClothing(entity, pilotEnt);
        RaiseLocalEvent(wearerEnt, ref wearerEv);

        return true;
    }

    /// <summary>
    /// Removes components from the Pilot and Wearer to stop the control relay.
    /// Returns false if a connection does not already exist.
    /// </summary>
    private bool StopPiloting(Entity<PilotedClothingComponent> entity)
    {
        if (entity.Comp.Pilot == null || entity.Comp.Wearer == null)
            return false;

        // Clean up components on the Pilot
        var pilotEnt = entity.Comp.Pilot.Value;
        RemCompDeferred<RelayInputMoverComponent>(pilotEnt);

        // Clean up components on the Wearer
        var wearerEnt = entity.Comp.Wearer.Value;
        RemCompDeferred<MovementRelayTargetComponent>(wearerEnt);
        RemCompDeferred<PilotedByClothingComponent>(wearerEnt);

        // Raise an event on the Pilot
        var pilotEv = new StoppedPilotingClothingEvent(entity, wearerEnt);
        RaiseLocalEvent(pilotEnt, ref pilotEv);

        // Raise an event on the Wearer
        var wearerEv = new StoppedBeingPilotedByClothing(entity, pilotEnt);
        RaiseLocalEvent(wearerEnt, ref wearerEv);

        return true;
    }
}

/// <summary>
/// Raised on the Pilot when they gain control of the Wearer.
/// </summary>
[ByRefEvent]
public record struct StartedPilotingClothingEvent(EntityUid Clothing, EntityUid Wearer);

/// <summary>
/// Raised on the Pilot when they lose control of the Wearer,
/// due to the Pilot exiting the clothing or the clothing being unequipped by the Wearer.
/// </summary>
[ByRefEvent]
public record struct StoppedPilotingClothingEvent(EntityUid Clothing, EntityUid Wearer);

/// <summary>
/// Raised on the Wearer when the Pilot gains control of them.
/// </summary>
[ByRefEvent]
public record struct StartingBeingPilotedByClothing(EntityUid Clothing, EntityUid Pilot);

/// <summary>
/// Raised on the Wearer when the Pilot loses control of them
/// due to the Pilot exiting the clothing or the clothing being unequipped by the Wearer.
/// </summary>
[ByRefEvent]
public record struct StoppedBeingPilotedByClothing(EntityUid Clothing, EntityUid Pilot);
