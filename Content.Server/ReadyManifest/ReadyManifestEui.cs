using Content.Server.EUI;
using Content.Shared.ReadyManifest;

namespace Content.Server.ReadyManifest;

public sealed class ReadyManifestEui : BaseEui
{
    private readonly ReadyManifestSystem _readyManifest;

    public ReadyManifestEui(ReadyManifestSystem readyManifestSystem)
    {
        _readyManifest = readyManifestSystem;
    }

    public override ReadyManifestEuiState GetNewState()
    {
        var entries = _readyManifest.GetReadyManifest();
        return new(entries);
    }

    public override void Closed()
    {
        base.Closed();

        _readyManifest.CloseEui(Player);
    }
}
