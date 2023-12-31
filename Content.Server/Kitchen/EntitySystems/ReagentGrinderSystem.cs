using Content.Server.Chemistry.Containers.EntitySystems;
using Content.Server.Construction;
using Content.Server.Kitchen.Components;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Stack;
using Content.Shared.Chemistry.Components;
using Content.Shared.Containers.ItemSlots;
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

namespace Content.Server.Kitchen.EntitySystems
{
    [UsedImplicitly]
    internal sealed class ReagentGrinderSystem : EntitySystem
    {
        [Dependency] private readonly IGameTiming _timing = default!;
        [Dependency] private readonly SolutionContainerSystem _solutionContainersSystem = default!;
        [Dependency] private readonly ItemSlotsSystem _itemSlotsSystem = default!;
        [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
        [Dependency] private readonly UserInterfaceSystem _userInterfaceSystem = default!;
        [Dependency] private readonly StackSystem _stackSystem = default!;
        [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;
        [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
        [Dependency] private readonly RandomHelperSystem _randomHelper = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ReagentGrinderComponent, ComponentStartup>((uid, _, _) => UpdateUiState(uid));
            SubscribeLocalEvent((EntityUid uid, ReagentGrinderComponent _, ref PowerChangedEvent _) => UpdateUiState(uid));
            SubscribeLocalEvent<ReagentGrinderComponent, InteractUsingEvent>(OnInteractUsing);
            SubscribeLocalEvent<ReagentGrinderComponent, RefreshPartsEvent>(OnRefreshParts);
            SubscribeLocalEvent<ReagentGrinderComponent, UpgradeExamineEvent>(OnUpgradeExamine);

            SubscribeLocalEvent<ReagentGrinderComponent, EntInsertedIntoContainerMessage>(OnContainerModified);
            SubscribeLocalEvent<ReagentGrinderComponent, EntRemovedFromContainerMessage>(OnContainerModified);
            SubscribeLocalEvent<ReagentGrinderComponent, ContainerIsRemovingAttemptEvent>(OnEntRemoveAttempt);

            SubscribeLocalEvent<ReagentGrinderComponent, ReagentGrinderStartMessage>(OnStartMessage);
            SubscribeLocalEvent<ReagentGrinderComponent, ReagentGrinderEjectChamberAllMessage>(OnEjectChamberAllMessage);
            SubscribeLocalEvent<ReagentGrinderComponent, ReagentGrinderEjectChamberContentMessage>(OnEjectChamberContentMessage);
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

                        solution.ScaleSolution(fitsCount);
                        _stackSystem.SetCount(item, stack.Count - fitsCount); // Setting to 0 will QueueDel
                    }
                    else
                    {
                        if (solution.Volume > containerSolution.AvailableVolume)
                            continue;

                        QueueDel(item);
                    }

                    _solutionContainersSystem.TryAddSolution(containerSoln.Value, solution);
                }

                _userInterfaceSystem.TrySendUiMessage(uid, ReagentGrinderUiKey.Key,
                    new ReagentGrinderWorkCompleteMessage());

                UpdateUiState(uid);
            }
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

        /// <remarks>
        /// Gotta be efficient, you know? you're saving a whole extra second here and everything.
        /// </remarks>
        private void OnRefreshParts(Entity<ReagentGrinderComponent> entity, ref RefreshPartsEvent args)
        {
            var ratingWorkTime = args.PartRatings[entity.Comp.MachinePartWorkTime];
            var ratingStorage = args.PartRatings[entity.Comp.MachinePartStorageMax];

            entity.Comp.WorkTimeMultiplier = MathF.Pow(entity.Comp.PartRatingWorkTimerMulitplier, ratingWorkTime - 1);
            entity.Comp.StorageMaxEntities = entity.Comp.BaseStorageMaxEntities + (int) (entity.Comp.StoragePerPartRating * (ratingStorage - 1));
        }

        private void OnUpgradeExamine(Entity<ReagentGrinderComponent> entity, ref UpgradeExamineEvent args)
        {
            args.AddPercentageUpgrade("reagent-grinder-component-upgrade-work-time", entity.Comp.WorkTimeMultiplier);
            args.AddNumberUpgrade("reagent-grinder-component-upgrade-storage", entity.Comp.StorageMaxEntities - entity.Comp.BaseStorageMaxEntities);
        }

        private void UpdateUiState(EntityUid uid)
        {
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
                GetNetEntityArray(inputContainer.ContainedEntities.ToArray()),
                containerSolution?.Contents.ToArray()
            );
            _userInterfaceSystem.TrySetUiState(uid, ReagentGrinderUiKey.Key, state);
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
                AudioParams.Default.WithPitchScale(1 / reagentGrinder.WorkTimeMultiplier)).Value.Entity; //slightly higher pitched
            _userInterfaceSystem.TrySendUiMessage(uid, ReagentGrinderUiKey.Key,
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
