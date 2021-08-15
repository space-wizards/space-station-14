using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Chemistry.Solution.Components;
using Content.Shared.Examine;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using SolutionAlias = Content.Shared.Chemistry.Solution.Solution;


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
    [UsedImplicitly]
    public class SolutionContainerSystem : EntitySystem
    {
        [Dependency] private readonly SharedChemicalReactionSystem _chemistrySystem = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<SolutionContainerManager, ComponentInit>(InitSolution);
            SubscribeLocalEvent<ExaminableSolutionComponent, ExaminedEvent>(OnExamineSolution);
        }

        private void InitSolution(EntityUid uid, SolutionContainerManager component, ComponentInit args)
        {
            foreach (var keyValue in component.Solutions)
            {
                var solutionHolder = keyValue.Value;
                solutionHolder.Owner = component.Owner;
                UpdateAppearance(solutionHolder);
            }
        }

        private void OnExamineSolution(EntityUid uid, ExaminableSolutionComponent examinableComponent,
            ExaminedEvent args)
        {
            if (!args.Examined.TryGetComponent(out SolutionContainerManager? solutionsManager)
                || !solutionsManager.Solutions.TryGetValue(examinableComponent.Solution, out var solutionHolder))
                return;

            if (solutionHolder.Contents.Count == 0)
            {
                args.Message.AddText(Loc.GetString("shared-solution-container-component-on-examine-empty-container"));
                return;
            }

            var primaryReagent = solutionHolder.GetPrimaryReagentId();

            if (!_prototypeManager.TryIndex(primaryReagent, out ReagentPrototype? proto))
            {
                Logger.Error(
                    $"{nameof(SolutionAlias)} could not find the prototype associated with {primaryReagent}.");
                return;
            }

            var colorHex = solutionHolder.Color
                .ToHexNoAlpha(); //TODO: If the chem has a dark color, the examine text becomes black on a black background, which is unreadable.
            var messageString = "shared-solution-container-component-on-examine-main-text";

            args.Message.AddMarkup(Loc.GetString(messageString,
                ("color", colorHex),
                ("wordedAmount", Loc.GetString(solutionHolder.Contents.Count == 1
                    ? "shared-solution-container-component-on-examine-worded-amount-one-reagent"
                    : "shared-solution-container-component-on-examine-worded-amount-multiple-reagents")),
                ("desc", Loc.GetString(proto.PhysicalDescription))));
        }

        private void UpdateAppearance(SolutionAlias solution)
        {
            if (solution.Owner.Deleted
                || !solution.Owner.TryGetComponent<SharedAppearanceComponent>(out var appearance))
                return;

            appearance.SetData(SolutionContainerVisuals.VisualState, solution.GetVisualState());
            solution.Owner.Dirty();
        }

        public void Refill(SolutionAlias solutionHolder, SolutionAlias solution)
        {
            if (!solutionHolder.Owner.HasComponent<RefillableSolutionComponent>())
                return;

            TryAddSolution(solutionHolder, solution);
        }

        public void Inject(SolutionAlias solutionHolder, SolutionAlias solution)
        {
            if (!solutionHolder.Owner.HasComponent<InjectableSolutionComponent>())
                return;

            TryAddSolution(solutionHolder, solution);
        }

        public SolutionAlias Draw(SolutionAlias solutionHolder, ReagentUnit amount)
        {
            if (!solutionHolder.Owner.HasComponent<DrawableSolutionComponent>())
            {
                var newSolution = new SolutionAlias();
                newSolution.Owner = solutionHolder.Owner;
            }

            return SplitSolution(solutionHolder, amount);
        }

        public SolutionAlias Drain(SolutionAlias solutionHolder, ReagentUnit amount)
        {
            if (!solutionHolder.Owner.HasComponent<DrainableSolutionComponent>())
            {
                var newSolution = new SolutionAlias();
                newSolution.Owner = solutionHolder.Owner;
            }

            return SplitSolution(solutionHolder, amount);
        }

        /// <summary>
        ///     Removes part of the solution in the container.
        /// </summary>
        /// <param name="solutionHolder"></param>
        /// <param name="quantity">the volume of solution to remove.</param>
        /// <returns>The solution that was removed.</returns>
        public SolutionAlias SplitSolution(SolutionAlias solutionHolder, ReagentUnit quantity)
        {
            var splitSol = solutionHolder.SplitSolution(quantity);
            splitSol.Owner = solutionHolder.Owner;
            UpdateChemicals(solutionHolder);
            return splitSol;
        }

        private void UpdateChemicals(SolutionAlias solutionHolder, bool needsReactionsProcessing = false)
        {
            // Process reactions
            if (needsReactionsProcessing && solutionHolder.CanReact)
            {
                _chemistrySystem
                    .FullyReactSolution(solutionHolder, solutionHolder.Owner, solutionHolder.MaxVolume);
            }

            UpdateAppearance(solutionHolder);
            RaiseLocalEvent(solutionHolder.Owner.Uid, new SolutionChangedEvent(solutionHolder.Owner));
        }

        public void RemoveAllSolution(SolutionAlias solutionHolder)
        {
            if (solutionHolder.CurrentVolume == 0)
                return;

            solutionHolder.RemoveAllSolution();
            UpdateChemicals(solutionHolder);
        }

        public void RemoveAllSolution(IEntity owner)
        {
            if (!owner.TryGetComponent(out SolutionContainerManager? solutionContainerManager))
                return;

            foreach (var solution in solutionContainerManager.Solutions.Values)
            {
                RemoveAllSolution(solution);
            }
        }

        /// <summary>
        ///     Adds reagent of an Id to the container.
        /// </summary>
        /// <param name="solutionHolder">Container to which we are adding reagent</param>
        /// <param name="reagentId">The Id of the reagent to add.</param>
        /// <param name="quantity">The amount of reagent to add.</param>
        /// <param name="acceptedQuantity">The amount of reagent successfully added.</param>
        /// <returns>If all the reagent could be added.</returns>
        public bool TryAddReagent(SolutionAlias? solutionHolder, string reagentId, ReagentUnit quantity,
            out ReagentUnit acceptedQuantity)
        {
            if (solutionHolder == null)
            {
                acceptedQuantity = ReagentUnit.Zero;
                return false;
            }

            acceptedQuantity = solutionHolder.EmptyVolume > quantity ? quantity : solutionHolder.EmptyVolume;
            solutionHolder.AddReagent(reagentId, acceptedQuantity);

            if (acceptedQuantity > 0)
                UpdateChemicals(solutionHolder, true);

            return acceptedQuantity == quantity;
        }

        /// <summary>
        ///     Removes reagent of an Id to the container.
        /// </summary>
        /// <param name="container">Solution container from which we are removing reagent</param>
        /// <param name="reagentId">The Id of the reagent to remove.</param>
        /// <param name="quantity">The amount of reagent to remove.</param>
        /// <returns>If the reagent to remove was found in the container.</returns>
        public bool TryRemoveReagent(SolutionAlias? container, string reagentId, ReagentUnit quantity)
        {
            if (container == null || !container.ContainsReagent(reagentId))
                return false;

            container.RemoveReagent(reagentId, quantity);
            UpdateChemicals(container);
            return true;
        }

        /// <summary>
        ///     Adds a solution to the container, if it can fully fit.
        /// </summary>
        /// <param name="targetSolution">The container to which we try to add.</param>
        /// <param name="solution">The solution to try to add.</param>
        /// <returns>If the solution could be added.</returns>
        public bool TryAddSolution(SolutionAlias? targetSolution, SolutionAlias solution)
        {
            if (targetSolution == null || !targetSolution.CanAddSolution(solution) || solution.TotalVolume == 0)
                return false;

            targetSolution.AddSolution(solution);
            UpdateChemicals(targetSolution, true);
            return true;
        }

        public bool TryGetInjectableSolution(IEntity owner,
            [NotNullWhen(true)] out SolutionAlias? solution)
        {
            if (owner.TryGetComponent(out InjectableSolutionComponent? injectable) &&
                owner.TryGetComponent(out SolutionContainerManager? manager) &&
                manager.Solutions.TryGetValue(injectable.Solution, out solution))
            {
                return true;
            }

            solution = null;
            return false;
        }

        public SolutionAlias? GetInjectableSolution(IEntity ownerEntity)
        {
            TryGetInjectableSolution(ownerEntity, out var solution);
            return solution;
        }

        public bool TryGetRefillableSolution(IEntity owner,
            [NotNullWhen(true)] out SolutionAlias? solution)
        {
            if (owner.TryGetComponent(out RefillableSolutionComponent? refillable) &&
                owner.TryGetComponent(out SolutionContainerManager? manager) &&
                manager.Solutions.TryGetValue(refillable.Solution, out solution))
            {
                return true;
            }

            solution = null;
            return false;
        }

        public bool TryGetDrainableSolution(IEntity owner,
            [NotNullWhen(true)] out SolutionAlias? solution)
        {
            if (owner.TryGetComponent(out DrainableSolutionComponent? drainable) &&
                owner.TryGetComponent(out SolutionContainerManager? manager) &&
                manager.Solutions.TryGetValue(drainable.Solution, out solution))
            {
                solution.Owner = owner;
                return true;
            }

            solution = null;
            return false;
        }

        public bool TryGetDrawableSolution(IEntity owner,
            [NotNullWhen(true)] out SolutionAlias? solution)
        {
            if (owner.TryGetComponent(out DrawableSolutionComponent? drawable) &&
                owner.TryGetComponent(out SolutionContainerManager? manager) &&
                manager.Solutions.TryGetValue(drawable.Solution, out solution))
            {
                solution.Owner = owner;
                return true;
            }

            solution = null;
            return false;
        }

        public ReagentUnit DrainAvailable(IEntity? owner)
        {
            if (owner == null || !TryGetDrainableSolution(owner, out var solution))
                return ReagentUnit.Zero;

            return solution.CurrentVolume;
        }

        public bool TryGetSolution(IEntity? target, string name,
            [NotNullWhen(true)] out SolutionAlias? solution)
        {
            if (target == null || target.Deleted || !target.TryGetComponent(out SolutionContainerManager? solutionsMgr))
            {
                solution = null;
                return false;
            }

            return solutionsMgr.Solutions.TryGetValue(name, out solution);
        }

        public bool HasFitsInDispenser(IEntity owner)
        {
            return !owner.Deleted && owner.HasComponent<FitsInDispenserComponent>();
        }

        public bool TryGetFitsInDispenser(IEntity owner,
            [NotNullWhen(true)] out SolutionAlias? solution)
        {
            if (owner.TryGetComponent(out FitsInDispenserComponent? dispenserFits) &&
                owner.TryGetComponent(out SolutionContainerManager? manager) &&
                manager.Solutions.TryGetValue(dispenserFits.Solution, out solution))
            {
                return true;
            }

            solution = null;
            return false;
        }

        public bool TryGetDefaultSolution(IEntity? target,
            [NotNullWhen(true)] out SolutionAlias? solution)
        {
            if (target == null
                || target.Deleted || !target.TryGetComponent(out SolutionContainerManager? solutionsMgr)
                || solutionsMgr.Solutions.Count != 1)
            {
                solution = null;
                return false;
            }

            solution = solutionsMgr.Solutions.Values.ToArray()[0];
            return true;
        }

        public SolutionAlias? EnsureSolution(IEntity owner, string name)
        {
            if (owner.Deleted || !owner.TryGetComponent(out SolutionContainerManager? solutionsMgr))
            {
                Logger.Warning($@"Entity (id:{owner.Uid}) is deleted or has no container manager");
                return null;
            }

            if (!solutionsMgr.Solutions.ContainsKey(name))
            {
                var newSolution = new SolutionAlias();
                newSolution.Owner = owner;
                solutionsMgr.Solutions.Add(name, newSolution);
            }
            return solutionsMgr.Solutions[name];
        }


        public bool HasSolution(IEntity owner)
        {
            return !owner.Deleted && owner.HasComponent<SolutionContainerManager>();
        }

        public void AddDrainable(IEntity owner, string name)
        {
            if (owner.Deleted)
                return;

            if (!owner.TryGetComponent(out DrainableSolutionComponent? component))
            {
                component = owner.AddComponent<DrainableSolutionComponent>();
            }

            component.Solution = name;
        }
        public void RemoveDrainable(IEntity owner)
        {
            if (owner.Deleted || !owner.HasComponent<DrainableSolutionComponent>())
                return;

            owner.RemoveComponent<DrainableSolutionComponent>();
        }

        public void AddRefillable(IEntity owner, string name)
        {
            if (owner.Deleted)
                return;

            if (!owner.TryGetComponent(out RefillableSolutionComponent? component))
            {
                component = owner.AddComponent<RefillableSolutionComponent>();
            }

            component.Solution = name;
        }

        public void RemoveRefillable(IEntity owner)
        {
            if (owner.Deleted || !owner.HasComponent<RefillableSolutionComponent>())
                return;

            owner.RemoveComponent<RefillableSolutionComponent>();
        }


        public IList<string> RemoveEachReagent(SolutionAlias solution, ReagentUnit quantity)
        {
            var removedReagent = new List<string>(solution.Contents.Count);
            if (quantity <= 0)
                return removedReagent;

            for (var i = 0; i < solution.Contents.Count; i++)
            {
                var (reagentId, curQuantity) = solution.Contents[i];
                removedReagent.Add(reagentId);

                var newQuantity = curQuantity - quantity;
                if (newQuantity <= 0)
                {
                    solution.Contents.RemoveSwap(i);
                    solution.TotalVolume -= curQuantity;
                }
                else
                {
                    solution.Contents[i] = new SolutionAlias.ReagentQuantity(reagentId, newQuantity);
                    solution.TotalVolume -= quantity;
                }

            }
            return removedReagent;
        }
    }

    public static class SolutionContainerHelpers
    {
        internal static SolutionContainerVisualState GetVisualState(this SolutionAlias component)
        {
            var filledVolumeFraction = component.CurrentVolume.Float() / component.MaxVolume.Float();

            return new SolutionContainerVisualState(component.Color, filledVolumeFraction);
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

        /// <summary>
        ///     Sets the solution state of a container.
        /// </summary>
        /// <param name="color"></param>
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
}
