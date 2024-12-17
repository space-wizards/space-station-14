using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.LandMines;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class LandMineComponent : Component
{
    /// <summary>
    /// Trigger sound effect when stepping onto landmine
    /// </summary>
    [DataField]
    public SoundSpecifier? Sound;

    /// <summary>
    /// Is the land mine armed and dangerous?
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Armed = false;

    /// <summary>
    /// Does it show its status on examination?
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool ShowStatusOnExamination = true;

    /// <summary>
    /// Does it give the option to be arme ?
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool ShowVerbArm = true;
}

[Serializable, NetSerializable]
public enum LandMineVisuals
{
    Armed,
}
