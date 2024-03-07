using Content.Shared.CartridgeLoader;
using Content.Shared.CartridgeLoader.Cartridges;
using Content.Shared.CriminalRecords;
using Content.Shared.Security;
using Content.Shared.CCVar;
using Robust.Shared.Prototypes;
using Robust.Shared.Containers;
using Robust.Shared.Configuration;
using Content.Server.StationRecords.Systems;
using Content.Server.Station.Systems;

namespace Content.Server.CartridgeLoader.Cartridges;

public sealed class CriminalRecordsCartridgeSystem : EntitySystem
{
  [Dependency] private readonly CartridgeLoaderSystem _cartridgeLoader = default!;
  [Dependency] private readonly StationRecordsSystem _stationRecords = default!;
  [Dependency] private readonly StationSystem _stationSystem = default!;

  [ValidatePrototypeId<EntityPrototype>]
  private const string CartridgePrototypeName = "CriminalRecordsCartridge";

  public override void Initialize()
  {
    base.Initialize();
    SubscribeLocalEvent<CriminalRecordsCartridgeComponent, CartridgeMessageEvent>(OnUiMessage);
    SubscribeLocalEvent<CriminalRecordsCartridgeComponent, CartridgeUiReadyEvent>(OnUiReady); 
    SubscribeLocalEvent<ProgramInstallationAttempt>(OnInstallationAttempt);
  }

  private void OnUiMessage(EntityUid uid, CriminalRecordsCartridgeComponent comp, CartridgeMessageEvent args)
  {
    UpdateUiState(uid, GetEntity(args.LoaderUid), comp);
  }

  private void OnUiReady(EntityUid uid, CriminalRecordsCartridgeComponent comp, CartridgeUiReadyEvent args)
  {
    UpdateUiState(uid, args.Loader, comp);
  }
  
  private void UpdateUiState(EntityUid uid, EntityUid loaderUid, CriminalRecordsCartridgeComponent? comp)
  {
    if(!Resolve(uid, ref comp))
      return;

    var owningStation = _stationSystem.GetOwningStation(uid);

    if (owningStation is null)
      return;

    var records = _stationRecords.GetRecordsOfType<CriminalRecord>(owningStation.Value);
    
    var state = new CriminalRecordsCartridgeUiState(records);
    _cartridgeLoader.UpdateCartridgeUiState(loaderUid, state);
  }

  private void OnInstallationAttempt(ref ProgramInstallationAttempt args)
  {
    //Do nothing
  }

}
