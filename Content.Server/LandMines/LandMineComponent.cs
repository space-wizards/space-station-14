using Robust.Shared.Audio;

namespace Content.Server.LandMines;

[RegisterComponent]
public sealed partial class LandMineComponent : Component
{
    /// <summary>
    /// Defines whether landmine will explode when stepping on it or when stepping off it
    /// True - StepTrigger, False - StepOffTrigger
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool TriggerImmediately = false;

    /// <summary>
    /// Trigger sound effect when stepping onto landmine
    /// </summary>
    [DataField("sound")]
    public SoundSpecifier TriggerSound;
}
