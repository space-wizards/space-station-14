using System;
using Content.Shared.FixedPoint;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Animals.Components
{
    [RegisterComponent]
    internal class UdderComponent : Component
    {
        public override string Name => "Udder";

        /// <summary>
        ///     The reagent to produce.
        /// </summary>
        [ViewVariables(VVAccess.ReadOnly)]
        [DataField("reagentId", serverOnly: true)]
        public string ReagentId = "Milk";

        /// <summary>
        ///     The solution to add reagent to.
        /// </summary>
        [ViewVariables(VVAccess.ReadOnly)]
        [DataField("targetSolution", serverOnly: true)]
        public string TargetSolutionName = "udder";

        /// <summary>
        ///     The amount of reagent to be generated on update.
        /// </summary>
        [ViewVariables(VVAccess.ReadOnly)]
        [DataField("quantity", serverOnly: true)]
        public FixedPoint2 QuantityPerUpdate = 1;

        /// <summary>
        ///     The time between updates (in seconds).
        /// </summary>
        [ViewVariables(VVAccess.ReadOnly)]
        [DataField("updateRate", serverOnly: true)]
        public float UpdateRate = 5;

        public float AccumulatedFrameTime;
    }
}
