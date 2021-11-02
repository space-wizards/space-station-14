using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Chemistry.Components.SolutionManager
{
    /// Allows the entity with this component to be placed in a <c>SharedReagentDispenserComponent</c>.
    /// <para>Otherwise it's considered to be too large or the improper shape to fit.</para>
    /// <para>Allows us to have obscenely large containers that are harder to abuse in chem dispensers
    /// since they can't be placed directly in them.</para>
    /// <see cref="Content.Shared.Chemistry.Dispenser.SharedReagentDispenserComponent"/>
    [RegisterComponent]
    public class FitsInDispenserComponent : Component
    {
        public override string Name => "FitsInDispenser";

        /// <summary>
        /// Solution name that will interact with ReagentDispenserComponent.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("solution")]
        public string Solution { get; set; } = "default";
    }
}
