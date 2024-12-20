using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

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
