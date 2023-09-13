using Robust.Shared.Audio;

namespace Content.Shared.Bed.Sleep;

[RegisterComponent]
public sealed partial class SleepEmitSoundComponent : Component
{
    /// <summary>
    /// Sound to play when sleeping
    /// </summary>
    [DataField("snore"), ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier Snore = new SoundCollectionSpecifier("Snores", AudioParams.Default.WithVariation(0.2f));

    /// <summary>
    /// Interval between snore attempts in seconds
    /// </summary>
    [DataField("interval"), ViewVariables(VVAccess.ReadWrite)]
    public float Interval = 5f;

    /// <summary>
    /// Chance for snore attempt to succeed
    /// </summary>
    [DataField("chance"), ViewVariables(VVAccess.ReadWrite)]
    public float Chance = 0.33f;

    /// <summary>
    /// Popup for snore (e.g. Zzz...)
    /// </summary>
    [DataField("popUp"), ViewVariables(VVAccess.ReadWrite)]
    public string PopUp = "sleep-onomatopoeia";
}
