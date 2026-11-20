using System.Linq;
using Content.Shared.Instruments;
using Robust.Shared.Audio.Midi;

namespace Content.Client.Instruments;

public sealed partial class InstrumentSystem
{
    /// <summary>
    /// Tries to parse the input data as a midi and set the channel names respectively.
    /// </summary>
    /// <remarks>
    /// Thank you to http://www.somascape.org/midi/tech/mfile.html for providing an awesome resource for midi files.
    /// </remarks>
    /// <remarks>
    /// This method has exception tolerance and does not throw, even if the midi file is invalid.
    /// </remarks>
    private bool TrySetChannels(EntityUid uid, byte[] data)
    {
        if (!MidiParser.MidiParser.TryGetMidiTracks(data, out var tracks, out var error))
        {
            Log.Error(error);
            return false;
        }

        var resolvedTracks = new List<MidiTrack?>();
        for (var index = 0; index < tracks.Length; index++)
        {
            var midiTrack = tracks[index];
            if (midiTrack is { TrackName: null, ProgramName: null, InstrumentName: null})
                continue;

            switch (midiTrack)
            {
                case { TrackName: not null, ProgramName: not null }:
                case { TrackName: not null, InstrumentName: not null }:
                case { TrackName: not null }:
                case { ProgramName: not null }:
                    resolvedTracks.Add(midiTrack);
                    break;
                default:
                    resolvedTracks.Add(null); // Used so the channel still displays as MIDI Channel X and doesn't just take the next valid one in the UI
                    break;
            }

            Log.Debug($"Channel name: {resolvedTracks.Last()}");
        }

        RaiseNetworkEvent(new InstrumentSetChannelsEvent(GetNetEntity(uid), resolvedTracks.Take(RobustMidiEvent.MaxChannels).ToArray()));
        Log.Debug($"Resolved {resolvedTracks.Count} channels.");

        return true;
    }
}
