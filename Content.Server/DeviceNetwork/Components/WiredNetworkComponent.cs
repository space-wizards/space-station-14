namespace Content.Server.DeviceNetwork.Components
{
    [RegisterComponent]
    [ComponentProtoName("WiredNetworkConnection")]
    public sealed partial class WiredNetworkComponent : Component
    {
        /// <summary>
        /// Indicates if the device can connect to the WiredNetwork off grid.
        /// Multiple devices need this component to connect off grid to one another.
        /// </summary>
        [DataField]
        public bool ConnectsOffGrid;
    }
}
