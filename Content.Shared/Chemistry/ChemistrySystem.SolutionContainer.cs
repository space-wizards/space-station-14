using Content.Shared.Chemistry.Reaction;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Chemistry.Solution.Components;
using Content.Shared.Examine;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;

namespace Content.Shared.Chemistry
{
    /// <summary>
    /// This event alerts system that the solution was changed
    /// </summary>
    public class SolutionChangedEvent : EntityEventArgs
    {
        public IEntity Owner { get; }

        public SolutionChangedEvent(IEntity owner)
        {
            Owner = owner;
        }
    }


    /// <summary>
    /// Part of Chemistry system deal with SolutionContainers
    /// </summary>
    public partial class ChemistrySystem
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly SharedChemicalReactionSystem _chemistrySystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<SolutionContainerComponent, ComponentInit>(InitSolution);
            SubscribeLocalEvent<SolutionContainerComponent, ExaminedEvent>(OnExamineSolution);
        }

        private void InitSolution(EntityUid uid, SolutionContainerComponent component, ComponentInit args)
        {
            UpdateAppearance(component);
        }

        private void OnExamineSolution(EntityUid uid, SolutionContainerComponent component, ExaminedEvent args)
        {
            if (!component.CanExamineContents)
                return;

            if (component.ReagentList.Count == 0)
            {
                args.Message.AddText(Loc.GetString("shared-solution-container-component-on-examine-empty-container"));
                return;
            }

            var primaryReagent = component.Solution.GetPrimaryReagentId();
            if (!_prototypeManager.TryIndex(primaryReagent, out ReagentPrototype? proto))
            {
                Logger.Error(
                    $"{nameof(SolutionContainerComponent)} could not find the prototype associated with {primaryReagent}.");
                return;
            }

            var colorHex =
                component.Color
                    .ToHexNoAlpha(); //TODO: If the chem has a dark color, the examine text becomes black on a black background, which is unreadable.
            var messageString = "shared-solution-container-component-on-examine-main-text";

            args.Message.AddMarkup(Loc.GetString(messageString,
                ("color", colorHex),
                ("wordedAmount", Loc.GetString(component.ReagentList.Count == 1
                    ? "shared-solution-container-component-on-examine-worded-amount-one-reagent"
                    : "shared-solution-container-component-on-examine-worded-amount-multiple-reagents")),
                ("desc", Loc.GetString(proto.PhysicalDescription))));
        }

        private void UpdateAppearance(SolutionContainerComponent component)
        {
            if (component.Owner.Deleted
                || !component.Owner.TryGetComponent<SharedAppearanceComponent>(out var appearance))
                return;

            appearance.SetData(SolutionContainerVisuals.VisualState, component.GetVisualState());
            component.Dirty();
        }

        public void Refill(SolutionContainerComponent container, Solution.Solution solution)
        {
            if (!container.CanRefill)
                return;

            TryAddSolution(container, solution);
        }

        public void Inject(SolutionContainerComponent container, Solution.Solution solution)
        {
            if (!container.CanInject)
                return;

            TryAddSolution(container, solution);
        }

        public Solution.Solution Draw(SolutionContainerComponent container, ReagentUnit amount)
        {
            if (!container.CanDraw)
                return new Solution.Solution();

            return SplitSolution(container, amount);
        }

        public Solution.Solution Drain(SolutionContainerComponent container, ReagentUnit amount)
        {
            if (!container.CanDrain)
                return new Solution.Solution();

            return SplitSolution(container, amount);
        }

        /// <summary>
        ///     Removes part of the solution in the container.
        /// </summary>
        /// <param name="quantity">the volume of solution to remove.</param>
        /// <returns>The solution that was removed.</returns>
        public Solution.Solution SplitSolution(SolutionContainerComponent container, ReagentUnit quantity)
        {
            var splitSol = container.Solution.SplitSolution(quantity);
            UpdateChemicals(container);
            return splitSol;
        }

        private void UpdateChemicals(SolutionContainerComponent container, bool needsReactionsProcessing = false)
        {
            // Process reactions
            if (needsReactionsProcessing && container.CanReact)
            {
                _chemistrySystem
                    .FullyReactSolution(container.Solution, container.Owner, container.MaxVolume);
            }

            UpdateAppearance(container);
            RaiseLocalEvent(new SolutionChangedEvent(container.Owner));
        }

        public void RemoveAllSolution(SolutionContainerComponent container)
        {
            if (container.CurrentVolume == 0)
                return;

            container.Solution.RemoveAllSolution();
            UpdateChemicals(container);
        }

        /// <summary>
        ///     Adds reagent of an Id to the container.
        /// </summary>
        /// <param name="container">Container to which we are adding reagent</param>
        /// <param name="reagentId">The Id of the reagent to add.</param>
        /// <param name="quantity">The amount of reagent to add.</param>
        /// <param name="acceptedQuantity">The amount of reagent sucesfully added.</param>
        /// <returns>If all the reagent could be added.</returns>
        public bool TryAddReagent(SolutionContainerComponent container, string reagentId, ReagentUnit quantity,
            out ReagentUnit acceptedQuantity)
        {
            acceptedQuantity = container.EmptyVolume > quantity ? quantity : container.EmptyVolume;
            container.Solution.AddReagent(reagentId, acceptedQuantity);

            if (acceptedQuantity > 0)
                UpdateChemicals(container, true);

            return acceptedQuantity == quantity;
        }

        /// <summary>
        ///     Removes reagent of an Id to the container.
        /// </summary>
        /// <param name="container">Solution container from which we are removing reagent</param>
        /// <param name="reagentId">The Id of the reagent to remove.</param>
        /// <param name="quantity">The amount of reagent to remove.</param>
        /// <returns>If the reagent to remove was found in the container.</returns>
        public bool TryRemoveReagent(SolutionContainerComponent? container, string reagentId, ReagentUnit quantity)
        {
            if (container == null || !container.Solution.ContainsReagent(reagentId))
                return false;

            container.Solution.RemoveReagent(reagentId, quantity);
            UpdateChemicals(container);
            return true;
        }

        /// <summary>
        ///     Adds a solution to the container, if it can fully fit.
        /// </summary>
        /// <param name="container">The container to which we try to add.</param>
        /// <param name="solution">The solution to try to add.</param>
        /// <returns>If the solution could be added.</returns>
        public bool TryAddSolution(SolutionContainerComponent? container, Solution.Solution solution)
        {
            if (container == null || !container.CanAddSolution(solution) || solution.TotalVolume == 0)
                return false;

            container.Solution.AddSolution(solution);
            UpdateChemicals(container, true);
            return true;
        }
    }

    public static class SolutionContainerHelpers
    {
        internal static SolutionContainerVisualState GetVisualState(this SolutionContainerComponent component)
        {
            var filledVolumeFraction = component.CurrentVolume.Float() / component.MaxVolume.Float();

            return new SolutionContainerVisualState(component.Color, filledVolumeFraction);
        }
    }
}
