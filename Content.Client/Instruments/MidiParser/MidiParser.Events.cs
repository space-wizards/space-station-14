using System.Linq;
using Content.Shared.Instruments;

namespace Content.Client.Instruments.MidiParser;

public static partial class MidiParser
{
    // Parser methods called for meta-events (Status = 0xFF)
    private static readonly Dictionary<ushort, Action<MidiStreamWrapper, int, int, MidiTrackInfo>> MidiMetaEventHandlers = new()
    {
        { 0x00, ReadNotImplementedEvent }, // Sequence Number
        { 0x01, ReadTextMetaEvent }, // Text
        { 0x02, ReadCopyrightMetaEvent }, // Copyright
        { 0x03, ReadTrackNameMetaEvent }, // Sequence / Track Name
        { 0x04, ReadInstrumentNameMetaEvent }, // Instrument Name
        { 0x05, ReadNotImplementedEvent }, // Lyric
        { 0x06, ReadNotImplementedEvent }, // Marker
        { 0x07, ReadNotImplementedEvent }, // Cue Point
        { 0x08, ReadNotImplementedEvent }, // Program Name
        { 0x09, ReadNotImplementedEvent }, // Device Name
        { 0x20, ReadNotImplementedEvent }, // MIDI Channel Prefix
        { 0x21, ReadNotImplementedEvent }, // MIDI Port
        { 0x2F, ReadEndOfTrackEvent }, // End of Track
        { 0x51, ReadTempoMetaEvent }, // Tempo Change
        { 0x54, ReadNotImplementedEvent }, // SMPTE Offset
        { 0x58, ReadNotImplementedEvent }, // Time Signature
        { 0x59, ReadNotImplementedEvent }, // Key Signature
        { 0x7F, ReadNotImplementedEvent }, // Sequence Specific Event
    };

    // Parser methods called for midi-events (Status = 0x80 to 0xE0)
    private static readonly Dictionary<ushort, Action<MidiStreamWrapper, int, int, MidiTrackInfo>> MidiEventHandlers = new()
    {
        { 0x80, ReadGenericDoubleByteMidiEvent }, // Note Off
        { 0x90, ReadGenericDoubleByteMidiEvent }, // Note On
        { 0xA0, ReadGenericDoubleByteMidiEvent }, // Polyphonic Key Pressure
        { 0xB0, ReadGenericDoubleByteMidiEvent }, // Control Change
        { 0xC0, ReadProgramChangeEvent }, // Program Change
        { 0xD0, ReadGenericSingleByteMidiEvent }, // Channel Pressure
        { 0xE0, ReadGenericDoubleByteMidiEvent }, // Pitch Bend
    };

    // Meta-Event methods
    private static void ReadTextMetaEvent(MidiStreamWrapper stream, int eventLength, int currentTick, MidiTrackInfo trackInfo)
    {
        trackInfo.Text = stream.ReadString(eventLength);
    }

    private static void ReadCopyrightMetaEvent(MidiStreamWrapper stream, int eventLength, int currentTick, MidiTrackInfo trackInfo)
    {
        trackInfo.Copyright = stream.ReadString(eventLength);
    }

    private static void ReadTrackNameMetaEvent(MidiStreamWrapper stream, int eventLength, int currentTick, MidiTrackInfo trackInfo)
    {
        trackInfo.TrackName = stream.ReadString(eventLength);
    }

    private static void ReadInstrumentNameMetaEvent(MidiStreamWrapper stream, int eventLength, int currentTick, MidiTrackInfo trackInfo)
    {
        trackInfo.InstrumentName = stream.ReadString(eventLength);
    }

    private static void ReadTempoMetaEvent(MidiStreamWrapper stream, int eventLength, int currentTick, MidiTrackInfo trackInfo)
    {
        var newTempo = stream.ReadUInt24();
        trackInfo.TempoMap[currentTick] = (int)newTempo;
    }

    private static void ReadEndOfTrackEvent(MidiStreamWrapper stream, int eventLength, int currentTick, MidiTrackInfo trackInfo)
    {
        if (trackInfo.TempoMap.Count > 0)
            trackInfo.TempoMap[currentTick] = trackInfo.TempoMap.Last().Value;
    }

    private static void ReadNotImplementedEvent(MidiStreamWrapper stream, int eventLength, int currentTick, MidiTrackInfo trackInfo)
    {
        stream.Skip(eventLength);
    }

    // Midi-Event methods
    private static void ReadProgramChangeEvent(MidiStreamWrapper stream, int channel, int currentTick, MidiTrackInfo trackInfo)
    {
        trackInfo.UsedChannels[channel] = true;

        // We could store used programs on each channel later for the user to view.
        stream.Skip(1);
    }

    private static void ReadGenericSingleByteMidiEvent(MidiStreamWrapper stream, int channel, int currentTick, MidiTrackInfo trackInfo)
    {
        trackInfo.UsedChannels[channel] = true;
        stream.Skip(1);
    }

    private static void ReadGenericDoubleByteMidiEvent(MidiStreamWrapper stream, int channel, int currentTick, MidiTrackInfo trackInfo)
    {
        trackInfo.UsedChannels[channel] = true;
        stream.Skip(2);
    }
}
