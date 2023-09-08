using Content.Shared.Actions;
using Robust.Shared.Random;

namespace Content.Shared.Spider;

public abstract class SharedSpiderSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _action = default!;
    [Dependency] private readonly IRobustRandom _robustRandom = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpiderComponent, MapInitEvent>(OnSpiderMapInit);
        SubscribeLocalEvent<SpiderWebObjectComponent, ComponentStartup>(OnWebStartup);
    }

    private void OnSpiderMapInit(EntityUid uid, SpiderComponent component, MapInitEvent args)
    {
        _action.AddAction(uid, Spawn(component.WebAction), null);
    }

    private void OnWebStartup(EntityUid uid, SpiderWebObjectComponent component, ComponentStartup args)
    {
        _appearance.SetData(uid, SpiderWebVisuals.Variant, _robustRandom.Next(1, 3));
    }
}
