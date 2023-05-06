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

        NoteControl.OnNoteChanged += (id, text) => SendMessage(new EditNoteRequest(id, text));
        NoteControl.OnNewNoteEntered += text => SendMessage(new CreateNoteRequest(text));
        NoteControl.OnNoteDeleted += id => SendMessage(new DeleteNoteRequest(id));
        NoteWindow.OnClose += OnClosed;
    }

    private void OnClosed()
    {
        SendMessage(new CloseEuiMessage());
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
        NoteControl.SetNotes(s.Notes);
        NoteControl.SetPermissions(s.CanCreate, s.CanDelete, s.CanEdit);
    }

    public override void Opened()
    {
        NoteWindow.OpenCentered();
    }
}
