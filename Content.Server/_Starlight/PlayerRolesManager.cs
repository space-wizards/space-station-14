using System.Threading.Tasks;
using Content.Server.Administration.Managers;
using Content.Server.Database;
using Content.Shared.Starlight;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Server.Starlight;

public sealed partial class PlayerRolesManager : IPlayerRolesManager, IPostInjectInit
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IServerDbManager _dbManager = default!;
    [Dependency] private readonly IServerNetManager _netMgr = default!;

    private readonly Dictionary<ICommonSession, PlayerReg> _players = new();

    public IEnumerable<PlayerReg> Players => _players.Values;

    public void Initialize() 
        => _netMgr.RegisterNetMessage<MsgUpdatePlayerStatus>();

    void IPostInjectInit.PostInject()
        => _playerManager.PlayerStatusChanged += PlayerStatusChanged;

    private void PlayerStatusChanged(object? sender, SessionStatusEventArgs e)
    {
        if (e.NewStatus == SessionStatus.Connected)
            UpdatePlayerStatus(e.Session);
        else if (e.NewStatus == SessionStatus.InGame)
            Login(e.Session);
        else if (e.NewStatus == SessionStatus.Disconnected)
        {
            if(_players.Remove(e.Session, out var data))
            _ = _dbManager.SetPlayerDataForAsync(e.Session.UserId, new PlayerDataDTO
            {
                GhostTheme = data!.Data.GhostTheme,
                Balance = data!.Data.Balance
            });
        }
    }
    private void UpdatePlayerStatus(ICommonSession session)
    {
        var userid = session.UserId;
        var msg = new MsgUpdatePlayerStatus();

        if (_players.TryGetValue(session, out var playerData))
            msg.Player = playerData.Data;

        _netMgr.ServerSendMessage(msg, session.Channel);
    }

    private async void Login(ICommonSession session)
    {
        var adminDat = await LoadPlayerData(session);
        var reg = new PlayerReg(session, adminDat);

        _players.Add(session, reg);

        UpdatePlayerStatus(session);
    }

    private async Task<PlayerData> LoadPlayerData(ICommonSession session)
    {
        var dbData = await _dbManager.GetPlayerDataForAsync(session.UserId);

        if (dbData == null)
        {
            dbData = new PlayerDataDTO
            {
                UserId = session.UserId,
                Balance = 500,
                GhostTheme = "None",
            };
            await _dbManager.SetPlayerDataForAsync(session.UserId, dbData);
        }

        return new PlayerData
        {
            Title = dbData.Title,
            Balance = dbData.Balance,
            GhostTheme = dbData.GhostTheme
        };
    }
    public PlayerData? GetPlayerData(EntityUid uid)
    {
        if (!_playerManager.TryGetSessionByEntity(uid, out var session)) return null;
        return GetPlayerData(session);
    }

    public PlayerData? GetPlayerData(ICommonSession session) => _players.TryGetValue(session, out var data) ? data.Data : null;

    public sealed class PlayerReg(ICommonSession session, PlayerData data)
    {
        public readonly ICommonSession Session = session;

        public PlayerData Data = data;
    }
}
