using Content.Shared.Atmos;

namespace Content.Server.Atmos.Piping.Unary.Components
{
    [RegisterComponent]
    public sealed partial class PipeFillComponent : Component, IGasMixtureHolder
    {
        /// <summary>
        /// The node to add gas to at component initialization.
        /// </summary>
        [ViewVariables]
        [DataField]
        public string NodeName { get; set; } = "tank";


        /// <summary>
        /// The gas mixture to be added to the pipe node / network on component initialization.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField]
        public GasMixture Air { get; set; } = new();
    }
}
