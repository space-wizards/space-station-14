using Content.Shared.CartridgeLoader;
using Content.Shared.CartridgeLoader.Cartridges;
using Content.Shared.CriminalRecords;
using Content.Shared.StationRecords;
using Content.Shared.Security;
using Content.Server.StationRecords.Systems;
using Content.Server.Station.Systems;

namespace Content.Server.CartridgeLoader.Cartridges;

public sealed class CriminalRecordsCartridgeSystem : EntitySystem
{
    [Dependency] private readonly CartridgeLoaderSystem? _cartridgeLoaderSystem = default!;
    [Dependency] private readonly StationRecordsSystem _stationRecords = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CriminalRecordsCartridgeComponent, CartridgeUiReadyEvent>(OnUiReady);
    }

    private void OnUiReady(
      EntityUid uid,
      CriminalRecordsCartridgeComponent criminalRecordsCartridge,
      CartridgeUiReadyEvent args
      )
    {
        UpdateUiState(uid, criminalRecordsCartridge, args.Loader);
    }

    private void UpdateUiState(
      EntityUid uid,
      CriminalRecordsCartridgeComponent? criminalRecordsCartridge,
      EntityUid loaderUid
      )
    {

        if (!Resolve(uid, ref criminalRecordsCartridge))
            return;

        var owningStation = _stationSystem.GetOwningStation(uid);

        if (owningStation is null)
            return;

        var records = _stationRecords.GetRecordsOfType<CriminalRecord>(owningStation.Value);

        var criminals = new List<(GeneralStationRecord, CriminalRecord)>();

        foreach (var (stationRecordKey, criminalRecord) in records)
        {
            if (criminalRecord.Status == SecurityStatus.None)
            {
                return;
            }

            if (!_stationRecords.TryGetRecord<GeneralStationRecord>(new StationRecordKey(stationRecordKey, owningStation.Value), out var stationRecord))
            {
                return;
            }
            criminals.Add((stationRecord, criminalRecord));
        }

        var state = new CriminalRecordsCartridgeUiState(criminals);
        _cartridgeLoaderSystem?.UpdateCartridgeUiState(loaderUid, state);
    }
}
