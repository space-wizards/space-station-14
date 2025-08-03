using Content.Client._Starlight.Managers;
using Content.Shared.Starlight;
using Robust.Client.Console;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Shared.ContentPack;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Client.Administration.Managers;

public sealed class ClientPlayerManager : IClientPlayerRolesManager, IPostInjectInit, ISharedPlayersRoleManager
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IClientNetManager _netMgr = default!;
    [Dependency] private readonly ILogManager _logManager = default!;

    private PlayerData? _playerData;
    private ISawmill _sawmill = default!;

    public event Action? PlayerStatusUpdated;

    public void Initialize()
        => _netMgr.RegisterNetMessage<MsgUpdatePlayerStatus>(UpdateMessageRx);

    private void UpdateMessageRx(MsgUpdatePlayerStatus message)
    {
        var host = IoCManager.Resolve<IClientConsoleHost>();

        _playerData = message.Player;
        _sawmill.Info("Updated player status");

        PlayerStatusUpdated?.Invoke();
        ConGroupUpdated?.Invoke();
    }

    public event Action? ConGroupUpdated;

    void IPostInjectInit.PostInject()
        => _sawmill = _logManager.GetSawmill("admin");

    public PlayerData? GetPlayerData()
        => _playerData;

    public PlayerData? GetPlayerData(EntityUid uid)
        => _player.LocalEntity == uid ? _playerData : null;

    public PlayerData? GetPlayerData(ICommonSession session)
        => _player.LocalUser == session.UserId ? _playerData : null;
}
