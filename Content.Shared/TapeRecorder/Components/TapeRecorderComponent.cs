using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.TapeRecorder.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(SharedTapeRecorderSystem))]
public sealed partial class TapeRecorderComponent : Component
{

    /// <summary>
    /// The current tape recorder mode, controls what using the item will do
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public TapeRecorderMode Mode { get; set; } = TapeRecorderMode.Empty;

    [ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public bool Active { get; set; } = false;

    /// <summary>
    /// How fast can this tape recorder rewind
    /// </summary>
    [DataField("rewindSpeed")]
    public float RewindSpeed { get; set; } = 3f;

    /// <summary>
    /// Sounds for each mode, used when activated
    /// </summary>
    [DataField("playSound")]
    public SoundSpecifier? PlaySound = new SoundPathSpecifier("/Audio/Items/Taperecorder/sound_items_taperecorder_taperecorder_play.ogg")
    {
        Params = AudioParams.Default.WithVolume(-2f).WithMaxDistance(3f)
    };

    [DataField("stopSound")]
    public SoundSpecifier? StopSound = new SoundPathSpecifier("/Audio/Items/Taperecorder/sound_items_taperecorder_taperecorder_stop.ogg")
    {
        Params = AudioParams.Default.WithVolume(-2f).WithMaxDistance(3f)
    };

    [DataField("rewindSound")]
    public SoundSpecifier? RewindSound = new SoundPathSpecifier("/Audio/Items/Taperecorder/sound_items_taperecorder_taperecorder_rewind.ogg")
    {
        Params = AudioParams.Default.WithVolume(-2f).WithMaxDistance(3f)
    };
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
