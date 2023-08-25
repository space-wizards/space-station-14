using Content.Server.Administration.Managers;
using Content.Server.EUI;
using Content.Shared.Administration.Notes;
using Content.Shared.Database;
using Content.Shared.Eui;
using System.Linq;
using System.Threading.Tasks;
using Content.Server.Database;
using Robust.Shared.Network;
using static Content.Shared.Administration.Notes.AdminNoteEuiMsg;

namespace Content.Server.Administration.Notes;

public sealed class AdminNotesEui : BaseEui
{
    [Dependency] private readonly IAdminManager _admins = default!;
    [Dependency] private readonly IAdminNotesManager _notesMan = default!;
    [Dependency] private readonly IPlayerLocator _locator = default!;

    public AdminNotesEui()
    {
        IoCManager.InjectDependencies(this);
    }

    private Guid NotedPlayer { get; set; }
    private string NotedPlayerName { get; set; } = string.Empty;
    private bool HasConnectedBefore { get; set; }
    private Dictionary<(int, NoteType), SharedAdminNote> Notes { get; set; } = new();

    public override async void Opened()
    {
        base.Opened();

        _admins.OnPermsChanged += OnPermsChanged;
        _notesMan.NoteAdded += NoteModified;
        _notesMan.NoteModified += NoteModified;
        _notesMan.NoteDeleted += NoteDeleted;
    }

    public override void Closed()
    {
        base.Closed();

        _admins.OnPermsChanged -= OnPermsChanged;
        _notesMan.NoteAdded -= NoteModified;
        _notesMan.NoteModified -= NoteModified;
        _notesMan.NoteDeleted -= NoteDeleted;
    }

    public override EuiStateBase GetNewState()
    {
        return new AdminNotesEuiState(
            NotedPlayerName,
            Notes,
            _notesMan.CanCreate(Player) && HasConnectedBefore,
            _notesMan.CanDelete(Player),
            _notesMan.CanEdit(Player)
        );
    }

    public override async void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        switch (msg)
        {
            case CreateNoteRequest request:
                {
                    if (!_notesMan.CanCreate(Player))
                    {
                        break;
                    }

                    if (string.IsNullOrWhiteSpace(request.Message))
                    {
                        break;
                    }

                    if (request.ExpiryTime is not null && request.ExpiryTime <= DateTime.UtcNow)
                    {
                        break;
                    }

                    await _notesMan.AddAdminRemark(Player, NotedPlayer, request.NoteType, request.Message, request.NoteSeverity, request.Secret, request.ExpiryTime);
                    break;
                }
            case DeleteNoteRequest request:
                {
                    if (!_notesMan.CanDelete(Player))
                    {
                        break;
                    }

                    await _notesMan.DeleteAdminRemark(request.Id, request.Type, Player);
                    break;
                }
            case EditNoteRequest request:
                {
                    if (!_notesMan.CanEdit(Player))
                    {
                        break;
                    }

                    if (string.IsNullOrWhiteSpace(request.Message))
                    {
                        break;
                    }

                    await _notesMan.ModifyAdminRemark(request.Id, request.Type, Player, request.Message, request.NoteSeverity, request.Secret, request.ExpiryTime);
                    break;
                }
        }
    }

    public async Task ChangeNotedPlayer(Guid notedPlayer)
    {
        NotedPlayer = notedPlayer;
        await LoadFromDb();
    }

    private void NoteModified(SharedAdminNote note)
    {
        if (note.Player != NotedPlayer)
            return;

        Notes[(note.Id, note.NoteType)] = note;
        StateDirty();
    }

    private void NoteDeleted(SharedAdminNote note)
    {
        if (note.Player != NotedPlayer)
            return;

        Notes.Remove((note.Id, note.NoteType));
        StateDirty();
    }

    private async Task LoadFromDb()
    {
        var locatedPlayer = await _locator.LookupIdAsync((NetUserId) NotedPlayer);
        NotedPlayerName = locatedPlayer?.Username ?? string.Empty;
        HasConnectedBefore = locatedPlayer?.LastAddress is not null;
        Notes = (from note in await _notesMan.GetAllAdminRemarks(NotedPlayer)
                 select note.ToShared())
            .ToDictionary(sharedNote => (sharedNote.Id, sharedNote.NoteType));
        StateDirty();
    }

    private void OnPermsChanged(AdminPermsChangedEventArgs args)
    {
        if (args.Player != Player)
        {
            return;
        }

        if (!_notesMan.CanView(Player))
        {
            Close();
        }
        else
        {
            StateDirty();
        }
    }
}
