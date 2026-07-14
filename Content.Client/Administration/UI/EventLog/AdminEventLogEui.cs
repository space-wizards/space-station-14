using Content.Client.Eui;
using Content.Shared.Administration.AdminEventLog;
using Content.Shared.Eui;
using JetBrains.Annotations;
using Robust.Client.Player;
using Robust.Shared.Utility;
using static Robust.Client.UserInterface.Controls.BaseButton;

namespace Content.Client.Administration.UI.EventLog;

[UsedImplicitly]
public sealed partial class AdminEventLogEui : BaseEui
{
    [Dependency] private IPlayerManager _playerManager = default!;

    private AdminEventLogWindow EventLogWindow { get; }

    public AdminEventLogEui()
    {
        EventLogWindow = new AdminEventLogWindow();
        EventLogWindow.SendEventLog.OnPressed += OnSendEventLogPressed;
    }

    public override void Opened()
    {
        base.Opened();

        EventLogWindow.OpenCentered();
    }

    public override void Closed()
    {
        base.Closed();

        EventLogWindow.Close();
    }

    public override void HandleState(EuiStateBase state)
    {
        var s = (AdminEventLogEuiState)state;

        EventLogWindow.SetCurrentRound(s.RoundId);
        EventLogWindow.SetRoundSpinBox(s.RoundId);
    }

    private void OnSendEventLogPressed(ButtonEventArgs args)
    {
        var eventText = Rope.Collapse(EventLogWindow.EventTextEdit.TextRope).TrimStart();

        if (string.IsNullOrEmpty(eventText))
            return;

        var message = new AdminEventLogEuiMsg(
            EventLogWindow.RoundSpinBox.Value,
            _playerManager.LocalSession!.Name,
            eventText);

        SendMessage(message);

        // close window to avoid admin accidentally spamming event logs
        EventLogWindow.Close();
    }
}
