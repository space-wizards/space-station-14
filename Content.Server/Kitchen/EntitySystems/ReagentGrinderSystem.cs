using Content.Server.Kitchen.Components;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Stack;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Destructible;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Kitchen;
using Content.Shared.Kitchen.Components;
using Content.Shared.Popups;
using Content.Shared.Random;
using Content.Shared.Stacks;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Timing;
using System.Linq;
using Content.Server.Jittering;
using Content.Shared.Jittering;
using Content.Shared.Power;

namespace Content.Server.Kitchen.EntitySystems
{
    [UsedImplicitly]
    internal sealed class ReagentGrinderSystem : EntitySystem
    {
        [Dependency] private readonly IGameTiming _timing = default!;
        [Dependency] private readonly SharedSolutionContainerSystem _solutionContainersSystem = default!;
        [Dependency] private readonly ItemSlotsSystem _itemSlotsSystem = default!;
        [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
        [Dependency] private readonly UserInterfaceSystem _userInterfaceSystem = default!;
        [Dependency] private readonly StackSystem _stackSystem = default!;
        [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;
        [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
        [Dependency] private readonly RandomHelperSystem _randomHelper = default!;
        [Dependency] private readonly JitteringSystem _jitter = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ActiveReagentGrinderComponent, ComponentStartup>(OnActiveGrinderStart);
            SubscribeLocalEvent<ActiveReagentGrinderComponent, ComponentRemove>(OnActiveGrinderRemove);
            SubscribeLocalEvent<ReagentGrinderComponent, ComponentStartup>((uid, _, _) => UpdateUiState(uid));
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

        private void OnToggleAutoModeMessage(Entity<ReagentGrinderComponent> entity, ref ReagentGrinderToggleAutoModeMessage message)
        {
            entity.Comp.AutoMode = (GrinderAutoMode) (((byte) entity.Comp.AutoMode + 1) % Enum.GetValues(typeof(GrinderAutoMode)).Length);

            UpdateUiState(entity);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            var query = EntityQueryEnumerator<ActiveReagentGrinderComponent, ReagentGrinderComponent>();
            while (query.MoveNext(out var uid, out var active, out var reagentGrinder))
            {
                if (active.EndTime > _timing.CurTime)
                    continue;

                reagentGrinder.AudioStream = _audioSystem.Stop(reagentGrinder.AudioStream);
                RemCompDeferred<ActiveReagentGrinderComponent>(uid);

                var inputContainer = _containerSystem.EnsureContainer<Container>(uid, SharedReagentGrinder.InputContainerId);
                var outputContainer = _itemSlotsSystem.GetItemOrNull(uid, SharedReagentGrinder.BeakerSlotId);
                if (outputContainer is null || !_solutionContainersSystem.TryGetFitsInDispenser(outputContainer.Value, out var containerSoln, out var containerSolution))
                    continue;

                foreach (var item in inputContainer.ContainedEntities.ToList())
                {
                    var solution = active.Program switch
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
                        var fitsCount = (int) (stack.Count * FixedPoint2.Min(containerSolution.AvailableVolume / totalVolume + 0.01, 1));
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
                        if (solution.Volume > containerSolution.AvailableVolume)
                            continue;

                        var dev = new DestructionEventArgs();
                        RaiseLocalEvent(item, dev);

                        QueueDel(item);
                    }

                    _solutionContainersSystem.TryAddSolution(containerSoln.Value, solution);
                }

                _userInterfaceSystem.ServerSendUiMessage(uid, ReagentGrinderUiKey.Key,
                    new ReagentGrinderWorkCompleteMessage());

                UpdateUiState(uid);
            }
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
            UpdateUiState(uid);

            var outputContainer = _itemSlotsSystem.GetItemOrNull(uid, SharedReagentGrinder.BeakerSlotId);
            _appearanceSystem.SetData(uid, ReagentGrinderVisualState.BeakerAttached, outputContainer.HasValue);

            if (reagentGrinder.AutoMode != GrinderAutoMode.Off && !HasComp<ActiveReagentGrinderComponent>(uid) && this.IsPowered(uid, EntityManager))
            {
                var program = reagentGrinder.AutoMode == GrinderAutoMode.Grind ? GrinderProgram.Grind : GrinderProgram.Juice;
                DoWork(uid, reagentGrinder, program);
            }
        }

        private void OnInteractUsing(Entity<ReagentGrinderComponent> entity, ref InteractUsingEvent args)
        {
            var heldEnt = args.Used;
            var inputContainer = _containerSystem.EnsureContainer<Container>(entity.Owner, SharedReagentGrinder.InputContainerId);

            if (!HasComp<ExtractableComponent>(heldEnt))
            {
                if (!HasComp<FitsInDispenserComponent>(heldEnt))
                {
                    // This is ugly but we can't use whitelistFailPopup because there are 2 containers with different whitelists.
                    _popupSystem.PopupEntity(Loc.GetString("reagent-grinder-component-cannot-put-entity-message"), entity.Owner, args.User);
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
            if (inputContainer.ContainedEntities.Count >= entity.Comp.StorageMaxEntities)
                return;

            if (!_containerSystem.Insert(heldEnt, inputContainer))
                return;

            args.Handled = true;
        }

        private void UpdateUiState(EntityUid uid)
        {
            ReagentGrinderComponent? grinderComp = null;
            if (!Resolve(uid, ref grinderComp))
                return;

            var inputContainer = _containerSystem.EnsureContainer<Container>(uid, SharedReagentGrinder.InputContainerId);
            var outputContainer = _itemSlotsSystem.GetItemOrNull(uid, SharedReagentGrinder.BeakerSlotId);
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
                this.IsPowered(uid, EntityManager),
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
            if (!this.IsPowered(entity.Owner, EntityManager) || HasComp<ActiveReagentGrinderComponent>(entity))
                return;

            DoWork(entity.Owner, entity.Comp, message.Program);
        }

        private void OnEjectChamberAllMessage(Entity<ReagentGrinderComponent> entity, ref ReagentGrinderEjectChamberAllMessage message)
        {
            var inputContainer = _containerSystem.EnsureContainer<Container>(entity.Owner, SharedReagentGrinder.InputContainerId);

            if (HasComp<ActiveReagentGrinderComponent>(entity) || inputContainer.ContainedEntities.Count <= 0)
                return;

            ClickSound(entity);
            foreach (var toEject in inputContainer.ContainedEntities.ToList())
            {
                _containerSystem.Remove(toEject, inputContainer);
                _randomHelper.RandomOffset(toEject, 0.4f);
            }
            UpdateUiState(entity);
        }

        private void OnEjectChamberContentMessage(Entity<ReagentGrinderComponent> entity, ref ReagentGrinderEjectChamberContentMessage message)
        {
            if (HasComp<ActiveReagentGrinderComponent>(entity))
                return;

            var inputContainer = _containerSystem.EnsureContainer<Container>(entity.Owner, SharedReagentGrinder.InputContainerId);
            var ent = GetEntity(message.EntityId);

            if (_containerSystem.Remove(ent, inputContainer))
            {
                _randomHelper.RandomOffset(ent, 0.4f);
                ClickSound(entity);
                UpdateUiState(entity);
            }
        }

        /// <summary>
        /// The wzhzhzh of the grinder. Processes the contents of the grinder and puts the output in the beaker.
        /// </summary>
        /// <param name="uid">The grinder itself</param>
        /// <param name="reagentGrinder"></param>
        /// <param name="program">Which program, such as grind or juice</param>
        private void DoWork(EntityUid uid, ReagentGrinderComponent reagentGrinder, GrinderProgram program)
        {
            var inputContainer = _containerSystem.EnsureContainer<Container>(uid, SharedReagentGrinder.InputContainerId);
            var outputContainer = _itemSlotsSystem.GetItemOrNull(uid, SharedReagentGrinder.BeakerSlotId);

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

            var active = AddComp<ActiveReagentGrinderComponent>(uid);
            active.EndTime = _timing.CurTime + reagentGrinder.WorkTime * reagentGrinder.WorkTimeMultiplier;
            active.Program = program;

            reagentGrinder.AudioStream = _audioSystem.PlayPvs(sound, uid,
                AudioParams.Default.WithPitchScale(1 / reagentGrinder.WorkTimeMultiplier))?.Entity; //slightly higher pitched
            _userInterfaceSystem.ServerSendUiMessage(uid, ReagentGrinderUiKey.Key,
                new ReagentGrinderWorkStartedMessage(program));
        }

        private void ClickSound(Entity<ReagentGrinderComponent> reagentGrinder)
        {
            _audioSystem.PlayPvs(reagentGrinder.Comp.ClickSound, reagentGrinder.Owner, AudioParams.Default.WithVolume(-2f));
        }

        private Solution? GetGrindSolution(EntityUid uid)
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

        private bool CanGrind(EntityUid uid)
        {
            var solutionName = CompOrNull<ExtractableComponent>(uid)?.GrindableSolution;

            return solutionName is not null && _solutionContainersSystem.TryGetSolution(uid, solutionName, out _, out _);
        }

        private bool CanJuice(EntityUid uid)
        {
            return CompOrNull<ExtractableComponent>(uid)?.JuiceSolution is not null;
        }
    }
}
