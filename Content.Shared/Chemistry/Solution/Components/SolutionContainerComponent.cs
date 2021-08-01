using System;
using System.Collections.Generic;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Maths;
using Robust.Shared.Players;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Shared.Chemistry.Solution.Components
{
    /// <summary>
    ///     Holds a <see cref="Solution"/> with a limited volume.
    /// </summary>
    [RegisterComponent]
    [ComponentReference(typeof(ISolutionInteractionsComponent))]
    [NetworkedComponent()]
    public class SolutionContainerComponent : Component, ISolutionInteractionsComponent
    {
        public override string Name => "SolutionContainer";

        [ViewVariables]
        [DataField("contents")]
        public Solution Solution { get; private set; } = new();

        public IReadOnlyList<Solution.ReagentQuantity> ReagentList => Solution.Contents;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("maxVol")]
        public ReagentUnit MaxVolume { get; set; } = ReagentUnit.Zero;

        [ViewVariables] public ReagentUnit CurrentVolume => Solution.TotalVolume;

        /// <summary>
        ///     Volume needed to fill this container.
        /// </summary>
        [ViewVariables]
        public ReagentUnit EmptyVolume => MaxVolume - CurrentVolume;

        [ViewVariables] public virtual Color Color => Solution.Color;

        /// <summary>
        ///     If reactions will be checked for when adding reagents to the container.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("canReact")]
        public bool CanReact { get; set; } = true;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("caps")]
        public Capability Capabilities { get; set; }

        public bool CanExamineContents => Capabilities.HasCap(Capability.CanExamine);

        public bool CanUseWithChemDispenser => Capabilities.HasCap(Capability.FitsInDispenser);

        public bool CanInject => Capabilities.HasCap(Capability.Injectable) || CanRefill;
        public bool CanDraw => Capabilities.HasCap(Capability.Drawable) || CanDrain;

        public bool CanRefill => Capabilities.HasCap(Capability.Refillable);
        public bool CanDrain => Capabilities.HasCap(Capability.Drainable);

        protected override void Initialize()
        {
            base.Initialize();
            UpdateAppearance();
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
            if (!CanAddSolution(solution) || solution.TotalVolume == 0)
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
            Owner.EntityManager.EventBus.RaiseLocalEvent(Owner.Uid, new SolutionChangeEvent(Owner));
        }

        private void ProcessReactions()
        {
            if (!CanReact)
                return;

            EntitySystem.Get<SharedChemicalReactionSystem>()
                .FullyReactSolution(Solution, Owner, MaxVolume);
        }

        public ReagentUnit RefillSpaceAvailable => EmptyVolume;
        public ReagentUnit InjectSpaceAvailable => EmptyVolume;
        public ReagentUnit DrawAvailable => CurrentVolume;
        public ReagentUnit DrainAvailable => CurrentVolume;

        [DataField("maxSpillRefill")] public ReagentUnit MaxSpillRefill { get; set; }

        public void Refill(Solution solution)
        {
            if (!CanRefill)
                return;

            TryAddSolution(solution);
        }

        public void Inject(Solution solution)
        {
            if (!CanInject)
                return;

            TryAddSolution(solution);
        }

        public Solution Draw(ReagentUnit amount)
        {
            if (!CanDraw)
                return new Solution();

            return SplitSolution(amount);
        }

        public Solution Drain(ReagentUnit amount)
        {
            if (!CanDrain)
                return new Solution();

            return SplitSolution(amount);
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

        public override ComponentState GetComponentState(ICommonSession player)
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

        // do we really need this just to save three bytes?
        public float FilledVolumePercent => (float) FilledVolumeFraction / byte.MaxValue;

        /// <param name="filledVolumeFraction">The fraction of the container's volume that is filled.</param>
        public SolutionContainerVisualState(Color color, float filledVolumeFraction)
        {
            Color = color;
            FilledVolumeFraction = (byte) (byte.MaxValue * filledVolumeFraction);
        }
    }

    public enum SolutionContainerLayers : byte
    {
        Fill,
        Base
    }

    [Serializable, NetSerializable]
    public class SolutionContainerComponentState : ComponentState
    {
        public readonly Solution Solution;

        public SolutionContainerComponentState(Solution solution)
        {
            Solution = solution;
        }
    }
}
