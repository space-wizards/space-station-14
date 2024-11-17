using Content.Client.Eui;
using Content.Shared.Administration.Notes;
using Content.Shared.Eui;
using JetBrains.Annotations;

namespace Content.Client.Administration.UI.AdminRemarks;

[UsedImplicitly]
public sealed class UserNotesEui : BaseEui
{
    public UserNotesEui()
    {
        NoteWindow = new AdminRemarksWindow();
        NoteWindow.OnClose += () => SendMessage(new CloseEuiMessage());
    }

    private AdminRemarksWindow NoteWindow { get; }

    public override void HandleState(EuiStateBase state)
    {
        if (state is not UserNotesEuiState s)
        {
            return;
        }

        NoteWindow.SetNotes(s.Notes);
    }

    public override void Opened()
    {
        NoteWindow.OpenCentered();
    }
}
