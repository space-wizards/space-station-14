using Content.Shared.CartridgeLoader;
using Content.Shared.CartridgeLoader.Cartridges;

namespace Content.Server.CartridgeLoader.Cartridges;

public sealed class CriminalRecordsCartridgeSystem : EntitySystem
{
  [Dependency] private readonly CartridgeLoaderSystem? _cartridgeLoaderSystem = default!;

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

    UpdateUiState(uid, criminalRecordsCartridge,GetEntity(args.LoaderUid));
  }

  private void UpdateUiState(
    EntityUid uid,
    CriminalRecordsCartridgeComponent? criminalRecordsCartridge,
    EntityUid loaderUid
    )
  {
    var state = new CriminalRecordsCartridgeUiState();
    _cartridgeLoaderSystem?.UpdateCartridgeUiState(loaderUid, state);
  }
}
