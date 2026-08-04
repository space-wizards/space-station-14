namespace Content.Client.DeadSpace.Heartbeat;

[RegisterComponent]
public sealed partial class CriticalMutedAudioComponent : Component
{
    public float OriginalVolume;
    public float AppliedVolume;
}
