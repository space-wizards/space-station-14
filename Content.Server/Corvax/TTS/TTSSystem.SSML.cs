namespace Content.Server.Corvax.TTS;

// ReSharper disable once InconsistentNaming
public sealed partial class TTSSystem
{
    private string ToSsmlText(string text, SpeechRate rate = SpeechRate.Medium)
    {
        return $"<speak><prosody rate=\"{SpeechRateMap[rate]}\">{text}</prosody></speak>";
    }

    private enum SpeechRate : byte
    {
        VerySlow,
        Slow,
        Medium,
        Fast,
        VeryFast,
    }

    private static readonly IReadOnlyDictionary<SpeechRate, string> SpeechRateMap =
        new Dictionary<SpeechRate, string>()
        {
            {SpeechRate.VerySlow, "x-slow"},
            {SpeechRate.Slow, "slow"},
            {SpeechRate.Medium, "medium"},
            {SpeechRate.Fast, "fast"},
            {SpeechRate.VeryFast, "x-fast"},
        };
}
