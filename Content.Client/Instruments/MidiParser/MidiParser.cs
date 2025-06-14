using System.Diagnostics.CodeAnalysis;
using System.Text;
using Content.Shared.Instruments;

namespace Content.Client.Instruments.MidiParser;

public static class MidiParser
{
    // Thanks again to http://www.somascape.org/midi/tech/mfile.html
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
                                        track.ProgramName = Loc.GetString($"instruments-component-menu-midi-channel-{((MidiInstrument)programNumber).GetStringRep()}");
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
}
