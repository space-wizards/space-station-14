// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Server.CartridgeLoader.Cartridges;

[RegisterComponent]
public sealed partial class MessengerClientCartridgeComponent : Component
{
    // to reduce the call to onInstall
    public bool IsInstalled = false;

    // EntityUid of loader device
    public EntityUid Loader;

    // List of accessed servers, in future user could select a server to connect
    public Dictionary<EntityUid, ServerInfo> Servers = new();

    // Current active server
    public EntityUid? ActiveServer;

    // When true, component try to send full state of messenger
    public bool SendState;
}

public sealed class ServerInfo
{
    public string Name = "";
    public string Address = "";
}
