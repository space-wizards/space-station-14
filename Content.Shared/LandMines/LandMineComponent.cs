using Robust.Shared.Audio;

namespace Content.Shared.LandMines;

/// <summary>
/// Give a warning if stepped on and will execute a trigger on step off. When used together with ArmableComponent and
/// ItemToggleComponent it will only trigger if "ItemToggle.Activated" is true.
/// </summary>
[RegisterComponent]
public sealed partial class LandMineComponent : Component
{
    /// <summary>
    /// The text that popups when the landmine is stepped on.
    /// </summary>
    [DataField]
    public LocId? TriggerText = "land-mine-triggered";

    /// <summary>
    /// Trigger sound effect when stepping onto landmine
    /// </summary>
    [DataField]
    public SoundSpecifier? Sound;
}
