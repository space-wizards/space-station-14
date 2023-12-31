using System.Collections.Immutable;
using System.Net;
using System.Net.Sockets;
using Content.Server.Administration.Managers;
using Content.Server.Administration.Systems;
using Content.Server.Chat.Managers;
using Content.Server.EUI;
using Content.Shared.Administration;
using Content.Shared.Database;
using Content.Shared.Eui;
using Robust.Server.Player;
using Robust.Shared.Network;

namespace Content.Server.Administration;

public sealed class BanPanelEui : BaseEui, IPostInjectInit
{
    [Dependency] private readonly IBanManager _banManager = default!;
    [Dependency] private readonly IEntityManager _entities = default!;
    [Dependency] private readonly ILogManager _log = default!;
    [Dependency] private readonly IPlayerLocator _playerLocator = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly IAdminManager _admins = default!;

    private ISawmill _sawmill = default!;

    private NetUserId? PlayerId { get; set; }
    private string PlayerName { get; set; } = string.Empty;
    private IPAddress? LastAddress { get; set; }
    private ImmutableArray<byte>? LastHwid { get; set; }
    private const int Ipv4_CIDR = 32;
    private const int Ipv6_CIDR = 64;

    public BanPanelEui()
    {
        IoCManager.InjectDependencies(this);
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
                BanPlayer(r.Player, r.IpAddress, r.UseLastIp, r.Hwid?.ToImmutableArray(), r.UseLastHwid, r.Minutes, r.Severity, r.Reason, r.Roles, r.Erase);
                break;
            case BanPanelEuiStateMsg.GetPlayerInfoRequest r:
                ChangePlayer(r.PlayerUsername);
                break;
        }
    }

    private async void BanPlayer(string? target, string? ipAddressString, bool useLastIp, ImmutableArray<byte>? hwid, bool useLastHwid, uint minutes, NoteSeverity severity, string reason, IReadOnlyCollection<string>? roles, bool erase)
    {
        if (!_admins.HasAdminFlag(Player, AdminFlags.Ban))
        {
            _sawmill.Warning($"{Player.Name} ({Player.UserId}) tried to create a ban with no ban flag");
            return;
        }
        if (target == null && string.IsNullOrWhiteSpace(ipAddressString) && hwid == null)
        {
            _chat.DispatchServerMessage(Player, Loc.GetString("ban-panel-no-data"));
            return;
        }

        (IPAddress, int)? addressRange = null;
        if (ipAddressString is not null)
        {
            var hid = "0";
            var split = ipAddressString.Split('/', 2);
            ipAddressString = split[0];
            if (split.Length > 1)
                hid = split[1];

            if (!IPAddress.TryParse(ipAddressString, out var ipAddress) || !uint.TryParse(hid, out var hidInt) || hidInt > Ipv6_CIDR || hidInt > Ipv4_CIDR && ipAddress.AddressFamily == AddressFamily.InterNetwork)
            {
                _chat.DispatchServerMessage(Player, Loc.GetString("ban-panel-invalid-ip"));
                return;
            }

            if (hidInt == 0)
                hidInt = (uint) (ipAddress.AddressFamily == AddressFamily.InterNetworkV6 ? Ipv6_CIDR : Ipv4_CIDR);

            addressRange = (ipAddress, (int) hidInt);
        }

        var targetUid = target is not null ? PlayerId : null;
        addressRange = useLastIp && LastAddress is not null ? (LastAddress, LastAddress.AddressFamily == AddressFamily.InterNetworkV6 ? Ipv6_CIDR : Ipv4_CIDR) : addressRange;
        var targetHWid = useLastHwid ? LastHwid : hwid;
        if (target != null && target != PlayerName || Guid.TryParse(target, out var parsed) && parsed != PlayerId)
        {
            var located = await _playerLocator.LookupIdByNameOrIdAsync(target);
            if (located == null)
            {
                _chat.DispatchServerMessage(Player, Loc.GetString("cmd-ban-player"));
                return;
            }
            targetUid = located.UserId;
            var targetAddress = located.LastAddress;
            if (useLastIp && targetAddress != null)
            {
                if (targetAddress.IsIPv4MappedToIPv6)
                    targetAddress = targetAddress.MapToIPv4();

                // Ban /64 for IPv6, /32 for IPv4.
                var hid = targetAddress.AddressFamily == AddressFamily.InterNetworkV6 ? Ipv6_CIDR : Ipv4_CIDR;
                addressRange = (targetAddress, hid);
            }
            targetHWid = useLastHwid ? located.LastHWId : hwid;
        }

        if (roles?.Count > 0)
        {
            var now = DateTimeOffset.UtcNow;
            foreach (var role in roles)
            {
                _banManager.CreateRoleBan(targetUid, target, Player.UserId, addressRange, targetHWid, role, minutes, severity, reason, now);
            }

            Close();
            return;
        }

        if (erase &&
            targetUid != null &&
            _playerManager.TryGetSessionById(targetUid.Value, out var targetPlayer))
        {
            try
            {
                if (_entities.TrySystem(out AdminSystem? adminSystem))
                    adminSystem.Erase(targetPlayer);
            }
            catch (Exception e)
            {
                _sawmill.Error($"Error while erasing banned player:\n{e}");
            }
        }

        _banManager.CreateServerBan(targetUid, target, Player.UserId, addressRange, targetHWid, minutes, severity, reason);

        Close();
    }

    public async void ChangePlayer(string playerNameOrId)
    {
        var located = await _playerLocator.LookupIdByNameOrIdAsync(playerNameOrId);
        ChangePlayer(located?.UserId, located?.Username ?? string.Empty, located?.LastAddress, located?.LastHWId);
    }

    public void ChangePlayer(NetUserId? playerId, string playerName, IPAddress? lastAddress, ImmutableArray<byte>? lastHwid)
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

    public void PostInject()
    {
        _sawmill = _log.GetSawmill("admin.bans_eui");
    }
}
