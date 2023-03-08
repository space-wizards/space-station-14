using System.Linq;
using Content.Server.Chemistry.Components;
using Content.Server.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.FixedPoint;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Player;
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
        [Dependency] private readonly AppearanceSystem _appearance = default!;
        [Dependency] private readonly AudioSystem _audio = default!;
        [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
        [Dependency] private readonly SolutionContainerSystem _solutionContainer = default!;
        [Dependency] private readonly UserInterfaceSystem _ui = default!;
        [Dependency] private readonly IPrototypeManager _prototype = default!;

        /// <summary>
        ///     A cache of all existant chemical reactions indexed by their resulting reagent.
        /// </summary>
        private IDictionary<string, List<ReactionPrototype>> _reactions = default!;

        public override void Initialize()
        {
            base.Initialize();

            InitializeReactionCache();

            _prototype.PrototypesReloaded += OnPrototypesReloaded;

            SubscribeLocalEvent<CentrifugeComponent, ComponentStartup>((uid, comp, _) => UpdateUiState(uid, comp));
            SubscribeLocalEvent<CentrifugeComponent, SolutionChangedEvent>((uid, comp, _) => UpdateUiState(uid, comp));
            SubscribeLocalEvent<CentrifugeComponent, EntInsertedIntoContainerMessage>((uid, comp, _) => UpdateUiState(uid, comp));
            SubscribeLocalEvent<CentrifugeComponent, EntRemovedFromContainerMessage>((uid, comp, _) => UpdateUiState(uid, comp));

            SubscribeLocalEvent((EntityUid uid, CentrifugeComponent comp, ref PowerChangedEvent _) => UpdateUiState(uid, comp));
            SubscribeLocalEvent<CentrifugeComponent, ContainerIsRemovingAttemptEvent>(OnEntRemoveAttempt);

            SubscribeLocalEvent<CentrifugeComponent, BoundUIOpenedEvent>((uid, comp, _) => UpdateUiState(uid, comp));

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

            var reactions = _prototype.EnumeratePrototypes<ReactionPrototype>();
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

        private void OnEntRemoveAttempt(EntityUid uid, CentrifugeComponent component, ContainerIsRemovingAttemptEvent args)
        {
            if (component.Busy)
                args.Cancel();
        }

        private void UpdateUiState(EntityUid uid, CentrifugeComponent centrifuge)
        {
            if (centrifuge.Busy)
                return;

            if (!_solutionContainer.TryGetSolution(uid, SharedCentrifuge.BufferSolutionName, out var bufferSolution))
                return;

            var inputContainer = _itemSlots.GetItemOrNull(uid, SharedCentrifuge.InputSlotName);
            var outputContainer = _itemSlots.GetItemOrNull(uid, SharedCentrifuge.OutputSlotName);

            if (TryComp(uid, out AppearanceComponent? appearance))
            {
                _appearance.SetData(uid, SharedCentrifuge.CentrifugeVisualState.BeakerAttached, centrifuge.BeakerSlot.HasItem, appearance);
                _appearance.SetData(uid, SharedCentrifuge.CentrifugeVisualState.OutputAttached, centrifuge.OutputSlot.HasItem, appearance);
            }

            var bufferReagents = bufferSolution.Contents;
            var bufferCurrentVolume = bufferSolution.Volume;

            var state = new CentrifugeBoundUserInterfaceState(
                centrifuge.Mode, BuildContainerInfo(inputContainer), BuildContainerInfo(outputContainer),
                bufferReagents, bufferCurrentVolume);

            _ui.TrySetUiState(uid, CentrifugeUiKey.Key, state);
        }

        private void OnSetModeMessage(EntityUid uid, CentrifugeComponent centrifuge, CentrifugeSetModeMessage message)
        {
            // Ensure the mode is valid, either Transfer or Discard.
            if (!Enum.IsDefined(typeof(CentrifugeMode), message.CentrifugeMode))
                return;

            centrifuge.Mode = message.CentrifugeMode;
            UpdateUiState(uid, centrifuge);
            ClickSound(uid, centrifuge);
        }

        private void OnReagentButtonMessage(EntityUid uid, CentrifugeComponent centrifuge, CentrifugeReagentAmountButtonMessage message)
        {
            // Ensure the amount corresponds to one of the reagent amount buttons.
            if (!Enum.IsDefined(typeof(CentrifugeReagentAmount), message.Amount))
                return;

            switch (centrifuge.Mode)
            {
                case CentrifugeMode.Transfer:
                    TransferReagents(uid, message.ReagentId, message.Amount.GetFixedPoint(), SharedCentrifuge.OutputSlotName, message.FromBuffer);
                    UpdateUiState(uid, centrifuge);
                    break;
                case CentrifugeMode.Discard:
                    DiscardReagents(uid, message.ReagentId, message.Amount.GetFixedPoint(), message.FromBuffer);
                    UpdateUiState(uid, centrifuge);
                    break;
                default:
                    // Invalid mode.
                    return;
            }

            ClickSound(uid, centrifuge);
        }

        private void OnActivateButtonMessage(EntityUid uid, CentrifugeComponent component, CentrifugeActivateButtonMessage message)
        {
            if (!this.IsPowered(uid, EntityManager))
                return;

            ClickSound(uid, component);

            if (component.Busy ||
                component.BeakerSlot.Item is not { } beakerEntity)
                return;

            //go through each reagent in beaker, transfer them to the buffer using TransferReagents button
            if (TryComp<SolutionContainerManagerComponent>(beakerEntity, out var solutions))
            {
                if (solutions.Solutions.First().Value.Contents.Count != 0)
                    _audio.Play(_audio.GetSound(component.GrindSound), Filter.Pvs(uid), uid, true, AudioParams.Default);

                foreach (var (_, solution) in solutions.Solutions)
                {
                    foreach (var reagent in solution.Contents)
                    {
                        TransferReagents(uid, reagent.ReagentId, reagent.Quantity, SharedCentrifuge.InputSlotName, false);
                    }
                }
            }

            UpdateUiState(uid, component);
        }

        private void OnElectrolysisButtonMessage(EntityUid uid, CentrifugeComponent component, CentrifugeElectrolysisButtonMessage message)
        {
            if (!this.IsPowered(uid, EntityManager) ||
                component.Busy ||
                component.BeakerSlot.Item is not { } beakerEntity ||
                !_solutionContainer.TryGetSolution(uid, SharedCentrifuge.BufferSolutionName, out var bufferSolution))
                return;

            ClickSound(uid, component);

            if (TryComp<SolutionContainerManagerComponent>(beakerEntity, out var solutions))
            {
                if (solutions.Solutions.First().Value.Contents.Count != 0)
                    _audio.Play(_audio.GetSound(component.GrindSound), Filter.Pvs(uid), uid, true, AudioParams.Default);

                foreach (var (_, solution) in solutions.Solutions)
                {
                    foreach (var reagent in solution.Contents)
                    {
                        if (_reactions.TryGetValue(reagent.ReagentId, out var productReactions))
                        {
                            DiscardReagents(uid, reagent.ReagentId, reagent.Quantity, false);
                            foreach (var reaction in productReactions)
                            {
                                var totalCoeff = 0f;
                                foreach (var (_, proto) in reaction.Reactants)
                                {
                                    totalCoeff += proto.Amount.Float();
                                }

                                foreach (var (name, proto) in reaction.Reactants)
                                {
                                    // PRECISION: We intentionally have the coefficient and quantity be floats to try and avoid accidentally deleting people's chems
                                    //            in situations where there's many prototypes.
                                    var amount = (reagent.Quantity.Float() / totalCoeff) * proto.Amount;

                                    if (!proto.Catalyst)
                                        bufferSolution.AddReagent(name, amount);
                                }
                            }
                        }
                        else
                        {
                            TransferReagents(uid, reagent.ReagentId, reagent.Quantity, SharedCentrifuge.InputSlotName, false);
                        }
                    }
                }
            }

            UpdateUiState(uid, component);
        }

        /// <summary>
        /// Transfers reagents from one buffer to another within the centrifuge.
        /// </summary>
        /// <remarks>This does not refresh the UI.</remarks>
        private void TransferReagents(EntityUid uid, string reagentId, FixedPoint2 amount, string slot, bool fromBuffer)
        {
            var container = _itemSlots.GetItemOrNull(uid, slot);
            if (container is null
                || !TryComp<SolutionContainerManagerComponent>(container.Value, out var containerSolution)
                || !_solutionContainer.TryGetSolution(uid, SharedCentrifuge.BufferSolutionName, out var bufferSolution))
                return;

            if (fromBuffer) // Buffer to container
            {
                foreach (var (_, solution) in containerSolution.Solutions)
                {
                    amount = FixedPoint2.Min(amount, solution.AvailableVolume);
                    amount = bufferSolution.RemoveReagent(reagentId, amount);
                    _solutionContainer.TryAddReagent(container.Value, solution, reagentId, amount, out _);
                }
            }
            else // Container to buffer
            {
                foreach (var (_, solution) in containerSolution.Solutions)
                {
                    amount = FixedPoint2.Min(amount, solution.GetReagentQuantity(reagentId));
                    _solutionContainer.TryRemoveReagent(container.Value, solution, reagentId, amount);
                    bufferSolution.AddReagent(reagentId, amount);
                }
            }
        }

        /// <summary>
        /// Discards reagents within the centrifuge.
        /// </summary>
        /// <remarks>This does not refresh the UI.</remarks>
        private void DiscardReagents(EntityUid uid, string reagentId, FixedPoint2 amount, bool fromBuffer)
        {
            if (fromBuffer)
            {
                if (_solutionContainer.TryGetSolution(uid, SharedCentrifuge.BufferSolutionName, out var bufferSolution))
                    bufferSolution.RemoveReagent(reagentId, amount);
            }
            else
            {
                var container = _itemSlots.GetItemOrNull(uid, SharedCentrifuge.InputSlotName);
                if (container is not null
                    && _solutionContainer.TryGetFitsInDispenser(container.Value, out var containerSolution))
                {
                    _solutionContainer.TryRemoveReagent(container.Value, containerSolution, reagentId, amount);
                }
            }
        }

        private void ClickSound(EntityUid uid, CentrifugeComponent centrifuge)
        {
            _audio.Play(centrifuge.ClickSound, Filter.Pvs(uid), uid, true, AudioParams.Default.WithVolume(-2f));
        }

        private CentrifugeContainerInfo? BuildContainerInfo(EntityUid? container)
        {
            if (container is not { Valid: true })
                return null;

            /*if (!TryComp(container, out FitsInDispenserComponent? fits)
                || !_solutionContainer.TryGetSolution(container.Value, fits.Solution, out var solution))
            {
                return null;
            }*/

            if (TryComp<SolutionContainerManagerComponent>(container, out var solutions))
            {
                foreach (var solution in (solutions.Solutions)) //will only work on the first iter val
                {
                    var reagents = solution.Value.Contents.Select(reagent => (reagent.ReagentId, reagent.Quantity)).ToList();
                    return new CentrifugeContainerInfo(Name(container.Value), true, solution.Value.Volume, solution.Value.MaxVolume, reagents);
                }
            }

            return null;
        }

        private void OnMapInit(EntityUid uid, CentrifugeComponent component, MapInitEvent args)
        {
            _itemSlots.AddItemSlot(uid, CentrifugeComponent.BeakerSlotId, component.BeakerSlot);
            _itemSlots.AddItemSlot(uid, CentrifugeComponent.OutputSlotId, component.OutputSlot);
        }
    }
}
