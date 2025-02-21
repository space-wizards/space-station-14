using Content.Server.Administration.Managers;
using Content.Server.Chat.Managers;
using Content.Server.EUI;
using Content.Shared.Administration;
using Content.Shared.Database;
using Content.Shared.Eui;

namespace Content.Server.Administration;

public sealed class AsnBanPanelEui : BaseEui
{
    [Dependency] private readonly IBanManager _banManager = default!;
    [Dependency] private readonly IEntityManager _entities = default!;
    [Dependency] private readonly ILogManager _log = default!;
    [Dependency] private readonly IPlayerLocator _playerLocator = default!;
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly IAdminManager _admins = default!;

    private readonly ISawmill _sawmill;

    public AsnBanPanelEui()
    {
        IoCManager.InjectDependencies(this);

        _sawmill = _log.GetSawmill("admin.asnbans_eui");
    }

    public override EuiStateBase GetNewState()
    {
        var hasBan = _admins.HasAdminFlag(Player, AdminFlags.Ban);
        return new AsnBanPanelEuiState(hasBan);
    }

    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        switch (msg)
        {
            case AsnBanPanelEuiStateMsg.CreateAsnBanRequest r:
                BanAsn(r.Asn, r.Minutes, r.Severity, r.Reason);
                break;
        }
    }

    private async void BanAsn(string asn, uint? minutes, NoteSeverity severity, string reason)
    {
        if (!_admins.HasAdminFlag(Player, AdminFlags.Ban))
        {
            _sawmill.Warning($"{Player.Name} ({Player.UserId}) tried to create an ASN ban with no ban flag");
            return;
        }

        _banManager.CreateAsnBan(asn, Player.UserId, minutes, severity, reason);

        Close();
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
