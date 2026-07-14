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

    private int CurrentRoundId => _e.System<GameTicker>().RoundId;

    public AdminEventLogEui()
    {
        IoCManager.InjectDependencies(this);
    }

    public override async void Opened()
    {
        base.Opened();

        _adminManager.OnPermsChanged += OnPermsChanged;
        StateDirty();
    }

    public override void Closed()
    {
        base.Closed();

        _adminManager.OnPermsChanged -= OnPermsChanged;
    }

    public override EuiStateBase GetNewState()
    {
        var state = new AdminEventLogEuiState(CurrentRoundId);
        return state;
    }

    public override async void HandleMessage(EuiMessageBase msg)
    {
    }

    private void OnPermsChanged(AdminPermsChangedEventArgs args)
    {
        if (args.Player == Player && !_adminManager.HasAdminFlag(Player, AdminFlags.Admin))
        {
            Close();
        }
    }
}
