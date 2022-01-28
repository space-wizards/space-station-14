using System;
using System.Collections.Generic;
using Content.Shared.Instruments;
using Robust.Client.Audio.Midi;
using Robust.Shared.Audio.Midi;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Client.Instruments;

[RegisterComponent, ComponentReference(typeof(SharedInstrumentComponent))]
public class InstrumentComponent : SharedInstrumentComponent
{
    public event Action? OnMidiPlaybackEnded;

    public IMidiRenderer? Renderer;

    public uint SequenceDelay;

    public uint SequenceStartTick;

    public TimeSpan LastMeasured = TimeSpan.MinValue;

    public int SentWithinASec;

    /// <summary>
    ///     A queue of MidiEvents to be sent to the server.
    /// </summary>
    [ViewVariables]
    public readonly List<MidiEvent> MidiEventBuffer = new();

    /// <summary>
    ///     Whether a midi song will loop or not.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public bool LoopMidi { get; set; } = false;

    /// <summary>
    ///     Whether this instrument is handheld or not.
    /// </summary>
    [ViewVariables]
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
}
