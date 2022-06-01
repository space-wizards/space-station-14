namespace Content.Client.Effects;

[RegisterComponent]
public sealed class EffectVisualsComponent : Component
{
    public float Length;
    public float Accumulator = 0f;
}
