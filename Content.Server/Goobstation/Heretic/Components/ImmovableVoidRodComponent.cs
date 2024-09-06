using Robust.Shared.Audio;

namespace Content.Server.Heretic.Components;

[RegisterComponent]
public sealed partial class ImmovableVoidRodComponent : Component
{
    [DataField] public TimeSpan Lifetime = TimeSpan.FromSeconds(1f);
    public float Accumulator = 0f;
}
