using System.Linq;
using Content.Shared.Instruments;
using Robust.Shared.Audio.Midi;

namespace Content.Client.Instruments;

public sealed partial class InstrumentSystem
{
    /// <summary>
    /// Tries to parse the input data as a midi and set all used channels to true.
    /// </summary>
    /// <remarks>
    /// Thank you to http://www.somascape.org/midi/tech/mfile.html for providing an awesome resource for midi files.
    /// </remarks>
    /// <remarks>
    /// This method has exception tolerance and does not throw, even if the midi file is invalid.
    /// </remarks>
    private bool TrySetChannels(EntityUid uid, byte[] data)
    {
        if (!MidiParser.MidiParser.TryParseMidi(data, out var info, out var error))
        {
            Log.Error(error);
            return false;
        }

        RaiseNetworkEvent(new InstrumentSetChannelsEvent(GetNetEntity(uid), info.UsedChannels));

        return true;
    }
}
