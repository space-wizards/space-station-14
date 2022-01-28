using System;
using Content.Server.Animals.Systems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.ViewVariables;

namespace Content.Server.Animals.Components
{
    [RegisterComponent, Friend(typeof(UdderSystem))]
    internal class UdderComponent : Component
    {
        public override string Name => "Udder";

        /// <summary>
        ///     The reagent to produce.
        /// </summary>
        [ViewVariables(VVAccess.ReadOnly)]
        [DataField("reagentId", customTypeSerializer:typeof(PrototypeIdSerializer<ReagentPrototype>))]
        public string ReagentId = "Milk";

        /// <summary>
        ///     The solution to add reagent to.
        /// </summary>
        [ViewVariables(VVAccess.ReadOnly)]
        [DataField("targetSolution")]
        public string TargetSolutionName = "udder";

        /// <summary>
        ///     The amount of reagent to be generated on update.
        /// </summary>
        [ViewVariables(VVAccess.ReadOnly)]
        [DataField("quantity")]
        public FixedPoint2 QuantityPerUpdate = 1;

        /// <summary>
        ///     The time between updates (in seconds).
        /// </summary>
        [ViewVariables(VVAccess.ReadOnly)]
        [DataField("updateRate")]
        public float UpdateRate = 5;

        public float AccumulatedFrameTime;

        public bool BeingMilked;
    }
}
