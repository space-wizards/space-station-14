// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.TTS.Commands;

namespace Content.Server.SS220.TTS;

// ReSharper disable once InconsistentNaming
public sealed partial class TTSSystem
{
    public void RequestResetAllClientQueues()
    {
        var ev = new TtsQueueResetMessage();
        RaiseNetworkEvent(ev);
    }
}
