using Robust.Shared.GameStates;

namespace Content.Server.CartridgeLoader.Cartridges;

[RegisterComponent, NetworkedComponent]
public sealed partial class OrderLogCartridgeComponent : Component
{
    [DataField]
    public bool NotificationOn = true;
}

