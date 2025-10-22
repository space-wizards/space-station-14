using Content.Shared.Eui;
using Robust.Shared.Serialization;
using Content.Shared.DeadSpace.Languages.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared.Administration
{
    public enum AdminAnnounceType
    {
        Station,
        Server,
    }

    [Serializable, NetSerializable]
    public sealed class AdminAnnounceEuiState : EuiStateBase
    {
    }

    public static class AdminAnnounceEuiMsg
    {
        [Serializable, NetSerializable]
        public sealed class DoAnnounce : EuiMessageBase
        {
            public bool CloseAfter;
            public string Announcer = default!;
            public string Announcement = default!;
            public AdminAnnounceType AnnounceType;
            public ProtoId<LanguagePrototype> LanguageId = default!; // DS14-Languages
            public string Voice = default!; // Corvax-TTS
            public bool EnableTTS = default!; // Corvax-TTS
            public bool CustomTTS = default!; // Corvax-TTS
        }
    }
}
