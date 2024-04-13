
using Robust.Shared.GameStates;

namespace Content.Shared.Tools.Components
{
    [RegisterComponent, NetworkedComponent, ComponentProtoName("HandheldGPS"), AutoGenerateComponentState]
    public sealed partial class HandheldGpsComponent : Component
    {
        [DataField]
        public float UpdateRate = 1.5f;

        /// <summary>
        /// Display mode: If false displays space coordinates, if true station coordinates
        /// </summary>
        [DataField]
        [AutoNetworkedField]
        public bool DisplayMode;
    }
}
