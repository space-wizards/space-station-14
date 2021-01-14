#nullable enable
using Content.Shared.Chemistry;
using Content.Shared.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.Appearance;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;
using System;
using System.Collections.Generic;

namespace Content.Shared.GameObjects.Components.Chemistry
{
    /// <summary>
    ///     Holds a <see cref="Solution"/> with a limited volume.
    /// </summary>
    public abstract class SharedSolutionContainerComponent : Component, IExamine
    {
        public override string Name => "SolutionContainer";

        /// <inheritdoc />
        public sealed override uint? NetID => ContentNetIDs.SOLUTION;

        [ViewVariables]
        public Solution Solution { get; private set; } = new();

        public IReadOnlyList<Solution.ReagentQuantity> ReagentList => Solution.Contents;

        [ViewVariables(VVAccess.ReadWrite)]
        public ReagentUnit MaxVolume { get; set; }

        [ViewVariables]
        public ReagentUnit CurrentVolume => Solution.TotalVolume;

        /// <summary>
        ///     Volume needed to fill this container.
        /// </summary>
        [ViewVariables]
        public ReagentUnit EmptyVolume => MaxVolume - CurrentVolume;

        [ViewVariables]
        public virtual Color Color => Solution.Color;

        /// <summary>
        ///     If reactions will be checked for when adding reagents to the container.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public bool CanReact { get; set; }

        [ViewVariables(VVAccess.ReadWrite)]
        public SolutionContainerCaps Capabilities { get; set; }

        public bool CanExamineContents => Capabilities.HasCap(SolutionContainerCaps.CanExamine);

        public bool CanUseWithChemDispenser => Capabilities.HasCap(SolutionContainerCaps.FitsInDispenser);

        public bool CanAddSolutions => Capabilities.HasCap(SolutionContainerCaps.AddTo);

        public bool CanRemoveSolutions => Capabilities.HasCap(SolutionContainerCaps.RemoveFrom);

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(this, x => x.CanReact, "canReact", true);
            serializer.DataField(this, x => x.MaxVolume, "maxVol", ReagentUnit.New(0));
            serializer.DataField(this, x => x.Solution, "contents", new Solution());
            serializer.DataField(this, x => x.Capabilities, "caps", SolutionContainerCaps.AddTo | SolutionContainerCaps.RemoveFrom | SolutionContainerCaps.CanExamine);
        }

        public void RemoveAllSolution()
        {
            if (CurrentVolume == 0)
                return;

            Solution.RemoveAllSolution();
            ChemicalsRemoved();
        }

        /// <summary>
        ///     Adds reagent of an Id to the container.
        /// </summary>
        /// <param name="reagentId">The Id of the reagent to add.</param>
        /// <param name="quantity">The amount of reagent to add.</param>
        /// <param name="acceptedQuantity">The amount of reagent sucesfully added.</param>
        /// <returns>If all the reagent could be added.</returns>
        public bool TryAddReagent(string reagentId, ReagentUnit quantity, out ReagentUnit acceptedQuantity)
        {
            acceptedQuantity = EmptyVolume > quantity ? quantity : EmptyVolume;
            Solution.AddReagent(reagentId, acceptedQuantity);

            if (acceptedQuantity > 0)
                ChemicalsAdded();

            return acceptedQuantity == quantity;
        }

        /// <summary>
        ///     Removes reagent of an Id to the container.
        /// </summary>
        /// <param name="reagentId">The Id of the reagent to remove.</param>
        /// <param name="quantity">The amount of reagent to remove.</param>
        /// <returns>If the reagent to remove was found in the container.</returns>
        public bool TryRemoveReagent(string reagentId, ReagentUnit quantity)
        {
            if (!Solution.ContainsReagent(reagentId))
                return false;

            Solution.RemoveReagent(reagentId, quantity);
            ChemicalsRemoved();
            return true;
        }

        /// <summary>
        ///     Removes part of the solution in the container.
        /// </summary>
        /// <param name="quantity">the volume of solution to remove.</param>
        /// <returns>The solution that was removed.</returns>
        public Solution SplitSolution(ReagentUnit quantity)
        {
            var splitSol = Solution.SplitSolution(quantity);
            ChemicalsRemoved();
            return splitSol;
        }

        /// <summary>
        ///     Checks if a solution can fit into the container.
        /// </summary>
        /// <param name="solution">The solution that is trying to be added.</param>
        /// <returns>If the solution can be fully added.</returns>
        public bool CanAddSolution(Solution solution)
        {
            return solution.TotalVolume <= EmptyVolume;
        }

        /// <summary>
        ///     Adds a solution to the container, if it can fully fit.
        /// </summary>
        /// <param name="solution">The solution to try to add.</param>
        /// <returns>If the solution could be added.</returns>
        public bool TryAddSolution(Solution solution)
        {
            if (!CanAddSolution(solution))
                return false;

            Solution.AddSolution(solution);
            ChemicalsAdded();
            return true;
        }

        private void ChemicalsAdded()
        {
            ProcessReactions();
            SolutionChanged();
            UpdateAppearance();
            Dirty();
        }

        private void ChemicalsRemoved()
        {
            SolutionChanged();
            UpdateAppearance();
            Dirty();
        }

        private void SolutionChanged()
        {
            EntitySystem.Get<ChemistrySystem>().HandleSolutionChange(Owner);
        }

        private void ProcessReactions()
        {
            if (!CanReact)
                return;

            EntitySystem.Get<SharedChemicalReactionSystem>()
                .FullyReactSolution(Solution, Owner, MaxVolume);
        }

        void IExamine.Examine(FormattedMessage message, bool inDetailsRange)
        {
            if (!CanExamineContents)
                return;

            var prototypeManager = IoCManager.Resolve<IPrototypeManager>();

            if (ReagentList.Count == 0)
            {
                message.AddText(Loc.GetString("Contains no chemicals."));
                return;
            }

            var primaryReagent = Solution.GetPrimaryReagentId();
            if (!prototypeManager.TryIndex(primaryReagent, out ReagentPrototype proto))
            {
                Logger.Error($"{nameof(SharedSolutionContainerComponent)} could not find the prototype associated with {primaryReagent}.");
                return;
            }

            var colorHex = Color.ToHexNoAlpha(); //TODO: If the chem has a dark color, the examine text becomes black on a black background, which is unreadable.
            var messageString = "It contains a [color={0}]{1}[/color] " + (ReagentList.Count == 1 ? "chemical." : "mixture of chemicals.");

            message.AddMarkup(Loc.GetString(messageString, colorHex, Loc.GetString(proto.PhysicalDescription)));
        }

        private void UpdateAppearance()
        {
            if (Owner.Deleted || !Owner.TryGetComponent<SharedAppearanceComponent>(out var appearance))
                return;

            appearance.SetData(SolutionContainerVisuals.VisualState, GetVisualState());
        }

        private SolutionContainerVisualState GetVisualState()
        {
            var filledVolumeFraction = CurrentVolume.Float() / MaxVolume.Float();

            return new SolutionContainerVisualState(Color, filledVolumeFraction);
        }

        public override ComponentState GetComponentState()
        {
            return new SolutionContainerComponentState(Solution);
        }

        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            if (curState is not SolutionContainerComponentState containerState)
                return;

            Solution = containerState.Solution;
        }
    }

    [Serializable, NetSerializable]
    public enum SolutionContainerVisuals : byte
    {
        VisualState
    }

    [Serializable, NetSerializable]
    public class SolutionContainerVisualState
    {
        public readonly Color Color;

        /// <summary>
        ///     Represents how full the container is, as a fraction equivalent to <see cref="FilledVolumeFraction"/>/<see cref="byte.MaxValue"/>.
        /// </summary>
        public readonly byte FilledVolumeFraction;

        /// <param name="filledVolumeFraction">The fraction of the container's volume that is filled.</param>
        public SolutionContainerVisualState(Color color, float filledVolumeFraction)
        {
            Color = color;
            FilledVolumeFraction = (byte) (byte.MaxValue * filledVolumeFraction);
        }
    }

    [Serializable, NetSerializable]
    public class SolutionContainerComponentState : ComponentState
    {
        public readonly Solution Solution;

        public SolutionContainerComponentState(Solution solution) : base(ContentNetIDs.SOLUTION)
        {
            Solution = solution;
        }
    }
}
