using System.Threading.Tasks;
using Content.Server.Administration.Managers;
using Content.Server.EUI;
using Content.Shared.Administration.Notes;
using Content.Shared.Eui;
using static Content.Shared.Administration.Notes.AdminNoteEuiMsg;

namespace Content.Server.Administration.Notes;

public sealed class AdminNotesEui : BaseEui
{
    [Dependency] private readonly IAdminManager _admins = default!;
    [Dependency] private readonly IAdminNotesManager _notes = default!;

    public AdminNotesEui()
    {
        IoCManager.InjectDependencies(this);
    }

    private Guid NotedPlayer { get; set; }
    private string NotedPlayerName { get; set; } = string.Empty;
    private Dictionary<int, SharedAdminNote> Notes { get; set; } = new();

    public override async void Opened()
    {
        base.Opened();

        _admins.OnPermsChanged += OnPermsChanged;
        _notes.NoteAdded += NoteModified;
        _notes.NoteModified += NoteModified;
        _notes.NoteDeleted += NoteDeleted;
    }

    public override void Closed()
    {
        base.Closed();

        _admins.OnPermsChanged -= OnPermsChanged;
        _notes.NoteAdded -= NoteModified;
        _notes.NoteModified -= NoteModified;
        _notes.NoteDeleted -= NoteDeleted;
    }

    public override EuiStateBase GetNewState()
    {
        return new AdminNotesEuiState(NotedPlayerName, Notes);
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
            case CreateNoteRequest {Message: var message}:
            {
                if (!_notes.CanCreate(Player))
                {
                    Close();
                    break;
                }

                if (string.IsNullOrWhiteSpace(message))
                {
                    break;
                }

                await _notes.AddNote(Player, NotedPlayer, message);
                break;
            }
            case DeleteNoteRequest request:
            {
                if (!_notes.CanDelete(Player))
                {
                    Close();
                    break;
                }

                await _notes.DeleteNote(request.Id, Player);
                break;
            }
            case EditNoteRequest request:
            {
                if (!_notes.CanEdit(Player))
                {
                    Close();
                    break;
                }

                if (string.IsNullOrWhiteSpace(request.Message))
                {
                    break;
                }

                await _notes.ModifyNote(request.Id, Player, request.Message);
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
        Notes[note.Id] = note;
        StateDirty();
    }

    private void NoteDeleted(int id)
    {
        Notes.Remove(id);
        StateDirty();
    }

    private async Task LoadFromDb()
    {
        NotedPlayerName = await _notes.GetPlayerName(NotedPlayer);

        var notes = new Dictionary<int, SharedAdminNote>();
        foreach (var note in await _notes.GetNotes(NotedPlayer))
        {
            notes.Add(note.Id, note.ToShared());
        }

        Notes = notes;

        StateDirty();
    }

    private void OnPermsChanged(AdminPermsChangedEventArgs args)
    {
        if (args.Player == Player && !_notes.CanView(Player))
        {
            Close();
        }
    }
}
