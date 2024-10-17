using Content.Shared.Atmos;

namespace Content.Server.Atmos.Piping.Unary.Components
{
    [RegisterComponent]
    public sealed partial class PipeFillComponent : Component
    {
        /// <summary>
        /// Dictionary of nodes on a NodeContainer which are Pipe nodes and their associated saved GasMixtures.
        /// </summary>
        [ViewVariables]
        [DataField]
        public Dictionary<string, GasMixture> AirDict { get; set; } = new();

        /// <summary>
        /// Set true after it fires to prevent readding gas. Set false before saving map to ensure gas is readded on load.
        /// </summary>
        [ViewVariables]
        [DataField]
        public bool HasFired = false;
    }
}
