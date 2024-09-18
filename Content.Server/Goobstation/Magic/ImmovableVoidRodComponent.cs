using Robust.Shared.Audio;

namespace Content.Server.Magic;

[RegisterComponent]
public sealed partial class ImmovableVoidRodComponent : Component
{
    [DataField] public TimeSpan Lifetime = TimeSpan.FromSeconds(1f);
    public float Accumulator = 0f;
}
