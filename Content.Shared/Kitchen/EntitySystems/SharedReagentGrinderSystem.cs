using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Destructible;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Kitchen.Components;
using Content.Shared.Popups;
using Content.Shared.Random;
using Content.Shared.Stacks;
using JetBrains.Annotations;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Timing;
using System.Linq;
using Content.Shared.Fluids;
using Content.Shared.Jittering;
using Content.Shared.Power;
using Content.Shared.Power.EntitySystems;
using Robust.Shared.Network;

namespace Content.Shared.Kitchen.EntitySystems;

[UsedImplicitly]
public abstract class SharedReagentGrinderSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
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
    [Dependency] private readonly INetManager _net = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ActiveReagentGrinderComponent, ComponentStartup>(OnActiveGrinderStart);
        SubscribeLocalEvent<ActiveReagentGrinderComponent, ComponentRemove>(OnActiveGrinderRemove);
        SubscribeLocalEvent<ReagentGrinderComponent, ComponentStartup>(OnGrinderStartup);
        SubscribeLocalEvent((EntityUid uid, ReagentGrinderComponent _, ref PowerChangedEvent _) => UpdateUiState(uid));
        SubscribeLocalEvent<ReagentGrinderComponent, InteractUsingEvent>(OnInteractUsing);

        SubscribeLocalEvent<ReagentGrinderComponent, EntInsertedIntoContainerMessage>(OnContainerModified);
        SubscribeLocalEvent<ReagentGrinderComponent, EntRemovedFromContainerMessage>(OnContainerModified);
        SubscribeLocalEvent<ReagentGrinderComponent, ContainerIsRemovingAttemptEvent>(OnEntRemoveAttempt);

        SubscribeLocalEvent<ReagentGrinderComponent, ReagentGrinderToggleAutoModeMessage>(OnToggleAutoModeMessage);
        SubscribeLocalEvent<ReagentGrinderComponent, ReagentGrinderStartMessage>(OnStartMessage);
        SubscribeLocalEvent<ReagentGrinderComponent, ReagentGrinderEjectChamberAllMessage>(OnEjectChamberAllMessage);
        SubscribeLocalEvent<ReagentGrinderComponent, ReagentGrinderEjectChamberContentMessage>(OnEjectChamberContentMessage);
    }

    private void OnToggleAutoModeMessage(Entity<ReagentGrinderComponent> ent, ref ReagentGrinderToggleAutoModeMessage message)
    {
        ent.Comp.AutoMode = (GrinderAutoMode) (((byte) ent.Comp.AutoMode + 1) % Enum.GetValues(typeof(GrinderAutoMode)).Length);
        Dirty(ent);

        UpdateUiState(ent);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ActiveReagentGrinderComponent, ReagentGrinderComponent>();
        while (query.MoveNext(out var uid, out var active, out var reagentGrinder))
        {
            if (reagentGrinder.EndTime > _timing.CurTime)
                continue;

            FinishGrinding((uid, reagentGrinder));
        }
    }

    private void OnGrinderStartup(Entity<ReagentGrinderComponent> ent, ref ComponentStartup args)
    {
        ent.Comp.InputContainer = _containerSystem.EnsureContainer<Container>(ent.Owner, ReagentGrinderComponent.InputContainerId);

        // We update the appearance here in case the reagent grinder already starts with a beaker.
        var beaker = _itemSlotsSystem.GetItemOrNull(ent, ReagentGrinderComponent.BeakerSlotId);
        _appearanceSystem.SetData(ent, ReagentGrinderVisualState.BeakerAttached, beaker.HasValue);
    }

    private void OnActiveGrinderStart(Entity<ActiveReagentGrinderComponent> ent, ref ComponentStartup args)
    {
        _jitter.AddJitter(ent, -10, 100);
    }

    private void OnActiveGrinderRemove(Entity<ActiveReagentGrinderComponent> ent, ref ComponentRemove args)
    {
        RemComp<JitteringComponent>(ent);
    }

    private void OnEntRemoveAttempt(Entity<ReagentGrinderComponent> entity, ref ContainerIsRemovingAttemptEvent args)
    {
        if (HasComp<ActiveReagentGrinderComponent>(entity))
            args.Cancel();
    }

    private void OnContainerModified(EntityUid uid, ReagentGrinderComponent reagentGrinder, ContainerModifiedMessage args)
    {
        if (_timing.ApplyingState)
            return;

        if (args.Container.ID != ReagentGrinderComponent.BeakerSlotId
            && args.Container.ID != ReagentGrinderComponent.InputContainerId)
            return;

        UpdateUiState(uid);

        var beaker = _itemSlotsSystem.GetItemOrNull(uid, ReagentGrinderComponent.BeakerSlotId);
        _appearanceSystem.SetData(uid, ReagentGrinderVisualState.BeakerAttached, beaker.HasValue);

        if (reagentGrinder.AutoMode != GrinderAutoMode.Off && !HasComp<ActiveReagentGrinderComponent>(uid) && _power.IsPowered(uid))
        {
            var program = reagentGrinder.AutoMode == GrinderAutoMode.Grind ? GrinderProgram.Grind : GrinderProgram.Juice;
            StartGrinder(uid, reagentGrinder, program);
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

    private void UpdateUiState(EntityUid uid)
    {
        ReagentGrinderComponent? grinderComp = null;
        if (!Resolve(uid, ref grinderComp))
            return;

        // While we have the cached reference in ReagentGrinderComponent, we have to EnsureContainer here again.
        // This is because UpdateUiState is ran before the component can properly initialize, causing a null reference exception.
        // TODO: Fix this when getting rid of BUI states.
        var inputContainer = _containerSystem.EnsureContainer<Container>(uid, ReagentGrinderComponent.InputContainerId);
        var outputContainer = _itemSlotsSystem.GetItemOrNull(uid, ReagentGrinderComponent.BeakerSlotId);
        Solution? containerSolution = null;
        var isBusy = HasComp<ActiveReagentGrinderComponent>(uid);
        var canJuice = false;
        var canGrind = false;

        if (outputContainer is not null
            && _solutionContainersSystem.TryGetFitsInDispenser(outputContainer.Value, out _, out containerSolution)
            && inputContainer.ContainedEntities.Count > 0)
        {
            canGrind = inputContainer.ContainedEntities.All(CanGrind);
            canJuice = inputContainer.ContainedEntities.All(CanJuice);
        }

        var state = new ReagentGrinderInterfaceState(
            isBusy,
            outputContainer.HasValue,
            _power.IsPowered(uid),
            canJuice,
            canGrind,
            grinderComp.AutoMode,
            GetNetEntityArray(inputContainer.ContainedEntities.ToArray()),
            containerSolution?.Contents.ToArray()
        );
        _userInterfaceSystem.SetUiState(uid, ReagentGrinderUiKey.Key, state);
    }

    private void OnStartMessage(Entity<ReagentGrinderComponent> entity, ref ReagentGrinderStartMessage message)
    {
        if (!_power.IsPowered(entity.Owner) || HasComp<ActiveReagentGrinderComponent>(entity))
            return;

        StartGrinder(entity.Owner, entity.Comp, message.Program);
    }

    private void OnEjectChamberAllMessage(Entity<ReagentGrinderComponent> ent, ref ReagentGrinderEjectChamberAllMessage message)
    {
        var inputContainer = _containerSystem.EnsureContainer<Container>(ent.Owner, ReagentGrinderComponent.InputContainerId);

        if (HasComp<ActiveReagentGrinderComponent>(ent) || inputContainer.ContainedEntities.Count <= 0)
            return;

        _audioSystem.PlayPvs(ent.Comp.ClickSound, ent.Owner);

        foreach (var toEject in inputContainer.ContainedEntities.ToList())
        {
            _containerSystem.Remove(toEject, inputContainer);
            _randomHelper.RandomOffset(toEject, 0.4f);
        }
        UpdateUiState(ent);
    }

    private void OnEjectChamberContentMessage(Entity<ReagentGrinderComponent> ent, ref ReagentGrinderEjectChamberContentMessage message)
    {
        if (HasComp<ActiveReagentGrinderComponent>(ent))
            return;

        var inputContainer = _containerSystem.EnsureContainer<Container>(ent.Owner, ReagentGrinderComponent.InputContainerId);
        var entity = GetEntity(message.EntityId);

        if (_containerSystem.Remove(entity, inputContainer))
        {
            _randomHelper.RandomOffset(ent, 0.4f);
            _audioSystem.PlayPvs(ent.Comp.ClickSound, ent.Owner);

            UpdateUiState(ent);
        }
    }

    /// <summary>
    /// The wzhzhzh of the grinder. Processes the contents of the grinder and puts the output in the beaker.
    /// </summary>
    /// <param name="uid">The grinder itself</param>
    /// <param name="reagentGrinder"></param>
    /// <param name="program">Which program, such as grind or juice</param>
    private void StartGrinder(EntityUid uid, ReagentGrinderComponent reagentGrinder, GrinderProgram program)
    {
        var inputContainer = _containerSystem.EnsureContainer<Container>(uid, ReagentGrinderComponent.InputContainerId);
        var outputContainer = _itemSlotsSystem.GetItemOrNull(uid, ReagentGrinderComponent.BeakerSlotId);

        // Do we have anything to grind/juice and a container to put the reagents in?
        if (inputContainer.ContainedEntities.Count <= 0 || !HasComp<FitsInDispenserComponent>(outputContainer))
            return;

        SoundSpecifier? sound;
        switch (program)
        {
            case GrinderProgram.Grind when inputContainer.ContainedEntities.All(CanGrind):
                sound = reagentGrinder.GrindSound;
                break;
            case GrinderProgram.Juice when inputContainer.ContainedEntities.All(CanJuice):
                sound = reagentGrinder.JuiceSound;
                break;
            default:
                return;
        }

        EnsureComp<ActiveReagentGrinderComponent>(uid);
        reagentGrinder.EndTime = _timing.CurTime + reagentGrinder.WorkTime * reagentGrinder.WorkTimeMultiplier;
        reagentGrinder.Program = program;

        reagentGrinder.AudioStream = _audioSystem.PlayPvs(sound, uid,
            AudioParams.Default.WithPitchScale(1 / reagentGrinder.WorkTimeMultiplier))?.Entity; //slightly higher pitched
        if (_net.IsServer)
            _userInterfaceSystem.ServerSendUiMessage(uid, ReagentGrinderUiKey.Key, new ReagentGrinderWorkStartedMessage(program));

        Dirty(uid, reagentGrinder);
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
            var solution = program switch
            {
                GrinderProgram.Grind => GetGrindSolution(item),
                GrinderProgram.Juice => CompOrNull<ExtractableComponent>(item)?.JuiceSolution,
                _ => null,
            };

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

        if (_net.IsServer)
            _userInterfaceSystem.ServerSendUiMessage(ent.Owner, ReagentGrinderUiKey.Key, new ReagentGrinderWorkCompleteMessage());

        UpdateUiState(ent);
    }

    public Solution? GetGrindSolution(EntityUid uid)
    {
        if (TryComp<ExtractableComponent>(uid, out var extractable)
            && extractable.GrindableSolution is not null
            && _solutionContainersSystem.TryGetSolution(uid, extractable.GrindableSolution, out _, out var solution))
        {
            return solution;
        }
        else
            return null;
    }

    public bool CanGrind(EntityUid uid)
    {
        var solutionName = CompOrNull<ExtractableComponent>(uid)?.GrindableSolution;

        return solutionName is not null && _solutionContainersSystem.TryGetSolution(uid, solutionName, out _, out _);
    }

    public bool CanJuice(EntityUid uid)
    {
        return CompOrNull<ExtractableComponent>(uid)?.JuiceSolution is not null;
    }
}

