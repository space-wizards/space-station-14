using Content.Shared.CartridgeLoader;
using Content.Shared.CartridgeLoader.Cartridges;
using Robust.Shared.Prototypes;
using Content.Server.StationRecords.Systems;
using Content.Shared.CriminalRecords;
using Content.Shared.CriminalRecords;
using Content.Shared.Security;

namespace Content.Server.CartridgeLoader.Cartridges;

public sealed class CriminalRecordsCartridgeSystem : EntitySystem
{
  [Dependency] private readonly StationRecordsSystem _StationRecords = default!;
  [Dependency] private readyonly StationSystem _StationSystem = default!;

  [ValidateProtoTypeId<EntityPrototype>]
  private const string CartridgePrototypeName = "CriminalRecordsCartridge";

  public override void Initialize()
  {
    base.Initialize();
    SubscribeLocalEvent<CriminalRecordsCartridgeComponent, CartridgeMessageEvent>(OnUiMessage) 
    SubscribeLocalEvent<CriminalRecordsCartridgeComponent, CartridgeUiReadyEvent>(OnUiReady); 
    SubscribeLocalEvent<ProgramInstallationAttempt>(OnInstallationAttempt);
  }

  private void OnUiMessage(EntityUid uid, CriminalRecordsCartridgeComponent comp, CartridgeMessageEvent args)
  {
    UpdateUiState(uid, GetEntity(args.LoaderUid), comp);
  }

  private void OnUiReady(EntityUid, CriminalRecordsCartridgeComponent comp, CartridgeUiReadyEvent args)
  {
    UpdateUiState(uid, args.Loader, comp);
  }
  
  private void UpdateUiState(EntityUid uid, EntityUid loaderUid, CriminalRecordsCartridgeComponent? comp)
  {
    if(!Resolve(uid, ref comp))
      return;

    var owningStation = _StationSystem.GetOwningStation(uid);

    if (owningStation is null)
      return;

    var records = _StationRecords.GetRecordsOfType<CriminalRecord>(owningStation);
    
    var state = new CriminalRecordsCartridgeUiState(records);
    _cartridgeLoader.UpdateCartridgeUiState(loaderUid, state);
  }

  private void OnInstallationAttempt(ref ProgramInstallationAttempt args)
  {
    //Do nothing
  }

}
