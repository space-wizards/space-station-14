using Robust.Shared.Audio;

namespace Content.Client.Light.Visualizers;

[RegisterComponent]
[Access(typeof(PoweredLightVisualizerSystem))]
public sealed class PoweredLightVisualizerComponent : Component
{
    [DataField("minBlinkingTime")] public float MinBlinkingTime = 0.5f;
    [DataField("maxBlinkingTime")] public float MaxBlinkingTime = 2;
    [DataField("blinkingSound")] public SoundSpecifier? BlinkingSound = default;

    public bool WasBlinking;

    public Action<string>? BlinkingCallback;
}
