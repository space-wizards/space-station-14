using Content.Shared.CartridgeLoader;
using Content.Shared.CartridgeLoader.Cartridges;
using Content.Shared.GPS.Components;

namespace Content.Server.CartridgeLoader.Cartridges;

public sealed class AstroNavCartridgeSystem : EntitySystem
{
    [Dependency] private readonly CartridgeLoaderSystem? _cartridgeLoaderSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AstroNavCartridgeComponent, CartridgeAddedEvent>(OnCartridgeAdded);
        SubscribeLocalEvent<AstroNavCartridgeComponent, CartridgeRemovedEvent>(OnCartridgeRemoved);
    }

    private void OnCartridgeAdded(Entity<AstroNavCartridgeComponent> ent, ref CartridgeAddedEvent args)
    {
        var loader = args.Loader;
        if (!EntityManager.HasComponent<HandheldGPSComponent>(loader))
        {
            EntityManager.AddComponent<HandheldGPSComponent>(loader);
        }
    }

    private void OnCartridgeRemoved(Entity<AstroNavCartridgeComponent> ent, ref CartridgeRemovedEvent args)
    {
        var loader = args.Loader;
        EntityManager.RemoveComponent<HandheldGPSComponent>(loader);
    }
}
