using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Content.Shared.Instruments;

namespace Content.Client.Instruments.MidiParser;

public static partial class MidiParser
{
    private const int DefaultTempoMicroseconds = 500_000;
    private const int OneMinuteInMicroseconds = 60_000_000;

    private static MidiHeaderInfo? ReadHeaderChunk(MidiStreamWrapper stream)
    {
        if (stream.ReadString(4) != "MThd")
            return null;

        var length = stream.ReadUInt32();
        var format = stream.ReadUInt16(); // format
        var trackCount = stream.ReadUInt16();
        var timebase = stream.ReadUInt16(); // format

        // Let's hope we don't get any of those.
        var isSmpte = (timebase & 0x8000) == 1;

        // Skip additional header nonsense
        stream.Skip((int)(length - 6));

        return new MidiHeaderInfo { Format = format, NumTracks = trackCount, IsSmpte = isSmpte, TimeBase = timebase };
    }

    private static MidiTrackInfo? ReadTrackChunk(MidiStreamWrapper stream)
    {
        var id = stream.ReadString(4);
        var length = stream.ReadUInt32();

        // Skip non-sense chunk
        if (id == "MTrk")
            return ReadTrack(stream, length);

        stream.Skip((int)length);
        return null;
    }

    private static MidiTrackInfo? ReadTrack(MidiStreamWrapper stream, uint length)
    {
        var trackInfo = new MidiTrackInfo();
        var trackEnd = stream.StreamPosition + length;
        long currentTick = 0;
        byte? lastStatusByte = null;

        while (stream.StreamPosition < trackEnd)
        {
            long deltaTime = stream.ReadVariableLengthQuantity();
            currentTick += deltaTime;

            var firstByte = stream.ReadByte();

            if (firstByte >= 0x80)
            {
                lastStatusByte = firstByte;
            }
            else
            {
                // Running status: push byte back for reading as data
                stream.Skip(-1);
            }

            // Return on invalid data
            if (lastStatusByte == null)
                return null;

            var eventType = (byte)(lastStatusByte & 0xF0);
            var eventChannel = (byte)(lastStatusByte & 0x0F);

            switch (lastStatusByte)
            {
                case 0xFF:
                {
                    // Meta-Event
                    var metaType = stream.ReadByte();
                    var metaLength = stream.ReadVariableLengthQuantity();

                    if (!MidiMetaEventHandlers.TryGetValue(metaType, out var midiMetaEventHandler))
                    {
                        stream.Skip((int)metaLength);
                        continue;
                    }

                    midiMetaEventHandler.Invoke(stream, (int)metaLength, currentTick, trackInfo);
                    break;
                }

                case 0xF0 or 0xF7:
                {
                    var sysexLength = stream.ReadVariableLengthQuantity();
                    stream.Skip((int)sysexLength);
                    // Sysex events and meta-events cancel any running status which was in effect.
                    // Running status does not apply to and may not be used for these messages.
                    lastStatusByte = null;
                    break;
                }

                default:
                    // Abort on invalid MIDI event
                    if (!MidiEventHandlers.TryGetValue(eventType, out var midiEventhandler))
                        return null;

                    midiEventhandler.Invoke(stream, eventChannel, currentTick, trackInfo);
                    break;
            }
        }

        trackInfo.TotalTicks = currentTick;
        return trackInfo;
    }

    private static double TickDeltaToMinutes(long timeBase, long tickDelta, long tempoMicroseconds)
    {
        var bpm = (double)OneMinuteInMicroseconds / tempoMicroseconds;
        var quarterNotesCount = (double)tickDelta / timeBase;
        return quarterNotesCount / bpm;
    }

    // Thanks again to http://www.somascape.org/midi/tech/mfile.html
    [Obsolete("Use MidiParser.TryParseMidi instead and access its Tracks property.")]
    public static bool TryGetMidiTracks(
        byte[] data,
        [NotNullWhen(true)] out MidiTrack[]? tracks,
        [NotNullWhen(false)] out string? error)
    {
        tracks = null;
        error = null;

        var stream = new MidiStreamWrapper(data);

        if (stream.ReadString(4) != "MThd")
        {
            error = "Invalid file header";
            return false;
        }

        var headerLength = stream.ReadUInt32();
        // MIDI specs define that the header is 6 bytes, we only look at the 6 bytes, if its more, we skip ahead.

        stream.Skip(2); // format
        var trackCount = stream.ReadUInt16();
        stream.Skip(2); // time div

        // We now skip ahead if we still have any header length left
        stream.Skip((int)(headerLength - 6));

        var parsedTracks = new List<MidiTrack>();

        for (var i = 0; i < trackCount; i++)
        {
            if (stream.ReadString(4) != "MTrk")
            {
                tracks = null;
                error = "Track contains invalid header";
                return false;
            }

            var track = new MidiTrack();

            var trackLength = stream.ReadUInt32();
            var trackEnd = stream.StreamPosition + trackLength;
            var hasMidiEvent = false;
            byte? lastStatusByte = null;

            while (stream.StreamPosition < trackEnd)
            {
                stream.ReadVariableLengthQuantity();

                /*
                 * If the first (status) byte is less than 128 (hex 80), this implies that running status is in effect,
                 * and that this byte is actually the first data byte (the status carrying over from the previous MIDI event).
                 * This can only be the case if the immediately previous event was also a MIDI event,
                 * i.e. SysEx and Meta events interrupt (clear) running status.
                 * See http://www.somascape.org/midi/tech/mfile.html#events
                 */

                var firstByte = stream.ReadByte();
                if (firstByte >= 0x80)
                {
                    lastStatusByte = firstByte;
                }
                else
                {
                    // Running status: push byte back for reading as data
                    stream.Skip(-1);
                }

                // The first event in each MTrk chunk must specify status.
                if (lastStatusByte == null)
                {
                    tracks = null;
                    error = "Track data not valid, expected status byte, got nothing.";
                    return false;
                }

                var eventType = (byte)(lastStatusByte & 0xF0);

                switch (lastStatusByte)
                {
                    // Meta events
                    case 0xFF:
                    {
                        var metaType = stream.ReadByte();
                        var metaLength = stream.ReadVariableLengthQuantity();
                        var metaData = stream.ReadBytes((int)metaLength);
                        if (metaType == 0x00) // SequenceNumber event
                            continue;

                        // Meta event types 01 through 0F are reserved for text and all follow the basic FF 01 len text format
                        if (metaType is < 0x01 or > 0x0F)
                            break;

                        // 0x03 is TrackName,
                        // 0x04 is InstrumentName

                        // This string can potentially contain control characters, including 0x00 which can cause problems if it ends up in database entries via admin logs
                        // we sanitize TrackName and InstrumentName after they have been send to the server
                        var text = Encoding.ASCII.GetString(metaData, 0, (int)metaLength);
                        switch (metaType)
                        {
                            case 0x03 when track.TrackName == null:
                                track.TrackName = text;
                                break;
                            case 0x04 when track.InstrumentName == null:
                                track.InstrumentName = text;
                                break;
                        }

                        // still here? then we dont care about the event
                        break;
                    }

                    // SysEx events
                    case 0xF0:
                    case 0xF7:
                    {
                        var sysexLength = stream.ReadVariableLengthQuantity();
                        stream.Skip((int)sysexLength);
                        // Sysex events and meta-events cancel any running status which was in effect.
                        // Running status does not apply to and may not be used for these messages.
                        lastStatusByte = null;
                        break;
                    }


                    default:
                        switch (eventType)
                        {
                            // Program Change
                            case 0xC0:
                            {
                                var programNumber = stream.ReadByte();
                                if (track.ProgramName == null)
                                {
                                    if (programNumber < Enum.GetValues<MidiInstrument>().Length)
                                        track.ProgramName =
                                            Loc.GetString(
                                                $"instruments-component-menu-midi-channel-{((MidiInstrument)programNumber).GetStringRep()}");
                                }

                                break;
                            }

                            case 0x80: // Note Off
                            case 0x90: // Note On
                            case 0xA0: // Polyphonic Key Pressure
                            case 0xB0: // Control Change
                            case 0xE0: // Pitch Bend
                            {
                                hasMidiEvent = true;
                                stream.Skip(2);
                                break;
                            }

                            case 0xD0: // Channel Pressure
                            {
                                hasMidiEvent = true;
                                stream.Skip(1);
                                break;
                            }

                            default:
                                error = $"Unknown MIDI event type {lastStatusByte:X2}";
                                tracks = null;
                                return false;
                        }

                        break;
                }
            }


            if (hasMidiEvent)
                parsedTracks.Add(track);
        }

        tracks = parsedTracks.ToArray();

        return true;
    }

    /// <summary>
    /// Parses MIDI and returns a DTO containing the parsed information about it.
    /// </summary>
    /// <param name="data">The MIDI data to parse in binary.</param>
    /// <param name="info">MidiInfo output containing the result.</param>
    /// <param name="error">String output containing the error on fail.</param>
    /// <returns>True on success</returns>
    public static bool TryParseMidi(
        byte[] data,
        [NotNullWhen(true)] out MidiInfo? info,
        [NotNullWhen(false)] out string? error)
    {
        error = "";
        info = null;
        var stream = new MidiStreamWrapper(data);
        var headerChunk = ReadHeaderChunk(stream);

        if (headerChunk == null)
        {
            error = "Invalid MIDI header";
            return false;
        }

        if (headerChunk.Format == 2)
        {
            error = "MIDI tempo format not supported";
            return false;
        }

        List<MidiTrackInfo> parsedTracks = [];
        var usedChannels = new BitArray(16, false);
        long mostTicksOnTrack = 0;

        for (var i = 0; i < headerChunk.NumTracks; i++)
        {
            var trackChunk = ReadTrackChunk(stream);
            if (trackChunk == null)
                continue;

            parsedTracks.Add(trackChunk);
            usedChannels.Or(trackChunk.UsedChannels);
            if (mostTicksOnTrack < trackChunk.TotalTicks)
                mostTicksOnTrack = trackChunk.TotalTicks;
        }

        double totalLengthMinutes = 0f;

        // https://midimusic.github.io/tech/midispec.html#BM2_2
        switch (headerChunk.Format)
        {
            // For a format 0 file, the tempo will be scattered through the track and the tempo map reader should ignore the intervening events.
            case 0:
            // For a format 1 file, the tempo map must be stored as the first track.
            case 1:
            {
                // Both formats (0 & 1) store the TempoMap inside the first track.
                if (parsedTracks.Count > 0 && parsedTracks[0].TempoMap.Count > 0)
                {
                    long currentTick = 0;
                    var currentTempo = DefaultTempoMicroseconds;
                    // Tempo changes parsed, calculate using TempoMap
                    foreach (var kv in parsedTracks[0].TempoMap)
                    {
                        // Calculate the passed ticks using the previous tempo.
                        var delta = kv.Key - currentTick;
                        var lengthMinutes = TickDeltaToMinutes(headerChunk.TimeBase, delta, currentTempo);
                        totalLengthMinutes += lengthMinutes;
                        // Update the current tempo
                        currentTempo = kv.Value;
                        currentTick += delta;
                    }

                    // Take longest tick length and add remaining delta with last tempo entry.
                    var remainingDelta = mostTicksOnTrack - currentTick;
                    totalLengthMinutes += TickDeltaToMinutes(headerChunk.TimeBase, remainingDelta, currentTempo);
                }
                else
                {
                    // No tempo changes, simply calculate using standard values and total ticks (Tempo = 500'000)
                    totalLengthMinutes = TickDeltaToMinutes(headerChunk.TimeBase,
                        mostTicksOnTrack,
                        DefaultTempoMicroseconds);
                }

                break;
            }

            default:
                error = "Invalid Format";
                return false;
        }

        info = new MidiInfo
        {
            Header = headerChunk,
            Tracks = parsedTracks.ToArray(),
            UsedChannels = usedChannels,
            TotalLengthMinutes = totalLengthMinutes,
        };

        return true;
    }
}
