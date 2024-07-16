using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.TapeRecorder.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(SharedTapeRecorderSystem))]
public sealed partial class TapeRecorderComponent : Component
{

    /// <summary>
    /// The current tape recorder mode, controls what using the item will do
    /// </summary>
    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public TapeRecorderMode Mode = TapeRecorderMode.Empty;

    /// <summary>
    /// True when the current mode is active i.e. recording or stopped
    /// </summary>
    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public bool Active = false;

    /// <summary>
    /// Paper that will spawn when printing transcript
    /// </summary>
    [DataField("paperPrototype")]
    public string PaperPrototype = "Paper";

    /// <summary>
    /// How fast can this tape recorder rewind
    /// Acts as a multiplier for the frameTime
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float RewindSpeed = 3f;

    /// <summary>
    /// Cooldown of print button
    /// </summary>
    [DataField]
    public TimeSpan PrintCooldown = TimeSpan.FromSeconds(4);

    /// <summary>
    /// Sound on print transcript
    /// </summary>
    [DataField("printSound")]
    public SoundSpecifier PrintSound = new SoundPathSpecifier("/Audio/Machines/diagnoser_printing.ogg")
    {
        Params = AudioParams.Default.WithVolume(-2f).WithMaxDistance(3f)
    };

    /// <summary>
    /// What sound is used when play mode is activated
    /// </summary>
    [DataField]
    public SoundSpecifier PlaySound = new SoundPathSpecifier("/Audio/Items/Taperecorder/taperecorder_play.ogg")
    {
        Params = AudioParams.Default.WithVolume(-2f).WithMaxDistance(3f)
    };

    /// <summary>
    /// What sound is used when stop mode is activated
    /// </summary>
    [DataField]
    public SoundSpecifier StopSound = new SoundPathSpecifier("/Audio/Items/Taperecorder/taperecorder_stop.ogg")
    {
        Params = AudioParams.Default.WithVolume(-2f).WithMaxDistance(3f)
    };

    /// <summary>
    /// What sound is used when rewind mode is activated
    /// </summary>
    [DataField]
    public SoundSpecifier RewindSound = new SoundPathSpecifier("/Audio/Items/Taperecorder/taperecorder_rewind.ogg")
    {
        Params = AudioParams.Default.WithVolume(-2f).WithMaxDistance(3f)
    };

    /// <summary>
    /// Do UI needs to be updated?
    /// </summary>
    [DataField]
    public bool NeedUIUpdate = true;

    //Locale references
    [DataField]
    public LocId TextCantEject = "tape-recorder-locked";

    [DataField]
    public LocId TextModePlaying = "tape-recorder-playing";

    [DataField]
    public LocId TextModeRecording = "tape-recorder-recording";

    [DataField]
    public LocId TextModeRewinding = "tape-recorder-rewinding";

    [DataField]
    public LocId TextModeStopped = "tape-recorder-stopped";

    [DataField]
    public LocId TextModeEmpty = "tape-recorder-empty";
}

[Serializable, NetSerializable]
public enum TapeRecorderVisuals : byte
{
    Status
}

[Serializable, NetSerializable]
public enum TapeRecorderMode : byte
{
    Empty,
    Stopped,
    Playing,
    Recording,
    Rewinding
}

[Serializable, NetSerializable]
public enum TapeRecorderUIKey : byte
{
    Key
}
