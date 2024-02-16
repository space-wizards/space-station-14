using Content.Client.Eui;
using Content.Shared.Administration.Notes;
using Content.Shared.Eui;
using JetBrains.Annotations;
using static Content.Shared.Administration.Notes.AdminNoteEuiMsg;

namespace Content.Client.Administration.UI.Notes;

[UsedImplicitly]
public sealed class AdminNotesEui : BaseEui
{
    public AdminNotesEui()
    {
        NoteWindow = new AdminNotesWindow();
        NoteControl = NoteWindow.Notes;

        NoteControl.NoteChanged += (id, type, text, severity, secret, expiryTime) => SendMessage(new EditNoteRequest(id, type, text, severity, secret, expiryTime));
        NoteControl.NewNoteEntered += (type, text, severity, secret, expiryTime) => SendMessage(new CreateNoteRequest(type, text, severity, secret, expiryTime));
        NoteControl.NoteDeleted += (id, type) => SendMessage(new DeleteNoteRequest(id, type));
        NoteWindow.OnClose += () => SendMessage(new CloseEuiMessage());
    }

    public override void Closed()
    {
        base.Closed();
        NoteWindow.Close();
    }

    private AdminNotesWindow NoteWindow { get; }

    private AdminNotesControl NoteControl { get; }

    public override void HandleState(EuiStateBase state)
    {
        if (state is not AdminNotesEuiState s)
        {
            return;
        }

        NoteWindow.SetTitlePlayer(s.NotedPlayerName);
        NoteControl.SetPlayerName(s.NotedPlayerName);
        NoteControl.SetNotes(s.Notes);
        NoteControl.SetPermissions(s.CanCreate, s.CanDelete, s.CanEdit);
    }

    public override void Opened()
    {
        NoteWindow.OpenCentered();
    }
}
