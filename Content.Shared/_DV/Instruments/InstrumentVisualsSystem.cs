using Content.Shared.Instruments;

namespace Content.Shared._DV.Instruments;

public sealed class InstrumentVisualsSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InstrumentVisualsComponent, BoundUIClosedEvent>(OnUIClosed);
        SubscribeLocalEvent<InstrumentVisualsComponent, BoundUIOpenedEvent>(OnUIOpened);
    }

    private void OnUIClosed(Entity<InstrumentVisualsComponent> ent, ref BoundUIClosedEvent args)
    {
        if (args.UiKey is not InstrumentUiKey)
            return;

        _appearance.SetData(ent, InstrumentVisuals.Playing, false);
    }

    private void OnUIOpened(Entity<InstrumentVisualsComponent> ent, ref BoundUIOpenedEvent args)
    {
        if (args.UiKey is not InstrumentUiKey)
            return;

        _appearance.SetData(ent, InstrumentVisuals.Playing, true);
    }
}
