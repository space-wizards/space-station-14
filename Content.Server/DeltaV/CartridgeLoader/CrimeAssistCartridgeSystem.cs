using Content.Shared.CartridgeLoader;
using Content.Server.DeltaV.CartridgeLoader;
using Content.Server.CartridgeLoader.Cartridges;
using Content.Server.CartridgeLoader;

namespace Content.Server.DeltaV.CartridgeLoader.Cartridges;

public sealed class CrimeAssistCartridgeSystem : EntitySystem
{
    [Dependency] private readonly CartridgeLoaderSystem? _cartridgeLoaderSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
    }
}
