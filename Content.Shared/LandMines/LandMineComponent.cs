using Robust.Shared.Audio;

namespace Content.Shared.LandMines;

[RegisterComponent]
public sealed partial class LandMineComponent : Component
{
    /// <summary>
    /// Trigger sound effect when stepping onto landmine
    /// </summary>
    [DataField]
    public SoundSpecifier? Sound;
}
