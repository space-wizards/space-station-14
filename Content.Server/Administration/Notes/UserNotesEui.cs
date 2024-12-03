using System.Linq;
using System.Threading.Tasks;
using Content.Server.EUI;
using Content.Shared.Administration.Notes;
using Content.Shared.CCVar;
using Content.Shared.Database;
using Content.Shared.Eui;
using Robust.Shared.Configuration;

namespace Content.Server.Administration.Notes;

public sealed class UserNotesEui : BaseEui
{
    [Dependency] private readonly IAdminNotesManager _notesMan = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly ILogManager _logManager = default!;
    private ISawmill _sawmill = default!;
    private const string SawmillName = "admin.notes";

    private readonly bool _seeOwnNotes;

    public UserNotesEui()
    {
        IoCManager.InjectDependencies(this);
        _seeOwnNotes = _cfg.GetCVar(CCVars.SeeOwnNotes);

        _sawmill = _logManager.GetSawmill(SawmillName);

        if (!_seeOwnNotes)
        {
            _sawmill.Warning("User notes initialized when see_own_notes set to false");
        }
    }

    private Dictionary<(int, NoteType), SharedAdminNote> Notes { get; set; } = new();

    public override EuiStateBase GetNewState()
    {
        return new UserNotesEuiState(
            Notes
        );
    }

    public async Task UpdateNotes()
    {
        if (!_seeOwnNotes)
        {
            _sawmill.Warning($"User {Player.Name} with ID {Player.UserId} tried to update their own user notes when see_own_notes was set to false");
            return;
        }

        Notes = (await _notesMan.GetVisibleRemarks(Player.UserId)).Select(note => note.ToShared()).ToDictionary(note => (note.Id, note.NoteType));
        StateDirty();
    }
}
