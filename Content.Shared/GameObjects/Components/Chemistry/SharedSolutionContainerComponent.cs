#nullable enable
using Content.Shared.Chemistry;
using Content.Shared.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.Appearance;
using Robust.Shared.Interfaces.GameObjects;
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
    public abstract class SharedSolutionContainerComponent : Component, IExamine
    {
        public override string Name => "SolutionContainer";

        /// <inheritdoc />
        public sealed override uint? NetID => ContentNetIDs.SOLUTION;

        [ViewVariables]
        public Solution Solution { get => _solution; set => _solution = value; }
        private Solution _solution = new();

        public IReadOnlyList<Solution.ReagentQuantity> ReagentList => Solution.Contents;

        [ViewVariables(VVAccess.ReadWrite)]
        public ReagentUnit MaxVolume { get => _maxVolume; set => _maxVolume = value; }
        private ReagentUnit _maxVolume;

        [ViewVariables]
        public ReagentUnit CurrentVolume => Solution.TotalVolume;

        [ViewVariables]
        public ReagentUnit EmptyVolume => MaxVolume - CurrentVolume;

        [ViewVariables(VVAccess.ReadWrite)]
        public virtual Color Color { get => _color; set => _color = value; }
        private Color _color;

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
            Solution.RemoveAllSolution();
        }

        public bool TryAddReagent(string reagentId, ReagentUnit quantity, out ReagentUnit acceptedQuantity)
        {
            acceptedQuantity = EmptyVolume > quantity ? quantity : EmptyVolume;
            Solution.AddReagent(reagentId, acceptedQuantity);
            CheckForReaction();
            return acceptedQuantity == quantity;
        }

        public bool TryRemoveReagent(string reagentId, ReagentUnit quantity)
        {
            if (!Solution.ContainsReagent(reagentId))
                return false;

            Solution.RemoveReagent(reagentId, quantity);
            return true;
        }

        public Solution SplitSolution(ReagentUnit quantity)
        {
            return Solution.SplitSolution(quantity);
        }

        public bool CanAddSolution(Solution solution)
        {
            return solution.TotalVolume <= MaxVolume - Solution.TotalVolume;
        }

        public bool TryAddSolution(Solution solution)
        {
            if (!CanAddSolution(solution))
                return false;

            Solution.AddSolution(solution);
            CheckForReaction();
            return true;
        }

        private void CheckForReaction()
        {
            if (!CanReact)
                return;

            IoCManager.Resolve<IEntitySystemManager>()
                .GetEntitySystem<ChemicalReactionSystem>()
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
            if (!Owner.TryGetComponent<SharedAppearanceComponent>(out var appearance))
                return;

            appearance.SetData(SolutionContainerVisuals.VisualState, VisualState);
        }

        private SolutionContainerVisualState VisualState => new SolutionContainerVisualState(Color);
    }

    [Serializable, NetSerializable]
    public enum SolutionContainerVisuals
    {
        VisualState
    }

    [Serializable, NetSerializable]
    public class SolutionContainerVisualState
    {
        public readonly Color Color;

        public SolutionContainerVisualState(Color color)
        {
            Color = color;
        }
    }
}
