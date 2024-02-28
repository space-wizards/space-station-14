using Content.Server.Database;
using Content.Server.EUI;
using Content.Shared.Administration.Notes;
using Content.Shared.CCVar;
using Content.Shared.Eui;
using Robust.Shared.Configuration;
using static Content.Shared.Administration.Notes.AdminMessageEuiMsg;

namespace Content.Server.Administration.Notes;

public sealed class AdminMessageEui : BaseEui
{
    [Dependency] private readonly IAdminNotesManager _notesMan = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    private readonly float _closeWait;
    private AdminMessageRecord? _message;
    private DateTime _startTime;

    public AdminMessageEui()
    {
        IoCManager.InjectDependencies(this);
        _closeWait = _cfg.GetCVar(CCVars.MessageWaitTime);
    }

    public void SetMessage(AdminMessageRecord message)
    {
        _message = message;
        _startTime = DateTime.UtcNow;
        StateDirty();
    }

    public override EuiStateBase GetNewState()
    {
        if (_message == null)
            return new AdminMessageEuiState(float.MaxValue, "An error has occurred.", string.Empty, DateTime.MinValue);
        return new AdminMessageEuiState(
            _closeWait,
            _message.Message,
            _message.CreatedBy?.LastSeenUserName ?? "[System]",
            _message.CreatedAt.UtcDateTime
        );
    }

    public override async void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        switch (msg)
        {
            case Accept:
                if (_message == null)
                    break;
                // No escape
                if (DateTime.UtcNow - _startTime >= TimeSpan.FromSeconds(_closeWait))
                    await _notesMan.MarkMessageAsSeen(_message.Id);
                Close();
                break;
            case Dismiss:
                Close();
                break;
        }
    }
}
