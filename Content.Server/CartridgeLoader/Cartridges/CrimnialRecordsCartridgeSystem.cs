using Content.Shared.CartridgeLoader;
using Content.Shared.CartridgeLoader.Cartridges;
using Content.Shared.CriminalRecords;
using Content.Shared.StationRecords;
using Content.Shared.Security;
using Content.Shared.StationRecords;
using Content.Server.StationRecords.Systems;
using Content.Server.StationRecords;
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
        SubscribeLocalEvent<CriminalRecordsCartridgeComponent, CartridgeMessageEvent>(OnUiMessage);
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

    private void OnUiMessage(
      EntityUid uid,
      CriminalRecordsCartridgeComponent criminalRecordsCartridge,
      CartridgeMessageEvent args
      )
    {
        if (args is not CriminalRecordsCartridgeUiMessageEvent message)
            return;

        UpdateUiState(uid, criminalRecordsCartridge, GetEntity(args.LoaderUid));
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

        var wanted = new List<(GeneralStationRecord, CriminalRecord)>();
        var detained = new List<(GeneralStationRecord, CriminalRecord)>();

        foreach (var (id, criminalRecord) in records)
        {
            var name = "Unknown";

            if (!_stationRecords.TryGetRecord<GeneralStationRecord>(new StationRecordKey(id, owningStation.Value), out var stationRecord))
            { return; }
            switch (criminalRecord.Status)
            {
                case SecurityStatus.Wanted:
                    wanted.Add((stationRecord, criminalRecord));
                    break;
                case SecurityStatus.Detained:
                    detained.Add((stationRecord, criminalRecord));
                    break;
                default:
                    break;
            }
        }

        var state = new CriminalRecordsCartridgeUiState(wanted, detained);
        _cartridgeLoaderSystem?.UpdateCartridgeUiState(loaderUid, state);
    }
}
