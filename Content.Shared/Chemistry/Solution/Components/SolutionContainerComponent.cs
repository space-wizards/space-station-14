using System;
using System.Collections.Generic;
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
   [NetworkedComponent()]
    public class SolutionContainerComponent : Component
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

        /// <summary>
        ///     Checks if a solution can fit into the container.
        /// </summary>
        /// <param name="solution">The solution that is trying to be added.</param>
        /// <returns>If the solution can be fully added.</returns>
        public bool CanAddSolution(Solution solution)
        {
            return solution.TotalVolume <= EmptyVolume;
        }

        public ReagentUnit RefillSpaceAvailable => EmptyVolume;
        public ReagentUnit InjectSpaceAvailable => EmptyVolume;
        public ReagentUnit DrawAvailable => CurrentVolume;
        public ReagentUnit DrainAvailable => CurrentVolume;

        [DataField("maxSpillRefill")] public ReagentUnit MaxSpillRefill { get; set; }
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
