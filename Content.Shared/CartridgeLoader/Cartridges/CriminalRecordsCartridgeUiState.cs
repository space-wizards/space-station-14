using Robust.Shared.Serialization;
using Content.Shared.CriminalRecords;

namespace Content.Shared.CartridgeLoader.Cartridges;

[Serializable, NetSerializable]
public sealed class CriminalRecordsCartridgeUiState : BoundUserInterfaceState
{

  public List<(string,CriminalRecord)> Wanted;
  public List<(string,CriminalRecord)> Detained;

  public CriminalRecordsCartridgeUiState(List<(string,CriminalRecord)> wanted, List<(string,CriminalRecord)> detained)
  {
    Wanted = wanted;
    Detained = detained;   
  }

}
