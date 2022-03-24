using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared.Administration.Notes;

[Serializable, NetSerializable]
public sealed class AdminNotesEuiState : EuiStateBase
{
    public AdminNotesEuiState(string notedPlayerName, Dictionary<int, SharedAdminNote> notes)
    {
        NotedPlayerName = notedPlayerName;
        Notes = notes;
    }

    public string NotedPlayerName { get; }
    public Dictionary<int, SharedAdminNote> Notes { get; }
}

public static class AdminNoteEuiMsg
{
    [Serializable, NetSerializable]
    public sealed class Close : EuiMessageBase
    {
    }

    [Serializable, NetSerializable]
    public sealed class CreateNoteRequest : EuiMessageBase
    {
        public CreateNoteRequest(string message)
        {
            Message = message;
        }

        public string Message { get; set; }
    }

    [Serializable, NetSerializable]
    public sealed class DeleteNoteRequest : EuiMessageBase
    {
        public DeleteNoteRequest(int id)
        {
            Id = id;
        }

        public int Id { get; set; }
    }

    [Serializable, NetSerializable]
    public sealed class EditNoteRequest : EuiMessageBase
    {
        public EditNoteRequest(int id, string message)
        {
            Id = id;
            Message = message;
        }

        public int Id { get; set; }
        public string Message { get; set; }
    }
}
