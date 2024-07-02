using Robust.Shared.Audio;
using Robust.Shared.Serialization;
using System.Threading;

namespace Content.Shared.VoiceRecorder;

[RegisterComponent]
public sealed partial class VoiceRecorderComponent : Component
{
    public CancellationTokenSource? CancelToken;

    [DataField]
    public List<string> RecordedText = new List<string>();

    [DataField("range")]
    public int Range = 4;

    [DataField("printSound")]
    public SoundSpecifier PrintSound = new SoundPathSpecifier("/Audio/Machines/diagnoser_printing.ogg");

    [DataField("recordingStartSound")]
    public SoundSpecifier RecordingStartSound = new SoundPathSpecifier("/Audio/Items/Recorder/recorder_start.ogg");

    [DataField("recordingStopSound")]
    public SoundSpecifier RecordingStopSound = new SoundPathSpecifier("/Audio/Items/Recorder/recorder_stop.ogg");

    [DataField("recordingEraseSound")]
    public SoundSpecifier RecordingEraseSound = new SoundPathSpecifier("/Audio/Items/Recorder/recorder_erase.ogg");

    [DataField]
    public bool IsRecording = false;

    [DataField]
    public TimeSpan RecordTime = TimeSpan.Zero;

    [DataField("paperPrototype")]
    public string PaperPrototype = "Paper";

    [DataField]
    public TimeSpan PrintCooldown = TimeSpan.FromSeconds(4);

    [DataField]
    public TimeSpan PrintCooldownEnd = TimeSpan.Zero;
}

[Serializable, NetSerializable]
public enum VoiceRecorderVisuals : byte
{
    IsRecording
}
