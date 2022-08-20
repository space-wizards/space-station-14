using Content.Shared.CartridgeLoader;

namespace Content.Server.CartridgeLoader.Cartridges;

public sealed class NotekeeperCartridgeSystem : EntitySystem
{
    [Dependency] private readonly CartridgeLoaderSystem? _computerSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<NotekeeperCartridgeComponent, CartridgeAddedEvent>(OnAdded);
        SubscribeLocalEvent<NotekeeperCartridgeComponent, CartridgeAfterInteractEvent>(OnUsed);
    }

    private void OnAdded(EntityUid uid, NotekeeperCartridgeComponent component, CartridgeAddedEvent args)
    {
        _computerSystem?.ActivateProgram(args.Computer, uid);
    }

    private void OnUsed(EntityUid uid, NotekeeperCartridgeComponent component, CartridgeAfterInteractEvent args)
    {
    }
}
