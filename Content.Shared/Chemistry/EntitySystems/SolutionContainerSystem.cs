using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Chemistry.Reagent;
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

namespace Content.Shared.Chemistry.EntitySystems
{
    /// <summary>
    /// This event alerts system that the solution was changed
    /// </summary>
    public class SolutionChangedEvent : EntityEventArgs
    {
        public EntityUid Owner { get; }

        public SolutionChangedEvent(EntityUid owner)
        {
            Owner = owner;
        }
    }

    /// <summary>
    /// Part of Chemistry system deal with SolutionContainers
    /// </summary>
    [UsedImplicitly]
    public partial class SolutionContainerSystem : EntitySystem
    {
        [Dependency] private readonly SharedChemicalReactionSystem _chemistrySystem = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<SolutionContainerManagerComponent, ComponentInit>(InitSolution);
            SubscribeLocalEvent<ExaminableSolutionComponent, ExaminedEvent>(OnExamineSolution);
        }

        private void InitSolution(EntityUid uid, SolutionContainerManagerComponent component, ComponentInit args)
        {
            foreach (var keyValue in component.Solutions)
            {
                var solutionHolder = keyValue.Value;
                solutionHolder.OwnerUid = component.Owner.Uid;
                UpdateAppearance(solutionHolder);
            }
        }

        private void OnExamineSolution(EntityUid uid, ExaminableSolutionComponent examinableComponent,
            ExaminedEvent args)
        {
            if (!args.Examined.TryGetComponent(out SolutionContainerManagerComponent? solutionsManager)
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
                    $"{nameof(Solution)} could not find the prototype associated with {primaryReagent}.");
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

        private void UpdateAppearance(Solution solution)
        {
            if (!_entityManager.TryGetEntity(solution.OwnerUid, out var solutionEntity)
                || solutionEntity.Deleted
                || !solutionEntity.TryGetComponent<SharedAppearanceComponent>(out var appearance))
                return;

            var filledVolumeFraction = solution.CurrentVolume.Float() / solution.MaxVolume.Float();
            appearance.SetData(SolutionContainerVisuals.VisualState, new SolutionContainerVisualState(solution.Color, filledVolumeFraction));
            solutionEntity.Dirty();
        }

        /// <summary>
        ///     Removes part of the solution in the container.
        /// </summary>
        /// <param name="solutionHolder"></param>
        /// <param name="quantity">the volume of solution to remove.</param>
        /// <returns>The solution that was removed.</returns>
        public Solution SplitSolution(Solution solutionHolder, ReagentUnit quantity)
        {
            var splitSol = solutionHolder.SplitSolution(quantity);
            splitSol.OwnerUid = solutionHolder.OwnerUid;
            UpdateChemicals(solutionHolder);
            return splitSol;
        }

        private void UpdateChemicals(Solution solutionHolder, bool needsReactionsProcessing = false)
        {
            // Process reactions
            if (needsReactionsProcessing && solutionHolder.CanReact)
            {
                _chemistrySystem
                    .FullyReactSolution(solutionHolder, _entityManager.GetEntity(solutionHolder.OwnerUid), solutionHolder.MaxVolume);
            }

            UpdateAppearance(solutionHolder);
            RaiseLocalEvent(solutionHolder.OwnerUid, new SolutionChangedEvent(solutionHolder.OwnerUid));
        }

        public void RemoveAllSolution(Solution solutionHolder)
        {
            if (solutionHolder.CurrentVolume == 0)
                return;

            solutionHolder.RemoveAllSolution();
            UpdateChemicals(solutionHolder);
        }

        public void RemoveAllSolution(EntityUid uid)
        {
            if (!_entityManager.TryGetEntity(uid, out var owner)
                || !owner.TryGetComponent(out SolutionContainerManagerComponent? solutionContainerManager))
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
        public bool TryAddReagent(Solution? solutionHolder, string reagentId, ReagentUnit quantity,
            out ReagentUnit acceptedQuantity)
        {
            if (solutionHolder == null)
            {
                acceptedQuantity = ReagentUnit.Zero;
                return false;
            }

            acceptedQuantity = solutionHolder.AvailableVolume > quantity ? quantity : solutionHolder.AvailableVolume;
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
        public bool TryRemoveReagent(Solution? container, string reagentId, ReagentUnit quantity)
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
        public bool TryAddSolution(Solution? targetSolution, Solution solution)
        {
            if (targetSolution == null || !targetSolution.CanAddSolution(solution) || solution.TotalVolume == 0)
                return false;

            targetSolution.AddSolution(solution);
            UpdateChemicals(targetSolution, true);
            return true;
        }

        public bool TryGetSolution(IEntity? target, string name,
            [NotNullWhen(true)] out Solution? solution)
        {
            if (target == null || target.Deleted || !target.TryGetComponent(out SolutionContainerManagerComponent? solutionsMgr))
            {
                solution = null;
                return false;
            }

            return solutionsMgr.Solutions.TryGetValue(name, out solution);
        }

        /// <summary>
        /// Will ensure a solution is added to given entity even if it's missing solutionContainerManager
        /// </summary>
        /// <param name="owner">Entity to which to add solution</param>
        /// <param name="name">name for the solution</param>
        /// <returns>solution</returns>
        public Solution EnsureSolution(IEntity owner, string name)
        {
            if (!owner.TryGetComponent(out SolutionContainerManagerComponent? solutionsMgr))
            {
                solutionsMgr = owner.AddComponent<SolutionContainerManagerComponent>();
            }

            if (!solutionsMgr.Solutions.ContainsKey(name))
            {
                var newSolution = new Solution();
                newSolution.OwnerUid = owner.Uid;
                solutionsMgr.Solutions.Add(name, newSolution);
            }
            return solutionsMgr.Solutions[name];
        }

        public string[] RemoveEachReagent(Solution solution, ReagentUnit quantity)
        {
            var removedReagent = new string[solution.Contents.Count];
            if (quantity <= 0)
                return Array.Empty<string>();

            var pos = 0;
            for (var i = 0; i < solution.Contents.Count; i++)
            {
                var (reagentId, curQuantity) = solution.Contents[i];
                removedReagent[pos++] = reagentId;

                var newQuantity = curQuantity - quantity;
                if (newQuantity <= 0)
                {
                    solution.Contents.RemoveSwap(i);
                    solution.TotalVolume -= curQuantity;
                }
                else
                {
                    solution.Contents[i] = new Solution.ReagentQuantity(reagentId, newQuantity);
                    solution.TotalVolume -= quantity;
                }

            }
            return removedReagent;
        }

        public void TryRemoveAllReagents(Solution solution, List<Solution.ReagentQuantity> removeReagents)
        {
            foreach (var reagent in removeReagents)
            {
                solution.RemoveReagent(reagent.ReagentId, reagent.Quantity);
            }
        }

        public ReagentUnit GetReagentQuantity(EntityUid ownerUid, string reagentId)
        {
            var reagentQuantity = ReagentUnit.New(0);
            if (_entityManager.TryGetEntity(ownerUid, out var owner)
                && owner.TryGetComponent(out SolutionContainerManagerComponent? managerComponent))
            {
                foreach (var solution in managerComponent.Solutions.Values)
                {
                    reagentQuantity += solution.GetReagentQuantity(reagentId);
                }
            }

            return reagentQuantity;
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
