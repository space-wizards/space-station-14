using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
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
                    $"{nameof(Components.Solution)} could not find the prototype associated with {primaryReagent}.");
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

        private void UpdateAppearance(Components.Solution solution)
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
        public Components.Solution SplitSolution(Components.Solution solutionHolder, ReagentUnit quantity)
        {
            var splitSol = solutionHolder.SplitSolution(quantity);
            splitSol.OwnerUid = solutionHolder.OwnerUid;
            UpdateChemicals(solutionHolder);
            return splitSol;
        }

        private void UpdateChemicals(Components.Solution solutionHolder, bool needsReactionsProcessing = false)
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

        public void RemoveAllSolution(Components.Solution solutionHolder)
        {
            if (solutionHolder.CurrentVolume == 0)
                return;

            solutionHolder.RemoveAllSolution();
            UpdateChemicals(solutionHolder);
        }

        public void RemoveAllSolution(IEntity owner)
        {
            if (!owner.TryGetComponent(out SolutionContainerManagerComponent? solutionContainerManager))
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
        public bool TryAddReagent(Components.Solution? solutionHolder, string reagentId, ReagentUnit quantity,
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
        public bool TryRemoveReagent(Components.Solution? container, string reagentId, ReagentUnit quantity)
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
        public bool TryAddSolution(Components.Solution? targetSolution, Components.Solution solution)
        {
            if (targetSolution == null || !targetSolution.CanAddSolution(solution) || solution.TotalVolume == 0)
                return false;

            targetSolution.AddSolution(solution);
            UpdateChemicals(targetSolution, true);
            return true;
        }

        public bool TryGetSolution(IEntity? target, string name,
            [NotNullWhen(true)] out Components.Solution? solution)
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
        public Components.Solution EnsureSolution(IEntity owner, string name)
        {
            if (!owner.TryGetComponent(out SolutionContainerManagerComponent? solutionsMgr))
            {
                solutionsMgr = owner.AddComponent<SolutionContainerManagerComponent>();
            }

            if (!solutionsMgr.Solutions.ContainsKey(name))
            {
                var newSolution = new Components.Solution();
                newSolution.OwnerUid = owner.Uid;
                solutionsMgr.Solutions.Add(name, newSolution);
            }
            return solutionsMgr.Solutions[name];
        }


        public bool HasSolution(IEntity owner)
        {
            return !owner.Deleted && owner.HasComponent<SolutionContainerManagerComponent>();
        }

        public string[] RemoveEachReagent(Components.Solution solution, ReagentUnit quantity)
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
                    solution.Contents[i] = new Components.Solution.ReagentQuantity(reagentId, newQuantity);
                    solution.TotalVolume -= quantity;
                }

            }
            return removedReagent;
        }

        public void TryRemoveAllReagents(Components.Solution solution, List<Components.Solution.ReagentQuantity> removeReagents)
        {
            foreach (var reagent in removeReagents)
            {
                solution.RemoveReagent(reagent.ReagentId, reagent.Quantity);
            }
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
