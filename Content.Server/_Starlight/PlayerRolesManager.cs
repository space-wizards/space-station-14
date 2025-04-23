using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Content.Server.Administration.Managers;
using Content.Server.Chat.Managers;
using Content.Server.Database;
using Content.Shared.Starlight;
using Content.Shared.Starlight;
using Content.Shared.Starlight.CCVar;
using Robust.Server.Console;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.Enums;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Toolshed;

namespace Content.Server.Starlight;

public sealed partial class PlayerRolesManager : IPlayerRolesManager, IPostInjectInit
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IServerDbManager _dbManager = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IServerNetManager _netMgr = default!;
    [Dependency] private readonly IConGroupController _conGroup = default!;
    [Dependency] private readonly IResourceManager _res = default!;
    [Dependency] private readonly IServerConsoleHost _consoleHost = default!;
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly ToolshedManager _toolshed = default!;
    [Dependency] private readonly ILogManager _logManager = default!;

    private readonly Dictionary<ICommonSession, PlayerReg> _players = new();

    public IEnumerable<ICommonSession> Mentors => _players
        .Where(p => p.Value.Data.Flags.HasFlag(PlayerFlags.Mentor))
        .Select(p => p.Key);

    public IEnumerable<PlayerReg> Players => _players.Values;

    private ISawmill _sawmill = default!;
    private string? _discordKey;
    private string? _discordCallback;
    private string? _secret;
    public void Initialize()
    {
        _netMgr.RegisterNetMessage<MsgUpdatePlayerStatus>();
        _sawmill = _logManager.GetSawmill("player roles");
        _discordKey = _cfg.GetCVar(StarlightCCVars.DiscordKey);
        _discordCallback = _cfg.GetCVar(StarlightCCVars.DiscordCallback);
        _secret = _cfg.GetCVar(StarlightCCVars.Secret);
    }

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
        var msg = new MsgUpdatePlayerStatus()
        {
            DiscordLink = GetDiscordAuthUrl(userid.ToString())
        };

        if (_players.TryGetValue(session, out var playerData))
            msg.Player = playerData.Data;

        _netMgr.ServerSendMessage(msg, session.Channel);
    }
    private string GetDiscordAuthUrl(string customState)
    {
        if (string.IsNullOrEmpty(_discordCallback) || string.IsNullOrEmpty(_discordKey) || string.IsNullOrEmpty(_secret)) return "";
        var scope = "identify%20guilds%20guilds.members.read";
        var secretKeyBytes = Encoding.UTF8.GetBytes(_secret);
        using var hmac = new HMACSHA256(secretKeyBytes);

        var dataBytes = Encoding.UTF8.GetBytes(customState);
        var hashBytes = hmac.ComputeHash(dataBytes);
        var state = $"{customState}|{BitConverter.ToString(hashBytes).Replace("-", "").ToLower()}";
        var encodedState = Uri.EscapeDataString(state);

        return $"https://discord.com/api/oauth2/authorize?client_id={_discordKey}&redirect_uri={Uri.EscapeDataString(_discordCallback)}&response_type=code&scope={scope}&state={encodedState}";
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
                Flags = 0,
                GhostTheme = "None",
            };
            await _dbManager.SetPlayerDataForAsync(session.UserId, dbData);
        }

        return new PlayerData
        {
            Flags = (PlayerFlags)dbData.Flags,
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

    private const int ALL_ROLES = (int)PlayerFlags.Staff | (int)PlayerFlags.Retiree | (int)PlayerFlags.AlfaTester | (int)PlayerFlags.Mentor | (int)PlayerFlags.AllRoles;
    public bool IsAllRolesAvailable(ICommonSession session) => _players.TryGetValue(session, out var data) && ((int)data.Data.Flags & ALL_ROLES) != 0;

    public sealed class PlayerReg(ICommonSession session, PlayerData data)
    {
        public readonly ICommonSession Session = session;

        public PlayerData Data = data;
    }
}
