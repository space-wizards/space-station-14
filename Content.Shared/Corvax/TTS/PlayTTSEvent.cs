using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Corvax.TTS;

[Serializable, NetSerializable]
// ReSharper disable once InconsistentNaming
public sealed class PlayTTSEvent : EntityEventArgs
{
    public byte[] Data { get; }
    public NetEntity? SourceUid { get; }
    public bool IsWhisper { get; }
    public bool IsRadio { get; }
    public bool IsLexiconSound { get; } // DS14-Language
    public string LanguageId { get; } // DS14-Language
    public PlayTTSEvent(byte[] data, NetEntity? sourceUid = null, bool isWhisper = false, bool isRadio = false, bool isSoundLexicon = false, string languageId = "")
    {
        Data = data;
        SourceUid = sourceUid;
        IsWhisper = isWhisper;
        IsRadio = isRadio;
        IsLexiconSound = isSoundLexicon; // DS14-Language
        LanguageId = languageId; // DS14-Language
    }
}
