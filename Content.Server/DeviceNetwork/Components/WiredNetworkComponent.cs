namespace Content.Server.DeviceNetwork.Components
{
    [RegisterComponent]
    [ComponentProtoName("WiredNetworkConnection")]
    public sealed partial class WiredNetworkComponent : Component
    {
        /// <summary>
        /// Indicates if the device can connect to the WiredNetwork off grid.
        /// Both devices need this to be true to connect off grid.
        /// </summary>
        [ViewVariables]
        [DataField("connectsOffGrid")]
        public bool ConnectsOffGrid;
    }
}
