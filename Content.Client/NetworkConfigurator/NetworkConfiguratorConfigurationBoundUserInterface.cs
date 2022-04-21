using JetBrains.Annotations;
using Robust.Client.GameObjects;

namespace Content.Client.NetworkConfigurator;

public sealed class NetworkConfiguratorConfigurationBoundUserInterface : BoundUserInterface
{
    public NetworkConfiguratorConfigurationBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey)
    {
    }
}
