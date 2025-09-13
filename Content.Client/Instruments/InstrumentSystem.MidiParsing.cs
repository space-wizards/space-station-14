using System.Linq;
using Content.Shared.Instruments;
using Robust.Shared.Audio.Midi;

namespace Content.Client.Instruments;

public sealed partial class InstrumentSystem
{
    /// <summary>
    /// Tries to parse the input data as a MIDI and retrieve track information..
    /// </summary>
    /// <remarks>
    /// Thank you to http://www.somascape.org/midi/tech/mfile.html for providing an awesome resource for midi files.
    /// </remarks>
    /// <remarks>
    /// This method has exception tolerance and does not throw, even if the midi file is invalid.
    /// </remarks>
    private void TryParseTracks(EntityUid uid, byte[] data)
    {
        if (!MidiParser.MidiParser.TryGetMidiTracks(data, out var tracks, out var error))
        {
            Log.Error(error);

            // We don't know how many channels there really are, so assume the max (16).
            return;
        }

        RaiseNetworkEvent(new InstrumentSetTracksEvent(GetNetEntity(uid), tracks.ToArray()));
    }
}
