namespace Content.Server.DeviceNetwork.Components
{
    [RegisterComponent]
    [ComponentProtoName("UniversalNetworkConnection")]
    /// <summary>
    /// Dummy component that gives access to Wired deviceNets, ignoring grid boundaries and dimensions.
    /// </summary>
    public sealed partial class UniversalNetworkComponent : Component
    {
    }
}
