using System.Threading.Tasks;
using Content.Server.EUI;
using Content.Shared.Administration.Notes;
using Content.Shared.Eui;
using Robust.Shared;
using Robust.Shared.Configuration;
using static Content.Shared.Administration.Notes.UserNotesEuiMsg;

namespace Content.Server.Administration.Notes;

public sealed class UserNotesEui : BaseEui
{
    [Dependency] private readonly IAdminNotesManager _notesMan = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    private readonly bool _seeOwnNotes;

    public UserNotesEui()
    {
        IoCManager.InjectDependencies(this);
        _seeOwnNotes = _cfg.GetCVar(CVars.SeeOwnNotes);

        if (!_seeOwnNotes)
        {
            Logger.WarningS("admin.notes", "User notes initialized when see_own_notes set to false");
        }
    }

    private Dictionary<int, SharedAdminNote> Notes { get; set; } = new();

    public override EuiStateBase GetNewState()
    {
        return new UserNotesEuiState(
            Notes
        );
    }

    public override async void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        switch (msg)
        {
            case Close _:
            {
                Close();
                break;
            }
        }
    }

    public async Task UpdateNotes()
    {
        var notes = new Dictionary<int, SharedAdminNote>();
        if (!_seeOwnNotes)
        {
            Logger.WarningS("admin.notes", $"User {Player.Name} with ID {Player.UserId} tried to update their own user notes when see_own_notes was set to false");
            return;
        }

        foreach (var note in await _notesMan.GetVisibleNotes(Player.UserId))
        {
            note.ExpiryTime = null;
            notes.Add(note.Id, note.ToShared());
        }

        Notes = notes;

        StateDirty();
    }
}
