using Content.Shared.CriminalRecords;
using Robust.Shared.Serialization;

namespace Content.Shared.CartridgeLoader.Cartridges;
[Serializable, NetSerializable]
public sealed class CriminalRecordsCartridgeUiState : BoundUserInterfaceState
{
  public IEnumerable<(uint, CriminalRecord)>? Records;

  public CriminalRecordsCartridgeUiState(IEnumerable<(uint, CriminalRecord)>? records)
  {
    Records = records;
  }
}
