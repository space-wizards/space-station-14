using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Administration;
using Content.Server.Chemistry.Components;
using Content.Server.Chemistry.Components.SolutionManager;
using Content.Server.Popups;
using Content.Server.Storage.Components;
using Content.Server.Storage.EntitySystems;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.FixedPoint;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Robust.Shared.Utility;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Robust.Shared.Prototypes;

namespace Content.Server.Chemistry.EntitySystems
{

    /// <summary>
    /// Contains all the server-side logic for Centrifuges.
    /// <seealso cref="CentrifugeComponent"/>
    /// </summary>
    [UsedImplicitly]
    public sealed class CentrifugeSystem : EntitySystem
    {
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly AudioSystem _audioSystem = default!;
        [Dependency] private readonly SolutionContainerSystem _solutionContainerSystem = default!;
        [Dependency] private readonly ItemSlotsSystem _itemSlotsSystem = default!;
        [Dependency] private readonly UserInterfaceSystem _userInterfaceSystem = default!;
        [Dependency] private readonly StorageSystem _storageSystem = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IGamePrototypeLoadManager _gamePrototypeLoadManager = default!;

        private Queue<CentrifugeComponent> _uiUpdateQueue = new();

        /// <summary>
        ///     A cache of all existant chemical reactions indexed by their resulting reagent.
        /// </summary>
        private IDictionary<string, List<ReactionPrototype>> _reactions = default!;

        public override void Initialize()
        {
            base.Initialize();

            InitializeReactionCache();

            _prototypeManager.PrototypesReloaded += OnPrototypesReloaded;

            SubscribeLocalEvent<CentrifugeComponent, ComponentStartup>((_, comp, _) => UpdateUiState(comp));
            SubscribeLocalEvent<CentrifugeComponent, SolutionChangedEvent>((_, comp, _) => UpdateUiState(comp));
            SubscribeLocalEvent<CentrifugeComponent, EntInsertedIntoContainerMessage>((_, comp, _) => UpdateUiState(comp));
            SubscribeLocalEvent<CentrifugeComponent, EntRemovedFromContainerMessage>((_, comp, _) => UpdateUiState(comp));

            SubscribeLocalEvent<CentrifugeComponent, PowerChangedEvent>(OnPowerChange);
            SubscribeLocalEvent<CentrifugeComponent, ContainerIsRemovingAttemptEvent>(OnEntRemoveAttempt);

            SubscribeLocalEvent<CentrifugeComponent, BoundUIOpenedEvent>((_, comp, _) => UpdateUiState(comp));

            SubscribeLocalEvent<CentrifugeComponent, CentrifugeSetModeMessage>(OnSetModeMessage);
            SubscribeLocalEvent<CentrifugeComponent, CentrifugeReagentAmountButtonMessage>(OnReagentButtonMessage);

            SubscribeLocalEvent<CentrifugeComponent, CentrifugeActivateButtonMessage>(OnActivateButtonMessage);
            SubscribeLocalEvent<CentrifugeComponent, CentrifugeElectrolysisButtonMessage>(OnElectrolysisButtonMessage);

            SubscribeLocalEvent<CentrifugeComponent, MapInitEvent>(OnMapInit);
        }

        /// <summary>
        ///     Handles building the reaction cache.
        /// </summary>
        private void InitializeReactionCache()
        {
            _reactions = new Dictionary<string, List<ReactionPrototype>>();

            var reactions = _prototypeManager.EnumeratePrototypes<ReactionPrototype>();
            foreach (var products in reactions)
            {
                CacheReaction(products);
            }
        }

        private void OnPrototypesReloaded(PrototypesReloadedEventArgs eventArgs)
        {
            if (!eventArgs.ByType.TryGetValue(typeof(ReactionPrototype), out var set))
                return;

            foreach (var (reactant, cache) in _reactions)
            {
                cache.RemoveAll((reaction) => set.Modified.ContainsKey(reaction.ID));
                if (cache.Count == 0)
                    _reactions.Remove(reactant);
            }

            foreach (var prototype in set.Modified.Values)
            {
                CacheReaction((ReactionPrototype)prototype);
            }
        }

        private void CacheReaction(ReactionPrototype reaction)
        {
            var reagents = reaction.Products.Keys;
            foreach (var reagent in reagents)
            {
                if (!_reactions.TryGetValue(reagent, out var cache))
                {
                    cache = new List<ReactionPrototype>();
                    _reactions.Add(reagent, cache);
                }

                cache.Add(reaction);
                return; // Only need to cache based on the first reagent.
            }
        }

        private void OnPowerChange(EntityUid uid, CentrifugeComponent component, ref PowerChangedEvent args)
        {
            EnqueueUiUpdate(component);
        }

        private void OnEntRemoveAttempt(EntityUid uid, CentrifugeComponent component, ContainerIsRemovingAttemptEvent args)
        {
            if (component.Busy)
                args.Cancel();
        }

        private void EnqueueUiUpdate(CentrifugeComponent component)
        {
            if (!_uiUpdateQueue.Contains(component)) _uiUpdateQueue.Enqueue(component);
        }

        private void UpdateUiState(CentrifugeComponent Centrifuge)
        {
            if (Centrifuge.Busy)
                return;

            if (!_solutionContainerSystem.TryGetSolution(Centrifuge.Owner, SharedCentrifuge.BufferSolutionName, out var bufferSolution))
                return;

            var inputContainer = _itemSlotsSystem.GetItemOrNull(Centrifuge.Owner, SharedCentrifuge.InputSlotName);
            var outputContainer = _itemSlotsSystem.GetItemOrNull(Centrifuge.Owner, SharedCentrifuge.OutputSlotName);

            if (TryComp(Centrifuge.Owner, out AppearanceComponent? appearance))
            {
                appearance.SetData(SharedCentrifuge.CentrifugeVisualState.BeakerAttached, Centrifuge.BeakerSlot.HasItem);
                appearance.SetData(SharedCentrifuge.CentrifugeVisualState.OutputAttached, Centrifuge.OutputSlot.HasItem);
            }

            var bufferReagents = bufferSolution.Contents;
            var bufferCurrentVolume = bufferSolution.Volume;

            var state = new CentrifugeBoundUserInterfaceState(
                Centrifuge.Mode, BuildContainerInfo(inputContainer), BuildContainerInfo(outputContainer),
                bufferReagents, bufferCurrentVolume);

            _userInterfaceSystem.TrySetUiState(Centrifuge.Owner, CentrifugeUiKey.Key, state);
        }

        private void OnSetModeMessage(EntityUid uid, CentrifugeComponent Centrifuge, CentrifugeSetModeMessage message)
        {
            // Ensure the mode is valid, either Transfer or Discard.
            if (!Enum.IsDefined(typeof(CentrifugeMode), message.CentrifugeMode))
                return;

            Centrifuge.Mode = message.CentrifugeMode;
            UpdateUiState(Centrifuge);
            ClickSound(Centrifuge);
        }

        private void OnReagentButtonMessage(EntityUid uid, CentrifugeComponent Centrifuge, CentrifugeReagentAmountButtonMessage message)
        {
            // Ensure the amount corresponds to one of the reagent amount buttons.
            if (!Enum.IsDefined(typeof(CentrifugeReagentAmount), message.Amount))
                return;

            switch (Centrifuge.Mode)
            {
                case CentrifugeMode.Transfer:
                    TransferReagents(Centrifuge, message.ReagentId, message.Amount.GetFixedPoint(), SharedCentrifuge.OutputSlotName, message.FromBuffer);
                    break;
                case CentrifugeMode.Discard:
                    DiscardReagents(Centrifuge, message.ReagentId, message.Amount.GetFixedPoint(), SharedCentrifuge.OutputSlotName, message.FromBuffer);
                    break;
                default:
                    // Invalid mode.
                    return;
            }

            ClickSound(Centrifuge);
        }

        private void OnActivateButtonMessage(EntityUid uid, CentrifugeComponent component, CentrifugeActivateButtonMessage message)
        {
            if (!this.IsPowered(component.Owner, EntityManager))
                return;

            ClickSound(component);

            if (!this.IsPowered(component.Owner, EntityManager) ||
                component.Busy ||
                component.BeakerSlot.Item is not EntityUid beakerEntity)
                return;

            component.Busy = true;

            //go through each reagent in beaker, transfer them to the buffer using TransferReagents button
            if (TryComp<SolutionContainerManagerComponent>(beakerEntity, out var solutions))
            {
                bool played = false;

                foreach (var solution in (solutions.Solutions)) //will only work on the first iter val //TODO make this better...
                {
                    var reagents = solution.Value.Contents.Select(reagent => (reagent.ReagentId, reagent.Quantity)).ToList();
                    foreach (var reagent in (reagents))
                    {
                        if (!played)
                        {
                            played = true;
                            SoundSystem.Play(component.GrindSound.GetSound(), Filter.Pvs(component.Owner), component.Owner, AudioParams.Default);
                        }
                        TransferReagents(component, reagent.ReagentId, reagent.Quantity, SharedCentrifuge.InputSlotName, false);
                    }
                }
            }

            component.Busy = false;
            UpdateUiState(component);
        }

        private void OnElectrolysisButtonMessage(EntityUid uid, CentrifugeComponent component, CentrifugeElectrolysisButtonMessage message)
        {
            ClickSound(component);

            if (!this.IsPowered(component.Owner, EntityManager) ||
                component.Busy ||
                component.BeakerSlot.Item is not EntityUid beakerEntity ||
                !_solutionContainerSystem.TryGetSolution(component.Owner, SharedCentrifuge.BufferSolutionName, out var bufferSolution))
                return;

            component.Busy = true;


            if (TryComp<SolutionContainerManagerComponent>(beakerEntity, out var solutions))
            {
                bool played = false;

                foreach (var solution in (solutions.Solutions)) //will only work on the first iter val //TODO make this better...
                {
                    var reagents = solution.Value.Contents.Select(reagent => (reagent.ReagentId, reagent.Quantity)).ToList();
                    foreach (var reagent in (reagents))
                    {
                        if (!played)
                        {
                            played = true;
                            SoundSystem.Play(component.GrindSound.GetSound(), Filter.Pvs(component.Owner), component.Owner, AudioParams.Default);
                        }
                        if (_reactions.TryGetValue(reagent.ReagentId, out var productReactions)) //typically only one of these...
                        {
                            DiscardReagents(component, reagent.ReagentId, reagent.Quantity, SharedCentrifuge.InputSlotName, false);
                            foreach (var reaction in productReactions)
                            {
                                FixedPoint2 totalCoeff = 0f;
                                foreach (var reactant in reaction.Reactants) {
                                    totalCoeff += reactant.Value.Amount;
                                }
                                foreach (var reactant in reaction.Reactants)
                                {
                                    var name = reactant.Key;
                                    var coeff = reactant.Value.Amount;
                                    var amount = (reagent.Quantity / totalCoeff) * coeff;

                                    if (!reactant.Value.Catalyst) {
                                        bufferSolution.AddReagent(name, amount);
                                    }
                                }
                            }

                        }
                        else
                        {
                            TransferReagents(component, reagent.ReagentId, reagent.Quantity, SharedCentrifuge.InputSlotName, false);
                        }
                    }


                }
            }

            component.Busy = false;
            UpdateUiState(component);
        }

        private void TransferReagents(CentrifugeComponent Centrifuge, string reagentId, FixedPoint2 amount, string slot, bool fromBuffer)
        {
            var container = _itemSlotsSystem.GetItemOrNull(Centrifuge.Owner, slot);
            if (container is null ||
                !TryComp<SolutionContainerManagerComponent>(container.Value, out var containerSolution) ||
                !_solutionContainerSystem.TryGetSolution(Centrifuge.Owner, SharedCentrifuge.BufferSolutionName, out var bufferSolution))
                return;

            if (containerSolution is null)
                return;

            if (fromBuffer) // Buffer to container
            {
                foreach (var solution in (containerSolution.Solutions)) //TODO make this better...
                {
                    amount = FixedPoint2.Min(amount, solution.Value.AvailableVolume);
                    amount = bufferSolution.RemoveReagent(reagentId, amount);
                    _solutionContainerSystem.TryAddReagent(container.Value, solution.Value, reagentId, amount, out var _);
                }
            }
            else // Container to buffer
            {
                foreach (var solution in (containerSolution.Solutions)) //TODO make this better...
                {
                    amount = FixedPoint2.Min(amount, solution.Value.GetReagentQuantity(reagentId));
                    _solutionContainerSystem.TryRemoveReagent(container.Value, solution.Value, reagentId, amount);
                    bufferSolution.AddReagent(reagentId, amount);
                }
            }

            UpdateUiState(Centrifuge);
        }

        private void DiscardReagents(CentrifugeComponent Centrifuge, string reagentId, FixedPoint2 amount, string slot, bool fromBuffer)
        {

            if (fromBuffer)
            {
                if (_solutionContainerSystem.TryGetSolution(Centrifuge.Owner, SharedCentrifuge.BufferSolutionName, out var bufferSolution))
                    bufferSolution.RemoveReagent(reagentId, amount);
                else
                    return;
            }
            else
            {
                var container = _itemSlotsSystem.GetItemOrNull(Centrifuge.Owner, SharedCentrifuge.InputSlotName);
                if (container is not null &&
                    _solutionContainerSystem.TryGetFitsInDispenser(container.Value, out var containerSolution))
                {
                    _solutionContainerSystem.TryRemoveReagent(container.Value, containerSolution, reagentId, amount);
                }
                else
                    return;
            }

            UpdateUiState(Centrifuge);
        }

        private void ClickSound(CentrifugeComponent Centrifuge)
        {
            _audioSystem.Play(Centrifuge.ClickSound, Filter.Pvs(Centrifuge.Owner), Centrifuge.Owner, false, AudioParams.Default.WithVolume(-2f));
        }

        private CentrifugeContainerInfo? BuildContainerInfo(EntityUid? container)
        {
            if (container is not { Valid: true })
                return null;

            /*if (!TryComp(container, out FitsInDispenserComponent? fits)
                || !_solutionContainerSystem.TryGetSolution(container.Value, fits.Solution, out var solution))
            {
                return null;
            }*/

            if (TryComp<SolutionContainerManagerComponent>(container, out var solutions))
                foreach (var solution in (solutions.Solutions)) //will only work on the first iter val
                {
                    var reagents = solution.Value.Contents.Select(reagent => (reagent.ReagentId, reagent.Quantity)).ToList();
                    return new CentrifugeContainerInfo(Name(container.Value), true, solution.Value.Volume, solution.Value.MaxVolume, reagents);
                }

            return null;
        }

        private void OnMapInit(EntityUid uid, CentrifugeComponent component, MapInitEvent args)
        {
            _itemSlotsSystem.AddItemSlot(uid, CentrifugeComponent.BeakerSlotId, component.BeakerSlot);
            _itemSlotsSystem.AddItemSlot(uid, CentrifugeComponent.OutputSlotId, component.OutputSlot);
        }
    }
}
