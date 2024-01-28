using Content.Shared.Prototypes.Components;

namespace Content.Shared.Prototypes.Systems;

public sealed class SharedComponentRemoverSystem : EntitySystem
{
    [Dependency] private readonly IComponentFactory _componentFactory = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ComponentRemoverComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(EntityUid uid, ComponentRemoverComponent comp, MapInitEvent ev)
    {
        if (!comp.DoOnMapInit)
            return;

        foreach (var entry in comp.Components)
            if (_componentFactory.TryGetRegistration(entry.Key, out var registration))
                RemComp(uid, registration.Type);
    }
}
