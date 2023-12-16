using Robust.Shared.Audio;
using Robust.Shared.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Shared.TapeRecorder.Components;

[RegisterComponent]
public sealed partial class TapeRecorderComponent : Component
{

    [ViewVariables(VVAccess.ReadOnly)]
    public TapeRecorderMode Mode { get; set; } = TapeRecorderMode.Empty;

    [DataField("ejectSound")]
    public SoundSpecifier? EjectSound = new SoundPathSpecifier("/Audio/Items/Taperecorder/sound_items_taperecorder_taperecorder_close.ogg");

    [DataField("playSound")]
    public SoundSpecifier? PlaySound = new SoundPathSpecifier("/Audio/Items/Taperecorder/sound_items_taperecorder_taperecorder_play.ogg");

    [DataField("stopSound")]
    public SoundSpecifier? StopSound = new SoundPathSpecifier("/Audio/Items/Taperecorder/sound_items_taperecorder_taperecorder_stop.ogg");

    [DataField("rewindSound")]
    public SoundSpecifier? RewindSound = new SoundPathSpecifier("/Audio/Items/Taperecorder/sound_items_taperecorder_taperecorder_rewind.ogg");
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
