using Robust.Shared.Serialization;
using Content.Shared.CriminalRecords;
using Content.Shared.StationRecords;

namespace Content.Shared.CartridgeLoader.Cartridges;

[Serializable, NetSerializable]
public sealed class CriminalRecordsCartridgeUiState : BoundUserInterfaceState
{

    public List<(GeneralStationRecord, CriminalRecord)> Criminals;
    public CriminalRecordsCartridgeUiState(List<(GeneralStationRecord, CriminalRecord)> criminals)
    {
        Criminals = criminals;
    }

}
