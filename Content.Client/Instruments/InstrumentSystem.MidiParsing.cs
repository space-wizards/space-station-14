using System.Linq;
using Content.Shared.Instruments;

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

        var resolvedTracks = new List<string?>();
        for (var index = 0; index < tracks.Length; index++)
        {
            var midiTrack = tracks[index];
            if (midiTrack is { TrackName: null, ProgramName: null, InstrumentName: null})
                continue;

            switch (midiTrack)
            {
                case { TrackName: not null, ProgramName: not null }:
                    resolvedTracks.Add($"{midiTrack.TrackName} ({midiTrack.ProgramName})");
                    break;
                case { TrackName: not null, InstrumentName: not null }:
                    resolvedTracks.Add($"{midiTrack.TrackName} ({midiTrack.InstrumentName})");
                    break;
                case { TrackName: not null }:
                    resolvedTracks.Add($"{midiTrack.TrackName}");
                    break;
                case { ProgramName: not null }:
                    resolvedTracks.Add($"{midiTrack.ProgramName}");
                    break;
                default:
                    resolvedTracks.Add(null);
                    break;
            }

            Log.Debug($"Channel name: {resolvedTracks.Last()}");
        }

        RaiseNetworkEvent(new InstrumentSetChannelsEvent(GetNetEntity(uid), resolvedTracks));
        Log.Debug($"Resolved {resolvedTracks.Count} channels.");

        return true;
    }
}
