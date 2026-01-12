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
using Content.Shared.Random;
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
    [Dependency] private readonly SharedUserInterfaceSystem _userInterfaceSystem = default!;
    [Dependency] private readonly SharedStackSystem _stackSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly SharedDestructibleSystem _destructible = default!;
    [Dependency] private readonly RandomHelperSystem _randomHelper = default!;
    [Dependency] private readonly SharedJitteringSystem _jitter = default!;
    [Dependency] private readonly SharedPowerReceiverSystem _power = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ActiveReagentGrinderComponent, ComponentStartup>(OnActiveGrinderStartup);
        SubscribeLocalEvent<ActiveReagentGrinderComponent, ComponentRemove>(OnActiveGrinderRemove);
        SubscribeLocalEvent<ActiveReagentGrinderComponent, ContainerIsRemovingAttemptEvent>(OnEntRemovingAttempt);

        SubscribeLocalEvent<ReagentGrinderComponent, ComponentStartup>(OnGrinderStartup);
        SubscribeLocalEvent((EntityUid uid, ReagentGrinderComponent _, ref PowerChangedEvent _) => UpdateUi(uid));
        SubscribeLocalEvent<ReagentGrinderComponent, InteractUsingEvent>(OnInteractUsing);

        SubscribeLocalEvent<ReagentGrinderComponent, EntInsertedIntoContainerMessage>(OnContainerModified);
        SubscribeLocalEvent<ReagentGrinderComponent, EntRemovedFromContainerMessage>(OnContainerModified);

        SubscribeLocalEvent<ReagentGrinderComponent, ReagentGrinderStartMessage>(OnStartMessage);
        SubscribeLocalEvent<ReagentGrinderComponent, ReagentGrinderToggleAutoModeMessage>(OnToggleAutoModeMessage);
        SubscribeLocalEvent<ReagentGrinderComponent, ReagentGrinderEjectChamberAllMessage>(OnEjectChamberAllMessage);
        SubscribeLocalEvent<ReagentGrinderComponent, ReagentGrinderEjectChamberContentMessage>(OnEjectChamberContentMessage);
    }

    private void OnActiveGrinderStartup(Entity<ActiveReagentGrinderComponent> ent, ref ComponentStartup args)
    {
        _jitter.AddJitter(ent, -10, 100);
    }

    private void OnActiveGrinderRemove(Entity<ActiveReagentGrinderComponent> ent, ref ComponentRemove args)
    {
        RemComp<JitteringComponent>(ent);
    }

    private void OnEntRemovingAttempt(Entity<ActiveReagentGrinderComponent> entity, ref ContainerIsRemovingAttemptEvent args)
    {
        // Allow server states to be applied.
        if (!_timing.ApplyingState)
            return;

        if (args.Container.ID == ReagentGrinderComponent.BeakerSlotId
            || args.Container.ID == ReagentGrinderComponent.InputContainerId)
            args.Cancel();
    }

    private void OnGrinderStartup(Entity<ReagentGrinderComponent> ent, ref ComponentStartup args)
    {
        ent.Comp.InputContainer = _containerSystem.EnsureContainer<Container>(ent.Owner, ReagentGrinderComponent.InputContainerId);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _timing.CurTime;
        var query = EntityQueryEnumerator<ActiveReagentGrinderComponent, ReagentGrinderComponent>();
        while (query.MoveNext(out var uid, out _, out var grinderComp))
        {
            if (grinderComp.EndTime > curTime)
                continue;

            FinishGrinding((uid, grinderComp));
        }
    }

    private void OnContainerModified(EntityUid uid, ReagentGrinderComponent comp, ContainerModifiedMessage args)
    {
        if (args.Container.ID != ReagentGrinderComponent.BeakerSlotId
            && args.Container.ID != ReagentGrinderComponent.InputContainerId)
            return;

        UpdateUi(uid);

        var beaker = _itemSlotsSystem.GetItemOrNull(uid, ReagentGrinderComponent.BeakerSlotId);
        _appearanceSystem.SetData(uid, ReagentGrinderVisualState.BeakerAttached, beaker.HasValue);

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
        ent.Comp.AutoMode = (GrinderAutoMode)(((byte)ent.Comp.AutoMode + 1) % Enum.GetValues<GrinderAutoMode>().Length);
        Dirty(ent);

        UpdateUi(ent);
    }

    private void OnEjectChamberAllMessage(Entity<ReagentGrinderComponent> ent, ref ReagentGrinderEjectChamberAllMessage message)
    {
        if (HasComp<ActiveReagentGrinderComponent>(ent) || ent.Comp.InputContainer.ContainedEntities.Count <= 0)
            return;

        _audioSystem.PlayPredicted(ent.Comp.ClickSound, ent.Owner, message.Actor);
        _containerSystem.EmptyContainer(ent.Comp.InputContainer);
    }

    private void OnEjectChamberContentMessage(Entity<ReagentGrinderComponent> ent, ref ReagentGrinderEjectChamberContentMessage message)
    {
        if (HasComp<ActiveReagentGrinderComponent>(ent))
            return;

        if (!TryGetEntity(message.EntityId, out var toRemove))
            return;

        if (_containerSystem.Remove(toRemove.Value, ent.Comp.InputContainer))
        {
            _audioSystem.PlayPredicted(ent.Comp.ClickSound, ent.Owner, message.Actor);
        }
    }

    /// <summary>
    /// The wzhzhzh of the grinder. Marks the grinder as active, but does not convert the items into reagents yet.
    /// </summary>
    private void StartGrinder(Entity<ReagentGrinderComponent> ent, GrinderProgram program)
    {
        if (HasComp<ActiveReagentGrinderComponent>(ent))
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

        AddComp<ActiveReagentGrinderComponent>(ent);
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
        ent.Comp.AudioStream = _audioSystem.Stop(ent.Comp.AudioStream);
        var program = ent.Comp.Program;
        ent.Comp.Program = null;
        ent.Comp.EndTime = null;
        Dirty(ent);
        RemCompDeferred<ActiveReagentGrinderComponent>(ent);

        var beaker = _itemSlotsSystem.GetItemOrNull(ent.Owner, ReagentGrinderComponent.BeakerSlotId);
        if (beaker is null || !_solutionContainersSystem.TryGetFitsInDispenser(beaker.Value, out var beakerSolutionEntity, out var beakerSolution))
            return;

        foreach (var item in ent.Comp.InputContainer.ContainedEntities.ToList())
        {
            var solution = GetGrinderSolution(item, active.Program);

            if (solution is null)
                continue;

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

                _stackSystem.SetCount(item, stack.Count - fitsCount); // Setting to 0 will QueueDel
            }
            else
            {
                if (solution.Volume > beakerSolution.AvailableVolume)
                    continue;

                _destructible.DestroyEntity(item);
            }

            _solutionContainersSystem.TryAddSolution(beakerSolutionEntity.Value, solution);
        }
        UpdateUi(ent);
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
                if (_solutionContainersSystem.TryGetSolution(ent.Owner, ent.Comp.GrindableSolution, out _, out var solution))
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
    public bool CanGrind(Entity<ExtractableComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return false;

        if (ent.Comp.GrindableSolution == null)
            return false;

        return _solutionContainersSystem.TryGetSolution(ent.Owner, ent.Comp.GrindableSolution, out _, out _);
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
