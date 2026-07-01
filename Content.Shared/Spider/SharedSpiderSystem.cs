using Content.Shared.Actions;

namespace Content.Shared.Spider;

public abstract partial class SharedSpiderSystem : EntitySystem
{
    [Dependency] private SharedActionsSystem _action = default!;

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
