namespace Content.Server.DeviceNetwork.Components
{
    [RegisterComponent]
    [ComponentProtoName("WiredNetworkConnection")]
    public sealed partial class WiredNetworkComponent : Component
    {
        [ViewVariables]
        [DataField]
        public bool ConnectsOffGrid;
    }
}
