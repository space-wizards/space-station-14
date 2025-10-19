using System.Net;
using System.Net.Sockets;
using Content.Server.Administration.Managers;
using Content.Server.Administration.Systems;
using Content.Server.Chat.Managers;
using Content.Server.EUI;
using Content.Shared.Administration;
using Content.Shared.Database;
using Content.Shared.Eui;
using Robust.Shared.Network;

namespace Content.Server.Administration;

public sealed class BanPanelEui : BaseEui
{
    [Dependency] private readonly IBanManager _banManager = default!;
    [Dependency] private readonly IEntityManager _entities = default!;
    [Dependency] private readonly ILogManager _log = default!;
    [Dependency] private readonly IPlayerLocator _playerLocator = default!;
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly IAdminManager _admins = default!;

    private readonly ISawmill _sawmill;

    private NetUserId? PlayerId { get; set; }
    private string PlayerName { get; set; } = string.Empty;
    private IPAddress? LastAddress { get; set; }
    private ImmutableTypedHwid? LastHwid { get; set; }
    private const int Ipv4_CIDR = 32;
    private const int Ipv6_CIDR = 64;

    public BanPanelEui()
    {
        IoCManager.InjectDependencies(this);

        _sawmill = _log.GetSawmill("admin.bans_eui");
    }

    public override EuiStateBase GetNewState()
    {
        var hasBan = _admins.HasAdminFlag(Player, AdminFlags.Ban);
        return new BanPanelEuiState(PlayerName, hasBan);
    }

    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        switch (msg)
        {
            case BanPanelEuiStateMsg.CreateBanRequest r:
                BanPlayer(r.Ban);
                break;
            case BanPanelEuiStateMsg.GetPlayerInfoRequest r:
                ChangePlayer(r.PlayerUsername);
                break;
        }
    }

    private async void BanPlayer(Ban ban)
    {
        if (!_admins.HasAdminFlag(Player, AdminFlags.Ban))
        {
            _sawmill.Warning($"{Player.Name} ({Player.UserId}) tried to create a ban with no ban flag");

            return;
        }

        if (ban.Target == null && string.IsNullOrWhiteSpace(ban.IpAddress) && ban.Hwid == null)
        {
            _chat.DispatchServerMessage(Player, Loc.GetString("ban-panel-no-data"));

            return;
        }

        (IPAddress, int)? addressRange = null;
        if (ban.IpAddress is not null)
        {
            if (!IPAddress.TryParse(ban.IpAddress, out var ipAddress) || !uint.TryParse(ban.IpAddressHid, out var hidInt) || hidInt > Ipv6_CIDR || hidInt > Ipv4_CIDR && ipAddress.AddressFamily == AddressFamily.InterNetwork)
            {
                _chat.DispatchServerMessage(Player, Loc.GetString("ban-panel-invalid-ip"));
                return;
            }

            if (hidInt == 0)
                hidInt = (uint) (ipAddress.AddressFamily == AddressFamily.InterNetworkV6 ? Ipv6_CIDR : Ipv4_CIDR);

            addressRange = (ipAddress, (int) hidInt);
        }

        var targetUid = ban.Target is not null ? PlayerId : null;
        addressRange = ban.UseLastIp && LastAddress is not null ? (LastAddress, LastAddress.AddressFamily == AddressFamily.InterNetworkV6 ? Ipv6_CIDR : Ipv4_CIDR) : addressRange;
        var targetHWid = ban.UseLastHwid ? LastHwid : ban.Hwid;
        if (ban.Target != null && ban.Target != PlayerName || Guid.TryParse(ban.Target, out var parsed) && parsed != PlayerId)
        {
            var located = await _playerLocator.LookupIdByNameOrIdAsync(ban.Target);
            if (located == null)
            {
                _chat.DispatchServerMessage(Player, Loc.GetString("cmd-ban-player"));
                return;
            }
            targetUid = located.UserId;
            var targetAddress = located.LastAddress;
            if (ban.UseLastIp && targetAddress != null)
            {
                if (targetAddress.IsIPv4MappedToIPv6)
                    targetAddress = targetAddress.MapToIPv4();

                // Ban /64 for IPv6, /32 for IPv4.
                var hid = targetAddress.AddressFamily == AddressFamily.InterNetworkV6 ? Ipv6_CIDR : Ipv4_CIDR;
                addressRange = (targetAddress, hid);
            }
            targetHWid = ban.UseLastHwid ? located.LastHWId : ban.Hwid;
        }

        if (ban.BannedJobs?.Length > 0 || ban.BannedAntags?.Length > 0)
        {
            var now = DateTimeOffset.UtcNow;
            foreach (var role in ban.BannedJobs ?? [])
            {
                _banManager.CreateRoleBan(
                    targetUid,
                    ban.Target,
                    Player.UserId,
                    addressRange,
                    targetHWid,
                    role,
                    ban.BanDurationMinutes,
                    ban.Severity,
                    ban.Reason,
                    now
                );
            }

            foreach (var role in ban.BannedAntags ?? [])
            {
                _banManager.CreateRoleBan(
                    targetUid,
                    ban.Target,
                    Player.UserId,
                    addressRange,
                    targetHWid,
                    role,
                    ban.BanDurationMinutes,
                    ban.Severity,
                    ban.Reason,
                    now
                );
            }

            Close();

            return;
        }

        if (ban.Erase && targetUid is not null)
        {
            try
            {
                if (_entities.TrySystem(out AdminSystem? adminSystem))
                    adminSystem.Erase(targetUid.Value);
            }
            catch (Exception e)
            {
                _sawmill.Error($"Error while erasing banned player:\n{e}");
            }
        }

        _banManager.CreateServerBan(
            targetUid,
            ban.Target,
            Player.UserId,
            addressRange,
            targetHWid,
            ban.BanDurationMinutes,
            ban.Severity,
            ban.Reason
        );

        Close();
    }

    public async void ChangePlayer(string playerNameOrId)
    {
        var located = await _playerLocator.LookupIdByNameOrIdAsync(playerNameOrId);
        ChangePlayer(located?.UserId, located?.Username ?? string.Empty, located?.LastAddress, located?.LastHWId);
    }

    public void ChangePlayer(NetUserId? playerId, string playerName, IPAddress? lastAddress, ImmutableTypedHwid? lastHwid)
    {
        PlayerId = playerId;
        PlayerName = playerName;
        LastAddress = lastAddress;
        LastHwid = lastHwid;
        StateDirty();
    }

    public override async void Opened()
    {
        base.Opened();
        _admins.OnPermsChanged += OnPermsChanged;
    }

    public override void Closed()
    {
        base.Closed();
        _admins.OnPermsChanged -= OnPermsChanged;
    }

    private void OnPermsChanged(AdminPermsChangedEventArgs args)
    {
        if (args.Player != Player)
        {
            return;
        }

        StateDirty();
    }
}
