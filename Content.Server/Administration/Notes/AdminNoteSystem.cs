using System.Threading.Tasks;
using Content.Server.Administration.Commands;
using Content.Server.Administration.Managers;
using Content.Server.Database;
using Content.Server.EUI;
using Content.Server.GameTicking;
using Content.Shared.Administration;
using Content.Shared.Administration.Notes;
using Content.Shared.Database;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Network;

namespace Content.Server.Administration.Notes;

public sealed class AdminNoteSystem : EntitySystem
{
    [Dependency] private readonly IAdminManager _admins = default!;
    [Dependency] private readonly IConsoleHost _console = default!;
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly ILogManager _logManager = default!;
    [Dependency] private readonly EuiManager _euis = default!;

    [Dependency] private readonly GameTicker _gameTicker = default!;

    public const string SawmillId = "admin.notes";

    public Action<SharedAdminNote>? NoteAdded;
    public Action<SharedAdminNote>? NoteModified;
    public Action<int>? NoteDeleted;

    private ISawmill _sawmill = default!;

    public override void Initialize()
    {
        _sawmill = _logManager.GetSawmill(SawmillId);

        SubscribeLocalEvent<GetVerbsEvent<Verb>>(AddVerbs);
    }

    private void AddVerbs(GetVerbsEvent<Verb> ev)
    {
        if (EntityManager.GetComponentOrNull<ActorComponent>(ev.User) is not {PlayerSession: var user} ||
            EntityManager.GetComponentOrNull<ActorComponent>(ev.Target) is not {PlayerSession: var target})
        {
            return;
        }

        if (!CanView(user))
        {
            return;
        }

        var verb = new Verb
        {
            Text = Loc.GetString("admin-notes-verb-text"),
            Category = VerbCategory.Admin,
            IconTexture = "/Textures/Interface/VerbIcons/examine.svg.192dpi.png",
            Act = () => _console.RemoteExecuteCommand(user, $"{OpenAdminNotesCommand.CommandName} \"{target.UserId}\""),
            Impact = LogImpact.Low
        };

        ev.Verbs.Add(verb);
    }

    public bool CanView(IPlayerSession player)
    {
        return _admins.HasAdminFlag(player, AdminFlags.ViewNotes);
    }

    public bool CanEdit(IPlayerSession player)
    {
        return _admins.HasAdminFlag(player, AdminFlags.EditNotes);
    }

    public async Task OpenEui(IPlayerSession admin, Guid notedPlayer)
    {
        var ui = new AdminNotesEui();
        _euis.OpenEui(ui, admin);

        await ui.ChangeNotedPlayer(notedPlayer);
    }

    public async Task AddNote(IPlayerSession createdBy, Guid player, string message)
    {
        _sawmill.Info($"Player {createdBy.Name} added note with message {message}");

        int? round = _gameTicker.RoundId == 0 ? null : _gameTicker.RoundId;
        var createdAt = DateTime.UtcNow;
        var noteId = await _db.AddAdminNote(round, player, message, createdBy.UserId, createdAt);

        var note = new SharedAdminNote(
            noteId,
            round,
            message,
            createdBy.Name,
            createdBy.Name,
            createdAt,
            createdAt
        );
        NoteAdded?.Invoke(note);
    }

    public async Task DeleteNote(int noteId, IPlayerSession deletedBy)
    {
        var note = await _db.GetAdminNote(noteId);
        if (note == null)
        {
            return;
        }

        _sawmill.Info($"Player {deletedBy.Name} deleted note {noteId}");

        var deletedAt = DateTime.UtcNow;
        await _db.DeleteAdminNote(noteId, deletedBy.UserId, deletedAt);

        NoteDeleted?.Invoke(noteId);
    }

    public async Task ModifyNote(int noteId, IPlayerSession editedBy, string message)
    {
        message = message.Trim();

        var note = await _db.GetAdminNote(noteId);
        if (note == null || note.Message == message)
        {
            return;
        }

        _sawmill.Info($"Player {editedBy.Name} modified note {noteId} with message {message}");

        var editedAt = DateTime.UtcNow;
        await _db.EditAdminNote(noteId, message, editedBy.UserId, editedAt);

        var sharedNote = new SharedAdminNote(
            noteId,
            note.RoundId,
            message,
            note.CreatedBy.LastSeenUserName,
            editedBy.Name,
            note.CreatedAt,
            note.LastEditedAt
        );
        NoteModified?.Invoke(sharedNote);
    }

    public async Task<List<AdminNote>> GetNotes(Guid player)
    {
        return await _db.GetAdminNotes(player);
    }

    public async Task<string> GetPlayerName(Guid player)
    {
        return (await _db.GetPlayerRecordByUserId(new NetUserId(player)))?.LastSeenUserName ?? string.Empty;
    }
}
