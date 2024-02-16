namespace Content.Client.NetworkConfigurator;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent]
public sealed partial class NetworkConfiguratorActiveLinkOverlayComponent : Component
{
    /// <summary>
    ///     The entities linked to this network configurator.
    ///     This could just... couldn't this just be grabbed
    ///     if DeviceList was shared?
    /// </summary>
    public HashSet<EntityUid> Devices = new();
}
