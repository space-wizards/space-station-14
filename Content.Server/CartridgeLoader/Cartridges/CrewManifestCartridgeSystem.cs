using Content.Server.CrewManifest;
using Content.Server.Station.Systems;
using Content.Shared.CartridgeLoader;
using Content.Shared.CartridgeLoader.Cartridges;
using Content.Shared.CCVar;
using Robust.Shared.Configuration;

namespace Content.Server.CartridgeLoader.Cartridges;

public sealed class CrewManifestCartridgeSystem : EntitySystem
{
    [Dependency] private readonly CartridgeLoaderSystem _cartridgeLoader = default!;
    [Dependency] private readonly IConfigurationManager _configManager = default!;
    [Dependency] private readonly CrewManifestSystem _crewManifest = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;

    private const string CartridgePrototypeName = "CrewManifestCartridge";

    /// <summary>
    /// Flag that shows that if crew manifest is allowed to be viewed from 'unsecure' entities,
    /// which is the keys for the cartridge.
    /// </summary>
    private bool _unsecureViewersAllowed = true;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CrewManifestCartridgeComponent, CartridgeMessageEvent>(OnUiMessage);
        SubscribeLocalEvent<CrewManifestCartridgeComponent, CartridgeUiReadyEvent>(OnUiReady);
        SubscribeLocalEvent<ProgramInstallationAttempt>(OnInstallationAttempt);
        _configManager.OnValueChanged(CCVars.CrewManifestUnsecure, OnCrewManifestUnsecureChanged, true);
    }

    /// <summary>
    /// The ui messages received here get wrapped by a CartridgeMessageEvent and are relayed from the <see cref="CartridgeLoaderSystem"/>
    /// </summary>
    /// <remarks>
    /// The cartridge specific ui message event needs to inherit from the CartridgeMessageEvent
    /// </remarks>
    private void OnUiMessage(EntityUid uid, CrewManifestCartridgeComponent component, CartridgeMessageEvent args)
    {
        UpdateUiState(uid, args.LoaderUid, component);
    }

    /// <summary>
    /// This gets called when the ui fragment needs to be updated for the first time after activating
    /// </summary>
    private void OnUiReady(EntityUid uid, CrewManifestCartridgeComponent component, CartridgeUiReadyEvent args)
    {
        UpdateUiState(uid, args.Loader, component);
    }

    private void UpdateUiState(EntityUid uid, EntityUid loaderUid, CrewManifestCartridgeComponent? component)
    {
        if (!Resolve(uid, ref component))
            return;

        var owningStation = _stationSystem.GetOwningStation(uid);

        if (owningStation is null)
            return;

        var (stationName, entries) = _crewManifest.GetCrewManifest(owningStation.Value);

        var state = new CrewManifestUiState(stationName, entries);
        _cartridgeLoader.UpdateCartridgeUiState(loaderUid, state);
    }

    private void OnInstallationAttempt(ref ProgramInstallationAttempt args)
    {
        if (args.Prototype == CartridgePrototypeName && !_unsecureViewersAllowed)
            args.Cancelled = true;
    }

    private void OnCrewManifestUnsecureChanged(bool unsecureViewersAllowed)
    {
        _unsecureViewersAllowed = unsecureViewersAllowed;

        var allCartridgeLoaders = AllEntityQuery<CartridgeLoaderComponent>();

        while (allCartridgeLoaders.MoveNext(out EntityUid loaderUid, out CartridgeLoaderComponent? comp))
        {
            if (_unsecureViewersAllowed)
                _cartridgeLoader?.InstallProgram(loaderUid, CartridgePrototypeName, false, comp);
            else
                _cartridgeLoader?.UninstallProgram(loaderUid, CartridgePrototypeName, comp);
        }
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _configManager.UnsubValueChanged(CCVars.CrewManifestUnsecure, OnCrewManifestUnsecureChanged);
    }
}
