using Content.Server.EUI;
using Content.Shared.CrewManifest;
using Content.Shared.Eui;

namespace Content.Server.CrewManifest;

public sealed class CrewManifestEui : BaseEui
{
    private readonly CrewManifestSystem _crewManifest;

    /// <summary>
    ///     Station this EUI instance is currently tracking.
    /// </summary>
    private readonly EntityUid _station;

    public CrewManifestEui(EntityUid station, CrewManifestSystem crewManifestSystem)
    {
        _station = station;
        _crewManifest = crewManifestSystem;
    }

    public override CrewManifestEuiState GetNewState()
    {
        var (name, entries) = _crewManifest.GetCrewManifest(_station);
        return new(name, entries);
    }

    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        switch (msg)
        {
            case CrewManifestEuiClosed:
                _crewManifest.CloseEui(_station, Player);
                break;
        }
    }
}
