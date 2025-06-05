using System.Diagnostics.CodeAnalysis;
using System.Text;
using Robust.Shared.Utility;

namespace Content.Client.Instruments.MidiParser;

public static class MidiParser
{
    // Based on https://www.ccarh.org/courses/253/handout/gminstruments/
    // Maybe localize? Idk.
    private static readonly string[] GeneralMidiInstruments =
    [
        "Acoustic Grand Piano", "Bright Acoustic Piano", "Electric Grand Piano", "Honky-tonk Piano",
        "Rhodes Piano", "Chorused Piano", "Harpsichord", "Clavinet",
        "Celesta", "Glockenspiel", "Music Box", "Vibraphone",
        "Marimba", "Xylophone", "Tubular Bells", "Dulcimer",
        "Hammond Organ", "Percussive Organ", "Rock Organ", "Church Organ",
        "Reed Organ", "Accordion", "Harmonica", "Tango Accordion",
        "Acoustic Nylon Guitar", "Acoustic Steel Guitar", "Electric Jazz Guitar", "Electric Clean Guitar",
        "Electric Muted Guitar", "Overdriven Guitar", "Distortion Guitar", "Guitar Harmonics",
        "Acoustic Bass", "Fingered Electric Bass", "Plucked Electric Bass", "Fretless Bass",
        "Slap Bass 1", "Slap Bass 2", "Synth Bass 1", "Synth Bass 2",
        "Violin", "Viola", "Cello", "Contrabass",
        "Tremolo Strings", "Pizzicato Strings", "Orchestral Harp", "Timpani",
        "String Ensemble 1", "String Ensemble 2", "Synth Strings 1", "Synth Strings 2",
        "Choir \"Aah\"", "Voice \"Ooh\"", "Synth Choir", "Orchestra Hit",
        "Trumpet", "Trombone", "Tuba", "Muted Trumpet",
        "French Horn", "Brass Section", "Synth Brass 1", "Synth Brass 2",
        "Soprano Sax", "Alto Sax", "Tenor Sax", "Baritone Sax",
        "Oboe", "English Horn", "Bassoon", "Clarinet",
        "Piccolo", "Flute", "Recorder", "Pan Flute",
        "Bottle Blow", "Shakuhachi", "Whistle", "Ocarina",
        "Square Wave Lead", "Sawtooth Wave Lead", "Calliope Lead", "Chiff Lead",
        "Charang Lead", "Voice Lead", "Fiths Lead", "Bass Lead",
        "New Age Pad", "Warm Pad", "Polysynth Pad", "Choir Pad",
        "Bowed Pad", "Metallic Pad", "Halo Pad", "Sweep Pad",
        "Rain Effect", "Soundtrack Effect", "Crystal Effect", "Atmosphere Effect",
        "Brightness Effect", "Goblins Effect", "Echoes Effect", "Sci-Fi Effect",
        "Sitar", "Banjo", "Shamisen", "Koto",
        "Kalimba", "Bagpipe", "Fiddle", "Shanai",
        "Tinkle Bell", "Agogo", "Steel Drums", "Woodblock",
        "Taiko Drum", "Melodic Tom", "Synth Drum", "Reverse Cymbal",
        "Guitar Fret Noise", "Breath Noise", "Seashore", "Bird Tweet",
        "Telephone Ring", "Helicopter", "Applause", "Gunshot",
    ];

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

        var headerLength = stream.ReadInt32();
        // MIDI specs define that the header is 6 bytes, we only look at the 6 bytes, if its more, we skip ahead.
        DebugTools.Assert(headerLength == 6, $"Invalid header length, expected 6, got {headerLength}");

        stream.Skip(2); // format
        var trackCount = stream.ReadInt16();
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

            var trackLength = stream.ReadInt32();
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
                            case 0x03 when track.TrackName != null:
                                track.TrackName = text;
                                break;
                            case 0x04 when track.InstrumentName != null:
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
                                    if (programNumber < GeneralMidiInstruments.Length)
                                        track.ProgramName = GeneralMidiInstruments[programNumber];
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
