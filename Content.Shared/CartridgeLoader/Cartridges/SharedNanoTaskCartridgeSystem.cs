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

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<NanoTaskCartridgeComponent>();
        while (query.MoveNext(out var uid, out var cartridge))
        {
            if (cartridge.PrintDelayRemaining is null)
                continue;

            cartridge.PrintDelayRemaining = cartridge.PrintDelayRemaining.Value - frameTime;
            if (cartridge.PrintDelayRemaining.Value <= 0.0)
            {
                cartridge.PrintDelayRemaining = null;
            }
        }
    }

    private void OnCartridgeAdded(Entity<NanoTaskCartridgeComponent> ent, ref CartridgeAddedEvent args)
    {
        EnsureComp<NanoTaskInteractionComponent>(args.Loader);
    }
}
