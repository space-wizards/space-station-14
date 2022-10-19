using Content.Server.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Chemistry.Components.SolutionManager
{
    /// <summary>
    ///     Reagents that can be added easily. For example like
    ///     pouring something into another beaker, glass, or into the gas
    ///     tank of a car.
    /// </summary>
    [RegisterComponent]
    public sealed class RefillableSolutionComponent : Component
    {
        /// <summary>
        /// Solution name that can added to easily.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("solution")]
        public string Solution { get; set; } = "default";

        /// <summary>
        /// The maximum amount that can be transferred to the solution at once
        /// </summary>
        [DataField("maxRefill")]
        [ViewVariables(VVAccess.ReadWrite)]
        public FixedPoint2? MaxRefill { get; set; } = null;

        /// <summary>
        /// What method of input, determined by tag, the target will allow
        /// If none given it will accept any input
        /// </summary>
        [ViewVariables]
        [DataField("inputContainer")]
        public string InputContainer = string.Empty;
    }
}
