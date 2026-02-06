using System.Linq;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Destructible;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Jittering;
using Content.Shared.Kitchen.Components;
using Content.Shared.Popups;
using Content.Shared.Power;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Stacks;
using JetBrains.Annotations;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared.Kitchen.EntitySystems;

[UsedImplicitly]
public abstract class SharedReagentGrinderSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainersSystem = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlotsSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedStackSystem _stackSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly SharedDestructibleSystem _destructible = default!;
    [Dependency] private readonly SharedJitteringSystem _jitter = default!;
    [Dependency] private readonly SharedPowerReceiverSystem _power = default!;
    [Dependency] private readonly SharedPowerStateSystem _powerState = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InsideReagentGrinderComponent, SolutionContainerChangedEvent>(OnBeakerSolutionContainerChanged);

        SubscribeLocalEvent<ReagentGrinderComponent, ComponentStartup>(OnGrinderStartup);
        SubscribeLocalEvent<ReagentGrinderComponent, ContainerIsRemovingAttemptEvent>(OnEntRemovingAttempt);
        SubscribeLocalEvent<ReagentGrinderComponent, EntRemovedFromContainerMessage>(OnEntRemoved);
        SubscribeLocalEvent<ReagentGrinderComponent, EntInsertedIntoContainerMessage>(OnEntInserted);
        SubscribeLocalEvent((EntityUid uid, ReagentGrinderComponent _, ref PowerChangedEvent _) => UpdateUi(uid));
        SubscribeLocalEvent<ReagentGrinderComponent, InteractUsingEvent>(OnInteractUsing);

        SubscribeLocalEvent<ReagentGrinderComponent, ReagentGrinderStartMessage>(OnStartMessage);
        SubscribeLocalEvent<ReagentGrinderComponent, ReagentGrinderToggleAutoModeMessage>(OnToggleAutoModeMessage);
        SubscribeLocalEvent<ReagentGrinderComponent, ReagentGrinderEjectChamberAllMessage>(OnEjectChamberAllMessage);
        SubscribeLocalEvent<ReagentGrinderComponent, ReagentGrinderEjectChamberContentMessage>(OnEjectChamberContentMessage);
    }

    private void OnBeakerSolutionContainerChanged(Entity<InsideReagentGrinderComponent> ent, ref SolutionContainerChangedEvent args)
    {
        // Update the UI if the reagents inside the beaker are changed.
        // This is needed in case the component state for the container is applied before that of the solution container
        // or if the beaker somehow changes its contents on its own (for with example SolutionRegenerationComponent).
        UpdateUi(Transform(ent).ParentUid);
    }

    private void OnGrinderStartup(Entity<ReagentGrinderComponent> ent, ref ComponentStartup args)
    {
        ent.Comp.InputContainer = _containerSystem.EnsureContainer<Container>(ent.Owner, ReagentGrinderComponent.InputContainerId);
    }

    private void OnEntRemovingAttempt(Entity<ReagentGrinderComponent> ent, ref ContainerIsRemovingAttemptEvent args)
    {
        // Allow server states to be applied without cancelling container changes.
        if (_timing.ApplyingState)
            return;

        if (args.Container.ID != ReagentGrinderComponent.BeakerSlotId
            && args.Container.ID != ReagentGrinderComponent.InputContainerId)
            return;

        // Cannot remove items while the grinder is active.
        if (IsActive(ent.AsNullable()))
            args.Cancel();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _timing.CurTime;
        var query = EntityQueryEnumerator<ActiveReagentGrinderComponent, ReagentGrinderComponent>();
        while (query.MoveNext(out var uid, out _, out var grinderComp))
        {
            if (grinderComp.EndTime == null || grinderComp.EndTime > curTime)
                continue;

            FinishGrinding((uid, grinderComp));
        }
    }

    private void OnEntRemoved(EntityUid uid, ReagentGrinderComponent comp, EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != ReagentGrinderComponent.BeakerSlotId
            && args.Container.ID != ReagentGrinderComponent.InputContainerId)
            return;

        // Always update the UI on the client, both during prediction and when applying a game state.
        UpdateUi(uid);

        // The component changes from the code below are already part of the same game state being applied.
        if (_timing.ApplyingState)
            return;

        if (args.Container.ID == ReagentGrinderComponent.BeakerSlotId) // Beaker removed.
        {
            RemComp<InsideReagentGrinderComponent>(args.Entity);
            _appearanceSystem.SetData(uid, ReagentGrinderVisualState.BeakerAttached, false);
        }
    }

    private void OnEntInserted(EntityUid uid, ReagentGrinderComponent comp, EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != ReagentGrinderComponent.BeakerSlotId
            && args.Container.ID != ReagentGrinderComponent.InputContainerId)
            return;

        // Always update the UI on the client, both during prediction and when applying a game state.
        UpdateUi(uid);

        // The component changes from the code below are already part of the same game state being applied.
        if (_timing.ApplyingState)
            return;

        if (args.Container.ID == ReagentGrinderComponent.BeakerSlotId) // Beaker inserted.
        {
            EnsureComp<InsideReagentGrinderComponent>(args.Entity);
            _appearanceSystem.SetData(uid, ReagentGrinderVisualState.BeakerAttached, true);
        }

        // Start grinder when in auto mode.
        if (comp.AutoMode != GrinderAutoMode.Off)
        {
            var program = comp.AutoMode == GrinderAutoMode.Grind ? GrinderProgram.Grind : GrinderProgram.Juice;
            StartGrinder((uid, comp), program);
        }
    }

    private void OnInteractUsing(Entity<ReagentGrinderComponent> ent, ref InteractUsingEvent args)
    {
        var heldEnt = args.Used;

        if (!HasComp<ExtractableComponent>(heldEnt))
        {
            if (!HasComp<FitsInDispenserComponent>(heldEnt))
            {
                // This is ugly but we can't use whitelistFailPopup because there are 2 containers with different whitelists.
                _popupSystem.PopupClient(Loc.GetString("reagent-grinder-component-cannot-put-entity-message"), ent.Owner, args.User);
            }

            // Entity did NOT pass the whitelist for grind/juice.
            // Wouldn't want the clown grinding up the Captain's ID card now would you?
            // Why am I asking you? You're biased.
            return;
        }

        if (args.Handled)
            return;

        // Cap the chamber. Don't want someone putting in 500 entities and ejecting them all at once.
        // Maybe I should have done that for the microwave too?
        if (ent.Comp.InputContainer.ContainedEntities.Count >= ent.Comp.StorageMaxEntities)
            return;

        if (!_containerSystem.Insert(heldEnt, ent.Comp.InputContainer))
            return;

        args.Handled = true;
    }

    public virtual void UpdateUi(EntityUid uid) { }

    private void OnStartMessage(Entity<ReagentGrinderComponent> ent, ref ReagentGrinderStartMessage message)
    {
        StartGrinder(ent, message.Program);
    }

    private void OnToggleAutoModeMessage(Entity<ReagentGrinderComponent> ent, ref ReagentGrinderToggleAutoModeMessage message)
    {
        // Cycle through the enum values.
        ent.Comp.AutoMode = (GrinderAutoMode)(((byte)ent.Comp.AutoMode + 1) % Enum.GetValues<GrinderAutoMode>().Length);
        Dirty(ent);

        UpdateUi(ent);
    }

    private void OnEjectChamberAllMessage(Entity<ReagentGrinderComponent> ent, ref ReagentGrinderEjectChamberAllMessage message)
    {
        if (IsActive(ent.AsNullable()) || ent.Comp.InputContainer.ContainedEntities.Count <= 0)
            return;

        _audioSystem.PlayPredicted(ent.Comp.ClickSound, ent.Owner, message.Actor);
        _containerSystem.EmptyContainer(ent.Comp.InputContainer);
        // UpdateUi is called in the resulting ContainerModifiedMessage.
    }

    private void OnEjectChamberContentMessage(Entity<ReagentGrinderComponent> ent, ref ReagentGrinderEjectChamberContentMessage message)
    {
        if (IsActive(ent.AsNullable()))
            return;

        if (!TryGetEntity(message.EntityId, out var toRemove))
            return;

        if (_containerSystem.Remove(toRemove.Value, ent.Comp.InputContainer))
        {
            _audioSystem.PlayPredicted(ent.Comp.ClickSound, ent.Owner, message.Actor);
        }
        // UpdateUi is called in the resulting ContainerModifiedMessage.
    }

    /// <summary>
    /// The wzhzhzh of the grinder. Marks the grinder as active, but does not convert the items into reagents yet.
    /// </summary>
    private void StartGrinder(Entity<ReagentGrinderComponent> ent, GrinderProgram program)
    {
        if (IsActive(ent.AsNullable()))
            return;

        if (!_power.IsPowered(ent.Owner))
            return;

        var beaker = _itemSlotsSystem.GetItemOrNull(ent, ReagentGrinderComponent.BeakerSlotId);

        // Do we have anything to grind/juice and a container to put the reagents in?
        if (ent.Comp.InputContainer.ContainedEntities.Count <= 0 || !HasComp<FitsInDispenserComponent>(beaker))
            return;

        SoundSpecifier? sound;
        switch (program)
        {
            case GrinderProgram.Grind when ent.Comp.InputContainer.ContainedEntities.All(x => CanGrind(x)):
                sound = ent.Comp.GrindSound;
                break;
            case GrinderProgram.Juice when ent.Comp.InputContainer.ContainedEntities.All(x => CanJuice(x)):
                sound = ent.Comp.JuiceSound;
                break;
            default:
                return;
        }

        EnsureComp<ActiveReagentGrinderComponent>(ent);
        _jitter.AddJitter(ent, -10, 100);
        _powerState.TrySetWorkingState(ent.Owner, true); // Not all grinders need power.
        ent.Comp.Program = program;
        ent.Comp.EndTime = _timing.CurTime + ent.Comp.WorkTime * ent.Comp.WorkTimeMultiplier;
        Dirty(ent);
        UpdateUi(ent);

        if (_net.IsServer) // can't cancel predicted audio
            ent.Comp.AudioStream = _audioSystem.PlayPvs(sound, ent,
            AudioParams.Default.WithPitchScale(1 / ent.Comp.WorkTimeMultiplier))?.Entity; //slightly higher pitched
    }

    /// <summary>
    /// Converts items into reagents and marks the grinder as inactive.
    /// </summary>
    private void FinishGrinding(Entity<ReagentGrinderComponent> ent)
    {
        if (ent.Comp.Program is not { } program)
            return; // Already finished.

        ent.Comp.Program = null;
        ent.Comp.AudioStream = _audioSystem.Stop(ent.Comp.AudioStream);
        ent.Comp.EndTime = null; // It's important that we do this first or PredictedQueueDelete will fail to remove the entity from the container because the grinder is still active.
        Dirty(ent);
        // Remove deferred to avoid modifying the component we are currently enumerating over in the update loop.
        RemCompDeferred<ActiveReagentGrinderComponent>(ent);
        RemCompDeferred<JitteringComponent>(ent);
        _powerState.TrySetWorkingState(ent.Owner, false);

        var beaker = _itemSlotsSystem.GetItemOrNull(ent.Owner, ReagentGrinderComponent.BeakerSlotId);
        if (beaker is null || !_solutionContainersSystem.TryGetFitsInDispenser(beaker.Value, out var beakerSolutionEntity, out var beakerSolution))
            return;

        // Convert items into reagents.
        foreach (var item in ent.Comp.InputContainer.ContainedEntities.ToList())
        {
            var solution = GetGrinderSolution(item, program);

            if (solution is null)
                continue;

            // Delete the item or reduce its stack size.
            if (TryComp<StackComponent>(item, out var stack))
            {
                var totalVolume = solution.Volume * stack.Count;
                if (totalVolume <= 0)
                    continue;

                // Maximum number of items we can process in the stack without going over AvailableVolume
                // We add a small tolerance, because floats are inaccurate.
                var fitsCount = (int)(stack.Count * FixedPoint2.Min(beakerSolution.AvailableVolume / totalVolume + 0.01, 1));
                if (fitsCount <= 0)
                    continue;

                // Make a copy of the solution to scale
                // Otherwise we'll actually change the volume of the remaining stack too
                var scaledSolution = new Solution(solution);
                scaledSolution.ScaleSolution(fitsCount);
                solution = scaledSolution;

                _stackSystem.SetCount((item, stack), stack.Count - fitsCount); // Setting to 0 will QueueDel
            }
            else
            {
                if (solution.Volume > beakerSolution.AvailableVolume)
                    continue;

                _destructible.DestroyEntity(item);
            }
            _solutionContainersSystem.TryAddSolution(beakerSolutionEntity.Value, solution);
        }
        // UpdateUi is called when the entity in the grinder is deleted or the solution in the beaker is changed.
    }

    /// <summary>
    /// Is the given grinder currently grinding/juicing?
    /// </summary>
    public bool IsActive(Entity<ReagentGrinderComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return false;

        // Don't use ActiveGrinderComponent for this because it is being removed deferred, meaning it will get updated at the end of the tick.
        // ActiveReagentGrinderComponent is only for improving the EntityQueryEnumerator performance in the update loop.
        return ent.Comp.EndTime != null;
    }

    /// <summary>
    /// Gets the solutions from an entity using the specified Grinder program.
    /// </summary>
    /// <param name="ent">The entity which we check for solutions.</param>
    /// <param name="program">The grinder program.</param>
    /// <returns>The solution received, or null if none.</returns>
    public Solution? GetGrinderSolution(Entity<ExtractableComponent?> ent, GrinderProgram program)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return null;

        switch (program)
        {
            case GrinderProgram.Grind:
                if (_solutionContainersSystem.TryGetSolution(ent.Owner, ent.Comp.GrindableSolutionName, out _, out var solution))
                {
                    return solution;
                }
                break;
            case GrinderProgram.Juice:
                return ent.Comp.JuiceSolution;
        }

        return null;
    }

    /// <summary>
    /// Checks whether the entity can be ground using a ReagentGrinder.
    /// </summary>
    /// <param name="ent">The entity to check.</param>
    /// <returns>True if it can be ground, otherwise false.</returns>
    /// <remarks>
    /// Will it blend? That is the question!
    /// </remarks>
    public bool CanGrind(Entity<ExtractableComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return false;

        if (ent.Comp.GrindableSolutionName == null)
            return false;

        return _solutionContainersSystem.TryGetSolution(ent.Owner, ent.Comp.GrindableSolutionName, out _, out _);
    }

    /// <summary>
    /// Checks whether the entity can be juiced using a ReagentGrinder.
    /// </summary>
    /// <param name="ent">The entity to check.</param>
    /// <returns>True if it can be juiced, otherwise false.</returns>
    public bool CanJuice(Entity<ExtractableComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return false;

        return ent.Comp.JuiceSolution is not null;
    }
}
