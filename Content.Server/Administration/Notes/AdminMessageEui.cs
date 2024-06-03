using System.Linq;
using Content.Server.Database;
using Content.Server.EUI;
using Content.Shared.Administration.Notes;
using Content.Shared.CCVar;
using Content.Shared.Eui;
using Robust.Shared.Configuration;
using Robust.Shared.Timing;
using static Content.Shared.Administration.Notes.AdminMessageEuiMsg;

namespace Content.Server.Administration.Notes;

public sealed class AdminMessageEui : BaseEui
{
    [Dependency] private readonly IAdminNotesManager _notesMan = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    private readonly TimeSpan _closeWait;
    private readonly TimeSpan _endTime;
    private readonly AdminMessageRecord[] _messages;

    public AdminMessageEui(AdminMessageRecord[] messages)
    {
        IoCManager.InjectDependencies(this);
        _closeWait = TimeSpan.FromSeconds(_cfg.GetCVar(CCVars.MessageWaitTime));
        _endTime = _gameTiming.RealTime + _closeWait;
        _messages = messages;
    }

    public override void Opened()
    {
        StateDirty();
    }

    public override EuiStateBase GetNewState()
    {
        return new AdminMessageEuiState(
            _closeWait,
            _messages.Select(x => new AdminMessageEuiState.Message(
                x.Message,
                x.CreatedBy?.LastSeenUserName ?? Loc.GetString("admin-notes-fallback-admin-name"),
                x.CreatedAt.UtcDateTime)).ToArray()
        );
    }

    public override async void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        switch (msg)
        {
            case Dismiss dismiss:
                if (_gameTiming.RealTime < _endTime)
                    return;

                foreach (var message in _messages)
                {
                    await _notesMan.MarkMessageAsSeen(message.Id, dismiss.Permanent);
                }
                Close();
                break;
        }
    }
}
