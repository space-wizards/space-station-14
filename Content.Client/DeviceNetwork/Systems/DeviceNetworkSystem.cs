using Content.Shared.DeviceNetwork.Systems;
using Robust.Shared.Player;
namespace Content.Client.DeviceNetwork.Systems;

public sealed partial class DeviceNetworkSystem : SharedDeviceNetworkSystem
{
    [Dependency] private ISharedPlayerManager _playerMan = default!;

    public override void Initialize()
    {
        base.Initialize();
        _playerMan.PlayerStatusChanged += PlayerManOnPlayerStatusChanged;
    }

    private void PlayerManOnPlayerStatusChanged(object? sender, SessionStatusEventArgs e)
    {
        PostInit();
    }
}
