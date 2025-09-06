using Content.Shared.Instruments;
using Robust.Client.Audio.Midi;
using Robust.Shared.Audio.Midi;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom; // Starlight

namespace Content.Client.Instruments;

[RegisterComponent]
[AutoGenerateComponentPause] // Starlight
public sealed partial class InstrumentComponent : SharedInstrumentComponent
{
    public event Action? OnMidiPlaybackEnded;

    [ViewVariables]
    public IMidiRenderer? Renderer;

    [ViewVariables]
    public uint SequenceDelay;

    [ViewVariables]
    public uint SequenceStartTick;

    [ViewVariables]
    public TimeSpan LastMeasured = TimeSpan.MinValue;

    [ViewVariables]
    public int SentWithinASec;

    /// <summary>
    ///     A queue of MidiEvents to be sent to the server.
    /// </summary>
    [ViewVariables]
    public readonly List<RobustMidiEvent> MidiEventBuffer = new();

    /// <summary>
    ///     Whether a midi song will loop or not.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public bool LoopMidi { get; set; } = false;

    /// <summary>
    ///     Whether this instrument is handheld or not.
    /// </summary>
    [DataField("handheld")]
    public bool Handheld { get; set; } // TODO: Replace this by simply checking if the entity has an ItemComponent.

    /// <summary>
    ///     Whether there's a midi song being played or not.
    /// </summary>
    [ViewVariables]
    public bool IsMidiOpen => Renderer?.Status == MidiRendererStatus.File;

    /// <summary>
    ///     Whether the midi renderer is listening for midi input or not.
    /// </summary>
    [ViewVariables]
    public bool IsInputOpen => Renderer?.Status == MidiRendererStatus.Input;

    /// <summary>
    ///     Whether the midi renderer is alive or not.
    /// </summary>
    [ViewVariables]
    public bool IsRendererAlive => Renderer != null;

    [ViewVariables]
    public int PlayerTotalTick => Renderer?.PlayerTotalTick ?? 0;

    [ViewVariables]
    public int PlayerTick => Renderer?.PlayerTick ?? 0;

    public void PlaybackEndedInvoke() => OnMidiPlaybackEnded?.Invoke();

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan NextInputTime { get; set; } = TimeSpan.Zero; // Starlight

    [DataField]
    public TimeSpan InputDelay { get; set; } = TimeSpan.FromSeconds(1); // Starlight
}
