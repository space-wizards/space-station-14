using System.Collections.Generic;
using System.Linq;
using Content.Server.Chemistry;
using Content.Server.GameObjects.Components.GUI;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.Chemistry;
using Content.Shared.GameObjects.Components.Chemistry;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.GameObjects.Verbs;
using Content.Shared.Utility;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Chemistry
{
    /// <summary>
    ///    ECS component that manages a liquid solution of reagents.
    /// </summary>
    [RegisterComponent]
    public class SolutionContainerComponent : SharedSolutionContainerComponent, IExamine
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;

        private IEnumerable<ReactionPrototype> _reactions;
        private string _fillInitState;
        private int _fillInitSteps;
        private string _fillPathString = "Objects/Specific/Chemistry/fillings.rsi";
        private ResourcePath _fillPath;
        private SpriteSpecifier _fillSprite;
        private AudioSystem _audioSystem;
        private ChemistrySystem _chemistrySystem;
        private SpriteComponent _spriteComponent;

        /// <summary>
        ///     The total volume of all the of the reagents in the container.
        /// </summary>
        [ViewVariables]
        public ReagentUnit CurrentVolume => Solution.TotalVolume;

        /// <summary>
        ///     The volume without reagents remaining in the container.
        /// </summary>
        [ViewVariables]
        public ReagentUnit EmptyVolume => MaxVolume - CurrentVolume;

        /// <summary>
        ///     The current blended color of all the reagents in the container.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public Color SubstanceColor { get; private set; }

        /// <summary>
        ///     The current capabilities of this container (is the top open to pour? can I inject it into another object?).
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public SolutionContainerCaps Capabilities { get; set; }

        /// <summary>
        ///     The contained solution.
        /// </summary>
        [ViewVariables]
        public Solution Solution { get; set; }

        /// <summary>
        ///     The maximum volume of the container.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public ReagentUnit MaxVolume { get; set; }

        public IReadOnlyList<Solution.ReagentQuantity> ReagentList => Solution.Contents;
        public bool CanExamineContents => (Capabilities & SolutionContainerCaps.NoExamine) == 0;
        public bool CanUseWithChemDispenser => (Capabilities & SolutionContainerCaps.FitsInDispenser) != 0;
        public bool CanAddSolutions => (Capabilities & SolutionContainerCaps.AddTo) != 0;
        public bool CanRemoveSolutions => (Capabilities & SolutionContainerCaps.RemoveFrom) != 0;

        /// <inheritdoc />
        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(this, x => MaxVolume, "maxVol", ReagentUnit.New(0));
            serializer.DataField(this, x => Solution, "contents", new Solution());
            serializer.DataField(this, x => Capabilities, "caps", SolutionContainerCaps.AddTo | SolutionContainerCaps.RemoveFrom);
            serializer.DataField(ref _fillInitState, "fillingState", string.Empty);
            serializer.DataField(ref _fillInitSteps, "fillingSteps", 7);
        }

        public override void Initialize()
        {
            base.Initialize();
            _audioSystem = EntitySystem.Get<AudioSystem>();
            _chemistrySystem = _entitySystemManager.GetEntitySystem<ChemistrySystem>();
            _reactions = _prototypeManager.EnumeratePrototypes<ReactionPrototype>();
        }

        protected override void Startup()
        {
            base.Startup();
            RecalculateColor();
            if (!string.IsNullOrEmpty(_fillInitState))
            {
                _spriteComponent = Owner.GetComponent<SpriteComponent>();
                _fillPath = new ResourcePath(_fillPathString);
                _fillSprite = new SpriteSpecifier.Rsi(_fillPath, _fillInitState + (_fillInitSteps - 1));
                _spriteComponent.AddLayerWithSprite(_fillSprite);
                UpdateFillIcon();
            }
        }

        public void RemoveAllSolution()
        {
            Solution.RemoveAllSolution();
            OnSolutionChanged(false);
        }

        public bool TryRemoveReagent(string reagentId, ReagentUnit quantity)
        {
            if (!ContainsReagent(reagentId, out var currentQuantity))
            {
                return false;
            }

            Solution.RemoveReagent(reagentId, quantity);
            OnSolutionChanged(false);
            return true;
        }

        /// <summary>
        /// Attempt to remove the specified quantity from this solution
        /// </summary>
        /// <param name="quantity">Quantity of this solution to remove</param>
        /// <returns>Whether or not the solution was successfully removed</returns>
        public bool TryRemoveSolution(ReagentUnit quantity)
        {
            if (CurrentVolume == 0)
            {
                return false;
            }

            Solution.RemoveSolution(quantity);
            OnSolutionChanged(false);
            return true;
        }

        public Solution SplitSolution(ReagentUnit quantity)
        {
            var solutionSplit = Solution.SplitSolution(quantity);
            OnSolutionChanged(false);
            return solutionSplit;
        }

        protected void RecalculateColor()
        {
            if (Solution.TotalVolume == 0)
            {
                SubstanceColor = Color.Transparent;
                return;
            }

            Color mixColor = default;
            var runningTotalQuantity = ReagentUnit.New(0);

            foreach (var reagent in Solution)
            {
                runningTotalQuantity += reagent.Quantity;

                if (!_prototypeManager.TryIndex(reagent.ReagentId, out ReagentPrototype proto))
                {
                    continue;
                }

                if (mixColor == default)
                {
                    mixColor = proto.SubstanceColor;
                    continue;
                }

                var interpolateValue = (1 / runningTotalQuantity.Float()) * reagent.Quantity.Float();
                mixColor = Color.InterpolateBetween(mixColor, proto.SubstanceColor, interpolateValue);
            }

            SubstanceColor = mixColor;
        }

        /// <summary>
        ///     Transfers solution from the held container to the target container.
        /// </summary>
        [Verb]
        private sealed class FillTargetVerb : Verb<SolutionContainerComponent>
        {
            protected override void GetData(IEntity user, SolutionContainerComponent component, VerbData data)
            {
                if (!ActionBlockerSystem.CanInteract(user) ||
                    !user.TryGetComponent<HandsComponent>(out var hands) ||
                    hands.GetActiveHand == null ||
                    hands.GetActiveHand.Owner == component.Owner ||
                    !hands.GetActiveHand.Owner.TryGetComponent<SolutionContainerComponent>(out var solution) ||
                    !solution.CanRemoveSolutions ||
                    !component.CanAddSolutions)
                {
                    data.Visibility = VerbVisibility.Invisible;
                    return;
                }

                var heldEntityName = hands.GetActiveHand.Owner?.Prototype?.Name ?? "<Item>";
                var myName = component.Owner.Prototype?.Name ?? "<Item>";

                var locHeldEntityName = Loc.GetString(heldEntityName);
                var locMyName = Loc.GetString(myName);

                data.Visibility = VerbVisibility.Visible;
                data.Text = Loc.GetString("Transfer liquid from [{0}] to [{1}].", locHeldEntityName, locMyName);
            }

            protected override void Activate(IEntity user, SolutionContainerComponent component)
            {
                if (!user.TryGetComponent<HandsComponent>(out var hands) || hands.GetActiveHand == null)
                {
                    return;
                }

                if (!hands.GetActiveHand.Owner.TryGetComponent<SolutionContainerComponent>(out var handSolutionComp) ||
                    !handSolutionComp.CanRemoveSolutions ||
                    !component.CanAddSolutions)
                {
                    return;
                }

                var transferQuantity = ReagentUnit.Min(component.MaxVolume - component.CurrentVolume, handSolutionComp.CurrentVolume, ReagentUnit.New(10));

                if (transferQuantity <= 0)
                {
                    return;
                }

                var transferSolution = handSolutionComp.SplitSolution(transferQuantity);
                component.TryAddSolution(transferSolution);
            }
        }

        void IExamine.Examine(FormattedMessage message, bool inDetailsRange)
        {
            if (!CanExamineContents)
            {
                return;
            }

            if (ReagentList.Count == 0)
            {
                message.AddText(Loc.GetString("It's empty."));
            }
            else if (ReagentList.Count == 1)
            {
                var reagent = ReagentList[0];

                if (_prototypeManager.TryIndex(reagent.ReagentId, out ReagentPrototype proto))
                {
                    message.AddMarkup(
                        Loc.GetString("It contains a [color={0}]{1}[/color] substance.",
                            proto.GetSubstanceTextColor().ToHexNoAlpha(),
                            Loc.GetString(proto.PhysicalDescription)));
                }
            }
            else
            {
                var reagent = ReagentList.Max();

                if (_prototypeManager.TryIndex(reagent.ReagentId, out ReagentPrototype proto))
                {
                    message.AddMarkup(
                        Loc.GetString("It contains a [color={0}]{1}[/color] mixture of substances.",
                            SubstanceColor.ToHexNoAlpha(),
                            Loc.GetString(proto.PhysicalDescription)));
                }
            }
        }

        /// <summary>
        ///     Transfers solution from a target container to the held container.
        /// </summary>
        [Verb]
        private sealed class EmptyTargetVerb : Verb<SolutionContainerComponent>
        {
            protected override void GetData(IEntity user, SolutionContainerComponent component, VerbData data)
            {
                if (!ActionBlockerSystem.CanInteract(user) ||
                    !user.TryGetComponent<HandsComponent>(out var hands) ||
                    hands.GetActiveHand == null ||
                    hands.GetActiveHand.Owner == component.Owner ||
                    !hands.GetActiveHand.Owner.TryGetComponent<SolutionContainerComponent>(out var solution) ||
                    !solution.CanAddSolutions ||
                    !component.CanRemoveSolutions)
                {
                    data.Visibility = VerbVisibility.Invisible;
                    return;
                }

                var heldEntityName = hands.GetActiveHand.Owner?.Prototype?.Name ?? "<Item>";
                var myName = component.Owner.Prototype?.Name ?? "<Item>";

                var locHeldEntityName = Loc.GetString(heldEntityName);
                var locMyName = Loc.GetString(myName);

                data.Visibility = VerbVisibility.Visible;
                data.Text = Loc.GetString("Transfer liquid from [{0}] to [{1}].", locMyName, locHeldEntityName);
                return;
            }

            protected override void Activate(IEntity user, SolutionContainerComponent component)
            {
                if (!user.TryGetComponent<HandsComponent>(out var hands) || hands.GetActiveHand == null)
                {
                    return;
                }

                if(!hands.GetActiveHand.Owner.TryGetComponent<SolutionContainerComponent>(out var handSolutionComp) ||
                    !handSolutionComp.CanAddSolutions ||
                    !component.CanRemoveSolutions)
                {
                    return;
                }

                var transferQuantity = ReagentUnit.Min(handSolutionComp.MaxVolume - handSolutionComp.CurrentVolume, component.CurrentVolume, ReagentUnit.New(10));

                if (transferQuantity <= 0)
                {
                    return;
                }

                var transferSolution = component.SplitSolution(transferQuantity);
                handSolutionComp.TryAddSolution(transferSolution);
            }
        }

        private void CheckForReaction()
        {
            bool checkForNewReaction = false;
            while (true)
            {
                //TODO: make a hashmap at startup and then look up reagents in the contents for a reaction
                //Check the solution for every reaction
                foreach (var reaction in _reactions)
                {
                    if (SolutionValidReaction(reaction, out var unitReactions))
                    {
                        PerformReaction(reaction, unitReactions);
                        checkForNewReaction = true;
                        break;
                    }
                }

                //Check for a new reaction if a reaction occurs, run loop again.
                if (checkForNewReaction)
                {
                    checkForNewReaction = false;
                    continue;
                }
                return;
            }
        }

        public bool TryAddReagent(string reagentId, ReagentUnit quantity, out ReagentUnit acceptedQuantity, bool skipReactionCheck = false, bool skipColor = false)
        {
            var toAcceptQuantity = MaxVolume - Solution.TotalVolume;
            if (quantity > toAcceptQuantity)
            {
                acceptedQuantity = toAcceptQuantity;
                if (acceptedQuantity == 0) return false;
            }
            else
            {
                acceptedQuantity = quantity;
            }

            Solution.AddReagent(reagentId, acceptedQuantity);
            if (!skipColor) {
                RecalculateColor();
            }
            if(!skipReactionCheck)
                CheckForReaction();
            OnSolutionChanged(skipColor);
            return true;
        }

        public bool CanAddSolution(Solution solution)
        {
            return solution.TotalVolume <= (MaxVolume - Solution.TotalVolume);
        }

        public bool TryAddSolution(Solution solution, bool skipReactionCheck = false, bool skipColor = false)
        {
            if (!CanAddSolution(solution))
                return false;

            Solution.AddSolution(solution);
            if (!skipColor) {
                RecalculateColor();
            }
            if(!skipReactionCheck)
                CheckForReaction();
            OnSolutionChanged(skipColor);
            return true;
        }

        /// <summary>
        /// Checks if a solution has the reactants required to cause a specified reaction.
        /// </summary>
        /// <param name="solution">The solution to check for reaction conditions.</param>
        /// <param name="reaction">The reaction whose reactants will be checked for in the solution.</param>
        /// <param name="unitReactions">The number of times the reaction can occur with the given solution.</param>
        /// <returns></returns>
        private bool SolutionValidReaction(ReactionPrototype reaction, out ReagentUnit unitReactions)
        {
            unitReactions = ReagentUnit.MaxValue; //Set to some impossibly large number initially
            foreach (var reactant in reaction.Reactants)
            {
                if (!ContainsReagent(reactant.Key, out ReagentUnit reagentQuantity))
                {
                    return false;
                }
                var currentUnitReactions = reagentQuantity / reactant.Value.Amount;
                if (currentUnitReactions < unitReactions)
                {
                    unitReactions = currentUnitReactions;
                }
            }

            if (unitReactions == 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// Perform a reaction on a solution. This assumes all reaction criteria have already been checked and are met.
        /// </summary>
        /// <param name="solution">Solution to be reacted.</param>
        /// <param name="reaction">Reaction to occur.</param>
        /// <param name="unitReactions">The number of times to cause this reaction.</param>
        private void PerformReaction(ReactionPrototype reaction, ReagentUnit unitReactions)
        {
            //Remove non-catalysts
            foreach (var reactant in reaction.Reactants)
            {
                if (!reactant.Value.Catalyst)
                {
                    var amountToRemove = unitReactions * reactant.Value.Amount;
                    TryRemoveReagent(reactant.Key, amountToRemove);
                }
            }

            // Add products
            foreach (var product in reaction.Products)
            {
                TryAddReagent(product.Key, product.Value * unitReactions, out var acceptedQuantity, true);
            }

            // Trigger reaction effects
            foreach (var effect in reaction.Effects)
            {
                effect.React(Owner, unitReactions.Double());
            }

            // Play reaction sound client-side
            _audioSystem.PlayAtCoords("/Audio/Effects/Chemistry/bubbles.ogg", Owner.Transform.Coordinates);
        }

        /// <summary>
        /// Check if the solution contains the specified reagent.
        /// </summary>
        /// <param name="reagentId">The reagent to check for.</param>
        /// <param name="quantity">Output the quantity of the reagent if it is contained, 0 if it isn't.</param>
        /// <returns>Return true if the solution contains the reagent.</returns>
        public bool ContainsReagent(string reagentId, out ReagentUnit quantity)
        {
            foreach (var reagent in Solution.Contents)
            {
                if (reagent.ReagentId == reagentId)
                {
                    quantity = reagent.Quantity;
                    return true;
                }
            }

            quantity = ReagentUnit.New(0);
            return false;
        }

        public string GetMajorReagentId()
        {
            if (Solution.Contents.Count == 0)
            {
                return "";
            }

            var majorReagent = Solution.Contents.OrderByDescending(reagent => reagent.Quantity).First();;
            return majorReagent.ReagentId;
        }

        protected void UpdateFillIcon()
        {
            if (string.IsNullOrEmpty(_fillInitState))
            {
                return;
            }

            var percentage =  (CurrentVolume / MaxVolume).Double();
            var level = ContentHelpers.RoundToLevels(percentage * 100, 100, _fillInitSteps);

            //Transformed glass uses special fancy sprites so we don't bother
            if (level == 0 || (Owner.TryGetComponent<TransformableContainerComponent>(out var transformComp) && transformComp.Transformed))
            {
                _spriteComponent.LayerSetColor(1, Color.Transparent);
                return;
            }

            _fillSprite = new SpriteSpecifier.Rsi(_fillPath, _fillInitState + level);
            _spriteComponent.LayerSetSprite(1, _fillSprite);
            _spriteComponent.LayerSetColor(1, SubstanceColor);
        }

        protected virtual void OnSolutionChanged(bool skipColor)
        {
            if (!skipColor)
            {
                RecalculateColor();
            }

            UpdateFillIcon();
            _chemistrySystem.HandleSolutionChange(Owner);
        }
    }
}
