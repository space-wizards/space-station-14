using Robust.Shared.Audio;

namespace Content.Server.Starlight.Gavel;

[RegisterComponent]
public sealed partial class GavelBlockComponent : Component
{
    [DataField]
    public SoundSpecifier HitSound;

    [DataField]
    public TimeSpan Cooldown = TimeSpan.FromSeconds(0.5);

    [DataField(readOnly: true)]
    public int Counter;

    [DataField(readOnly: true)]
    public int MaxCounter = 60;

    public TimeSpan? PrevSound;
}