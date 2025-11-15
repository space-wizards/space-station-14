using Robust.Shared.Configuration;

namespace Content.Shared.Instruments;

/// <summary>
/// Shared configuration variables for musical instruments.
/// </summary>
[CVarDefs]
public static class InstrumentCVars
{
    /// <summary>
    /// Maximum allowed MIDI notes per second per player.
    /// Prevents flooding the network.
    /// </summary>
    public static readonly CVarDef<int> MidiNotesPerSecondCap =
        CVarDef.Create("instrument.midi_notes_per_second_cap", 500,
            CVar.SERVER | CVar.REPLICATED,
            "Maximum number of MIDI notes per second allowed per player.");

    /// <summary>
    /// Maximum allowed packet size for individual MIDI messages.
    /// </summary>
    public static readonly CVarDef<int> MidiPacketSizeLimit =
        CVarDef.Create("instrument.midi_packet_size_limit", 1024,
            CVar.SERVER | CVar.REPLICATED,
            "Maximum size in bytes of an instrument MIDI packet.");
}
