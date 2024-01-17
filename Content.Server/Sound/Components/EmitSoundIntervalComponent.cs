using Content.Shared.Sound.Components;

namespace Content.Server.Sound.Components;

/// <summary>
/// Repeatedly plays a sound with a randomized delay.
/// </summary>
[RegisterComponent]
public sealed partial class EmitSoundIntervalComponent : BaseEmitSoundComponent
{
    [DataField]
    public TimeSpan MinInterval = TimeSpan.FromSeconds(5);

    [DataField]
    public TimeSpan MaxInterval = TimeSpan.FromSeconds(10);

    [DataField]
    public bool Enabled = true;

    public TimeSpan NextEmitTime = TimeSpan.Zero;
}
