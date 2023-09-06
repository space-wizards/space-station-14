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

        SubscribeLocalEvent<SpiderWebObjectComponent, ComponentStartup>(OnWebStartup);
        SubscribeLocalEvent<SpiderComponent, ComponentStartup>(OnSpiderStartup);
    }

    private void OnSpiderStartup(EntityUid uid, SpiderComponent component, ComponentStartup args)
    {
        _action.AddAction(uid, Spawn(component.WebActionName), null);
    }

    private void OnWebStartup(EntityUid uid, SpiderWebObjectComponent component, ComponentStartup args)
    {
        _appearance.SetData(uid, SpiderWebVisuals.Variant, _robustRandom.Next(1, 3));
    }
}
