using Robust.Shared.Serialization;

namespace Content.Shared.Instruments;

/// <summary>
/// Handles shared logic and data validation for musical instruments.
/// Client and server both depend on this for event definitions and helper functions.
/// </summary>
public sealed class SharedInstrumentSystem : EntitySystem
{
    /// <summary>
    /// The maximum allowed bytes per MIDI packet.
    /// </summary>
    public const int MaxMidiPacketSize = 1024;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<InstrumentComponent, ComponentInit>(OnInit);
    }

    private void OnInit(EntityUid uid, InstrumentComponent component, ComponentInit args)
    {
        // Shared initialization (if any)
    }

    /// <summary>
    /// Verifies that a MIDI payload is valid before sending or playing it.
    /// </summary>
    public bool ValidateMidi(byte[] data)
        => data.Length <= MaxMidiPacketSize;
}

/// <summary>
/// Raised when an instrument plays a MIDI event.
/// Used to synchronize playback across clients.
/// </summary>
[Serializable, NetSerializable]
public sealed class InstrumentMidiEvent : EntityEventArgs
{
    public EntityUid Uid;
    public byte[] MidiData;

    public InstrumentMidiEvent(EntityUid uid, byte[] midiData)
    {
        Uid = uid;
        MidiData = midiData;
    }
}
