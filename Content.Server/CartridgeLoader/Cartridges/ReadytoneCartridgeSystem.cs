using Content.Server.Instruments;
using Content.Shared.CartridgeLoader;
using Content.Shared.PDA;

namespace Content.Server.CartridgeLoader.Cartridges;

public sealed class ReadytoneCartridgeSystem : EntitySystem
{
    [Dependency] private readonly CartridgeLoaderSystem _cartridgeLoaderSystem = default!;
    [Dependency] private readonly InstrumentSystem _instrumentSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ReadytoneCartridgeComponent, CartridgeActivatedEvent>(OnCartridgeActivated);
        SubscribeLocalEvent<ReadytoneCartridgeComponent, CartridgeAddedEvent>(OnCartridgeAdded);
        SubscribeLocalEvent<ReadytoneCartridgeComponent, CartridgeRemovedEvent>(OnCartridgeRemoved);
    }

    private void OnCartridgeAdded(Entity<ReadytoneCartridgeComponent> ent, ref CartridgeAddedEvent args)
    {
        var instrument = EnsureComp<InstrumentComponent>(args.Loader);

        _instrumentSystem.SetInstrumentProgram(args.Loader, instrument, 2, 1);
    }

    private void OnCartridgeRemoved(Entity<ReadytoneCartridgeComponent> ent, ref CartridgeRemovedEvent args)
    {
        // only remove when the program itself is removed
        if (!_cartridgeLoaderSystem.HasProgram<ReadytoneCartridgeComponent>(args.Loader))
        {
            if (TryComp<InstrumentComponent>(args.Loader, out var instrument))
                _instrumentSystem.Clean(args.Loader, instrument);

            RemComp<InstrumentComponent>(args.Loader);
        }
    }

    private void OnCartridgeActivated(Entity<ReadytoneCartridgeComponent> ent, ref CartridgeActivatedEvent args)
    {
        if (TryComp<InstrumentComponent>(args.Loader, out var instrument))
            _instrumentSystem.ToggleInstrumentUi(args.Loader, args.Actor, instrument);
    }
}
