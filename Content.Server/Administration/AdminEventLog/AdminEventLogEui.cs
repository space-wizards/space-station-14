using Content.Server.Administration.Managers;
using Content.Server.EUI;
using Content.Server.GameTicking;
using Content.Shared.Administration;
using Content.Shared.Administration.AdminEventLog;
using Content.Shared.Eui;

namespace Content.Server.Administration.AdminEventLog;

public sealed partial class AdminEventLogEui : BaseEui
{
    [Dependency] private IAdminManager _adminManager = default!;
    [Dependency] private IEntityManager _e = default!;

    public AdminEventLogEui()
    {
        IoCManager.InjectDependencies(this);
    }

    public override async void Opened()
    {
        base.Opened();

        _adminManager.OnPermsChanged += OnPermsChanged;
    }

    private int CurrentRoundId => _e.System<GameTicker>().RoundId;

    private void OnPermsChanged(AdminPermsChangedEventArgs args)
    {
        if (args.Player == Player && !_adminManager.HasAdminFlag(Player, AdminFlags.Admin))
        {
            Close();
        }
    }

    public override EuiStateBase GetNewState()
    {
        return new AdminEventLogEuiState(CurrentRoundId);
    }
}
