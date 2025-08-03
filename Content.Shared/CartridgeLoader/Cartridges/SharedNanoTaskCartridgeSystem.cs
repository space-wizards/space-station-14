using Content.Shared.CartridgeLoader;
using Content.Shared.CartridgeLoader.Cartridges;

namespace Content.Shared.CartridgeLoader.Cartridges;

public abstract class SharedNanoTaskCartridgeSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NanoTaskCartridgeComponent, CartridgeAddedEvent>(OnCartridgeAdded);
    }

    private void OnCartridgeAdded(Entity<NanoTaskCartridgeComponent> ent, ref CartridgeAddedEvent args)
    {
        EnsureComp<NanoTaskInteractionComponent>(args.Loader);
    }
}
