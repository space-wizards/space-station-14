using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Utility;

namespace Content.Shared.TapeRecorder.Components;

[RegisterComponent, NetworkedComponent, Access(typeof(SharedTapeRecorderSystem))]
[AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class TapeRecorderComponent : Component
{
    /// <summary>
    /// The current tape recorder mode, controls what using the item will do
    /// </summary>
    [DataField, AutoNetworkedField]
    public TapeRecorderMode Mode = TapeRecorderMode.Stopped;

    /// <summary>
    /// Paper that will spawn when printing transcript
    /// </summary>
    [DataField]
    public EntProtoId PaperPrototype = "TapeRecorderTranscript";

    /// <summary>
    /// How fast can this tape recorder rewind
    /// Acts as a multiplier for the frameTime
    /// </summary>
    [DataField]
    public float RewindSpeed = 3f;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan CooldownEndTime = TimeSpan.Zero;

    /// <summary>
    /// Cooldown of print button
    /// </summary>
    [DataField]
    public TimeSpan PrintCooldown = TimeSpan.FromSeconds(4);

    /// <summary>
    /// Default name as fallback if a message doesn't have one.
    /// </summary>
    [DataField]
    public LocId DefaultName = "tape-recorder-voice-unknown";

    /// <summary>
    /// Sound on print transcript
    /// </summary>
    [DataField]
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
    Mode,
    TapeInserted
}

[Serializable, NetSerializable]
public enum TapeRecorderMode : byte
{
    Stopped,
    Recording,
    Playing,
    Rewinding
}

[Serializable, NetSerializable]
public enum TapeRecorderUIKey : byte
{
    Key
}
