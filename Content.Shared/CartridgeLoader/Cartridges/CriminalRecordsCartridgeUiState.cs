using Robust.Shared.Serialization;
using Content.Shared.CriminalRecords;
using Content.Shared.StationRecords;

namespace Content.Shared.CartridgeLoader.Cartridges;

[Serializable, NetSerializable]
public sealed class CriminalRecordsCartridgeUiState : BoundUserInterfaceState
{

    public List<(GeneralStationRecord, CriminalRecord)> Wanted;
    public List<(GeneralStationRecord, CriminalRecord)> Detained;

    public CriminalRecordsCartridgeUiState(List<(GeneralStationRecord, CriminalRecord)> wanted, List<(GeneralStationRecord, CriminalRecord)> detained)
    {
        Wanted = wanted;
        Detained = detained;
    }

}
