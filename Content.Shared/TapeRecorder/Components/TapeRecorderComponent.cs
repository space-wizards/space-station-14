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
    [ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public TapeRecorderMode Mode = TapeRecorderMode.Empty;

    /// <summary>
    /// True when the current mode is active i.e. recording or stopped
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public bool Active = false;

    /// <summary>
    /// How fast can this tape recorder rewind
    /// Acts as a multiplier for the frameTime
    /// </summary>
    [DataField]
    public float RewindSpeed = 3f;

    /// <summary>
    /// What sound is used when play mode is activated
    /// </summary>
    [DataField]
    public SoundSpecifier PlaySound = new SoundPathSpecifier("/Audio/Items/Taperecorder/sound_items_taperecorder_taperecorder_play.ogg")
    {
        Params = AudioParams.Default.WithVolume(-2f).WithMaxDistance(3f)
    };

    /// <summary>
    /// What sound is used when stop mode is activated
    /// </summary>
    [DataField]
    public SoundSpecifier StopSound = new SoundPathSpecifier("/Audio/Items/Taperecorder/sound_items_taperecorder_taperecorder_stop.ogg")
    {
        Params = AudioParams.Default.WithVolume(-2f).WithMaxDistance(3f)
    };

    /// <summary>
    /// What sound is used when rewind mode is activated
    /// </summary>
    [DataField]
    public SoundSpecifier RewindSound = new SoundPathSpecifier("/Audio/Items/Taperecorder/sound_items_taperecorder_taperecorder_rewind.ogg")
    {
        Params = AudioParams.Default.WithVolume(-2f).WithMaxDistance(3f)
    };


    //TODO: Replace with RSI
    [DataField]
    public SpriteSpecifier PlayIcon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/playarrow.svg.192dpi.png"));

    [DataField]
    public SpriteSpecifier RecordIcon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/dot.svg.192dpi.png"));

    [DataField]
    public SpriteSpecifier RewindIcon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/rewindarrow.svg.192dpi.png"));
}

/// <summary>
/// Currently recording tape recorder
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class RecordingTapeRecorderComponent : Component
{
}

/// <summary>
/// Currently rewinding tape recorder
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class RewindingTapeRecorderComponent : Component
{
}

/// <summary>
/// Currently playing tape recorder
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class PlayingTapeRecorderComponent : Component
{
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
