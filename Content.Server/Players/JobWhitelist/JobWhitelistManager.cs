using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.Database;
using Content.Shared.CCVar;
using Content.Shared.Players.JobWhitelist;
using Content.Shared.Roles;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.Players.JobWhitelist;

public sealed class JobWhitelistManager : IPostInjectInit
{
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly UserDbDataManager _userDb = default!;
    [Dependency] private readonly ILogManager _logManager = default!;

    private readonly Dictionary<NetUserId, HashSet<string>> _whitelists = new();
    private ISawmill _sawmill = default!;

    public void Initialize()
    {
        _net.RegisterNetMessage<MsgJobWhitelist>();
    }

    private async Task LoadData(ICommonSession session, CancellationToken cancel)
    {
        var whitelists = await _db.GetJobWhitelists(session.UserId, cancel);
        cancel.ThrowIfCancellationRequested();
        _whitelists[session.UserId] = whitelists.ToHashSet();
    }

    private void FinishLoad(ICommonSession session)
    {
        SendJobWhitelist(session);
    }

    private void ClientDisconnected(ICommonSession session)
    {
        _whitelists.Remove(session.UserId);
    }

    public async void AddWhitelist(NetUserId player, ProtoId<JobPrototype> job)
    {
        if (_whitelists.TryGetValue(player, out var whitelists))
            whitelists.Add(job);

        await _db.AddJobWhitelist(player, job);

        if (_player.TryGetSessionById(player, out var session))
            SendJobWhitelist(session);
    }

    /// <summary>
    /// Returns false if role whitelist is required but the player does not have it.
    /// </summary>
    public bool IsAllowed(ICommonSession session, ProtoId<JobPrototype> job)
    {
        if (!_config.GetCVar(CCVars.GameRoleWhitelist))
            return true;

        if (!_prototypes.Resolve(job, out var jobPrototype) ||
            !jobPrototype.Whitelisted)
        {
            return true;
        }

        return IsWhitelisted(session.UserId, job);
    }

    public bool IsWhitelisted(NetUserId player, ProtoId<JobPrototype> job)
    {
        if (!_whitelists.TryGetValue(player, out var whitelists))
        {
            _sawmill.Error("Unable to check if player {Player} is whitelisted for {Job}. Stack trace:\\n{StackTrace}",
                player,
                job,
                Environment.StackTrace);
            return false;
        }

        return whitelists.Contains(job);
    }

    public async void RemoveWhitelist(NetUserId player, ProtoId<JobPrototype> job)
    {
        _whitelists.GetValueOrDefault(player)?.Remove(job);
        await _db.RemoveJobWhitelist(player, job);

        if (_player.TryGetSessionById(new NetUserId(player), out var session))
            SendJobWhitelist(session);
    }

    public void SendJobWhitelist(ICommonSession player)
    {
        var msg = new MsgJobWhitelist
        {
            Whitelist = _whitelists.GetValueOrDefault(player.UserId) ?? new HashSet<string>()
        };

        _net.ServerSendMessage(msg, player.Channel);
    }

    void IPostInjectInit.PostInject()
    {
        _userDb.AddOnLoadPlayer(LoadData);
        _userDb.AddOnFinishLoad(FinishLoad);
        _userDb.AddOnPlayerDisconnect(ClientDisconnected);
        _sawmill = _logManager.GetSawmill("job_whitelist");
    }
}
