namespace Content.Server.SurveillanceCamera;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent]
public sealed class SurveillanceCameraSpeakerComponent : Component
{
    // mostly copied from Speech
    [DataField("speechEnabled")] public bool SpeechEnabled = false;

    [ViewVariables] public float SpeechSoundCooldown = 0.5f;

    [ViewVariables] public Queue<string> LastSpokenNames = new();

    public TimeSpan LastSoundPlayed = TimeSpan.Zero;
}
