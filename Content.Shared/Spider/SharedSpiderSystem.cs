using Content.Shared.Actions;
using Robust.Shared.Timing;

namespace Content.Shared.Spider;

public abstract class SharedSpiderSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _action = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpiderComponent, MapInitEvent>(OnInit);
    }

    private void OnInit(EntityUid uid, SpiderComponent component, MapInitEvent args)
    {
        _action.AddAction(uid, ref component.Action, component.WebAction, uid);
    }
}
