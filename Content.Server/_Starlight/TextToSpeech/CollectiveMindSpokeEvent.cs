namespace Content.Server.Starlight.TTS;

public sealed class CollectiveMindSpokeEvent : EntityEventArgs
{
    public EntityUid Source { get; set; }
    public string Message { get; set; } = null!;
    public EntityUid[] Receivers { get; set; } = null!;
}
