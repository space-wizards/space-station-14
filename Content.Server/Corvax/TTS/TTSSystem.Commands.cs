// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Corvax.TTS.Commands;

namespace Content.Server.Corvax.TTS;

// ReSharper disable once InconsistentNaming
public sealed partial class TTSSystem
{
    public void RequestResetAllClientQueues()
    {
        var ev = new TtsQueueResetMessage();
        RaiseNetworkEvent(ev);
    }
}
