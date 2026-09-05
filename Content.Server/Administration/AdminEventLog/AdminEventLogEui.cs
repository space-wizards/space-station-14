using Content.Server.Administration.Managers;
using Content.Server.Discord.WebhookMessages;
using Content.Server.EUI;
using Content.Server.GameTicking;
using Content.Shared.Administration;
using Content.Shared.Administration.AdminEventLog;
using Content.Shared.CCVar;
using Content.Shared.Eui;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;

namespace Content.Server.Administration.AdminEventLog;

/// <summary>
/// Sends a message to discord after a admin logs a event
/// </summary>
public sealed partial class AdminEventLogEui : BaseEui
{
    [Dependency] private IAdminManager _adminManager = default!;
    [Dependency] private IConfigurationManager _config = default!;
    [Dependency] private EventWebhook _eventWebhook = default!;
    [Dependency] private IEntityManager _e = default!;
    [Dependency] private IPrototypeManager _protoMan = default!;

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
        var state = new AdminEventLogEuiState(CurrentRoundId, _config.GetCVar(CCVars.AdminEventAllowNoneCategory));
        return state;
    }

    public override async void HandleMessage(EuiMessageBase msg)
    {
        var message = (AdminEventLogEuiMsg)msg;
        var categoryTitle = message.EventCategory is {} category
            ? _protoMan.Index<EventCategoryPrototype>(category).Title
            : (LocId?)null;

        _eventWebhook.TrySendMessage(
            message.AdminUser,
            message.RoundId,
            message.EventDescription,
            categoryTitle,
            _config.GetCVar(CCVars.DiscordEventWebhook));
    }

    private void OnPermsChanged(AdminPermsChangedEventArgs args)
    {
        if (args.Player == Player && !_adminManager.HasAdminFlag(Player, AdminFlags.Admin))
        {
            Close();
        }
    }
}
