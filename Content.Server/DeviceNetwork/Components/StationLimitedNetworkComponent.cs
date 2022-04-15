using Robust.Shared.GameObjects;
using Robust.Shared.Map;

namespace Content.Server.DeviceNetwork.Components
{
    [RegisterComponent]
    public sealed class StationLimitedNetworkComponent : Component
    {
        /// <summary>
        /// The station id the device is limited to.
        /// Uses the grid id until moonys station beacon system is implemented
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public GridId? StationId;
    }
}
